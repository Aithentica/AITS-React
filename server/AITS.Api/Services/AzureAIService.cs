using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITS.Api.Services;

public sealed class AzureAIService : IAzureAIService
{
    private readonly HttpClient _httpClient;
    private readonly AzureAIOptions _options;
    private readonly ILogger<AzureAIService> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public AzureAIService(HttpClient httpClient, IOptions<AzureAIOptions> options, ILogger<AzureAIService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("Nie skonfigurowano endpointu Azure AI.");
        }

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new InvalidOperationException($"Niepoprawny adres endpointu Azure AI: {_options.Endpoint}");
        }

        _httpClient.BaseAddress = endpointUri;

        if (_httpClient.Timeout == default)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(90);
        }

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<AzureAICompletionResult> GetCompletionAsync(AzureAICompletionRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Treść promptu nie może być pusta.", nameof(request));
        }

        var maxTokens = request.MaxTokens ?? _options.MaxTokens;
        if (maxTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxTokens), "Maksymalna liczba tokenów musi być większa od zera.");
        }

        var temperature = request.Temperature ?? _options.Temperature;
        if (double.IsNaN(temperature) || temperature < 0 || temperature > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Temperature), "Temperatura musi zawierać się w zakresie 0-2.");
        }

        var systemPrompt = string.IsNullOrWhiteSpace(request.SystemPrompt)
            ? _options.DefaultSystemPrompt
            : request.SystemPrompt;

        var payload = new ChatCompletionsPayload(
            Messages: BuildMessages(systemPrompt, request.Prompt),
            Temperature: temperature,
            MaxOutputTokens: maxTokens
        );

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildCompletionsPath());
        httpRequest.Headers.TryAddWithoutValidation("api-key", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(payload, options: _serializerOptions);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure AI zwróciło błąd {StatusCode}: {Response}", (int)response.StatusCode, responseText);
                throw new InvalidOperationException($"Azure AI zwróciło kod {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            return ParseResponse(responseText);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Żądanie do Azure AI zostało anulowane.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wystąpił błąd podczas komunikacji z Azure AI.");
            throw;
        }
    }

    private AzureAICompletionResult ParseResponse(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);

        if (!document.RootElement.TryGetProperty("choices", out var choicesElement) || choicesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Azure AI zwróciło niepoprawną odpowiedź (brak pola 'choices').");
        }

        var firstChoice = choicesElement.EnumerateArray().FirstOrDefault();
        if (firstChoice.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("Azure AI nie zwróciło żadnych wyników.");
        }

        var content = ExtractContent(firstChoice);
        var finishReason = firstChoice.TryGetProperty("finish_reason", out var finishElement)
            ? finishElement.GetString()
            : null;

        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;

        if (document.RootElement.TryGetProperty("usage", out var usageElement) && usageElement.ValueKind == JsonValueKind.Object)
        {
            if (usageElement.TryGetProperty("prompt_tokens", out var promptTokensElement) && promptTokensElement.TryGetInt32(out var promptValue))
            {
                promptTokens = promptValue;
            }

            if (usageElement.TryGetProperty("completion_tokens", out var completionTokensElement) && completionTokensElement.TryGetInt32(out var completionValue))
            {
                completionTokens = completionValue;
            }

            if (usageElement.TryGetProperty("total_tokens", out var totalTokensElement) && totalTokensElement.TryGetInt32(out var totalValue))
            {
                totalTokens = totalValue;
            }
        }

        return new AzureAICompletionResult(
            Content: content,
            FinishReason: finishReason,
            Usage: new AzureAIUsage(promptTokens, completionTokens, totalTokens)
        );
    }

    private static string ExtractContent(JsonElement choiceElement)
    {
        if (!choiceElement.TryGetProperty("message", out var messageElement))
        {
            return string.Empty;
        }

        if (!messageElement.TryGetProperty("content", out var contentElement))
        {
            return string.Empty;
        }

        return contentElement.ValueKind switch
        {
            JsonValueKind.String => contentElement.GetString() ?? string.Empty,
            JsonValueKind.Array => ExtractContentArray(contentElement),
            _ => string.Empty
        };
    }

    private static string ExtractContentArray(JsonElement arrayElement)
    {
        var builder = new StringBuilder();

        foreach (var item in arrayElement.EnumerateArray())
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.String:
                    AppendLine(builder, item.GetString());
                    break;
                case JsonValueKind.Object when item.TryGetProperty("text", out var textElement):
                    AppendLine(builder, textElement.GetString());
                    break;
            }
        }

        return builder.ToString();
    }

    private static void AppendLine(StringBuilder builder, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.Append(value);
    }

    private string BuildCompletionsPath()
    {
        var modelSegment = Uri.EscapeDataString(_options.ModelName);
        var version = Uri.EscapeDataString(_options.ModelVersion);
        return $"openai/models/{modelSegment}/chat/completions?api-version={version}";
    }

    private static IReadOnlyList<ChatMessagePayload> BuildMessages(string? systemPrompt, string prompt)
    {
        var messages = new List<ChatMessagePayload>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessagePayload("system", systemPrompt));
        }

        messages.Add(new ChatMessagePayload("user", prompt));

        return messages;
    }

    private sealed record ChatMessagePayload(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionsPayload(
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessagePayload> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens);
}


