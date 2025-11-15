using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using AITS.Api.Configuration;
using AITS.Api.Services;
using AITS.Api.Services.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AITS.Tests;

public sealed class AzureAIServiceTests
{
    [Fact]
    public async Task GetCompletionAsync_ShouldReturnParsedResult()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var json = "{\"choices\":[{\"index\":0,\"finish_reason\":\"stop\",\"message\":{\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"Odpowiedź modelu\"}]}}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":90,\"total_tokens\":100}}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var options = Options.Create(new AzureAIOptions
        {
            Endpoint = "https://example.openai.azure.com/",
            ApiKey = "test-key",
            ModelName = "gpt-4.1",
            ModelVersion = "2025-01-01-preview",
            MaxTokens = 100,
            Temperature = 0.7,
            DefaultSystemPrompt = "Jesteś pomocnym asystentem."
        });

        var service = new AzureAIService(httpClient, options, NullLogger<AzureAIService>.Instance);

        var result = await service.GetCompletionAsync(new AzureAICompletionRequest("Cześć"), CancellationToken.None);

        Assert.Equal("Odpowiedź modelu", result.Content);
        Assert.Equal("stop", result.FinishReason);
        Assert.Equal(10, result.Usage.PromptTokens);
        Assert.Equal(90, result.Usage.CompletionTokens);
        Assert.Equal(100, result.Usage.TotalTokens);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://example.openai.azure.com/openai/models/gpt-4.1/chat/completions?api-version=2025-01-01-preview", handler.LastRequest!.RequestUri!.ToString());
        Assert.True(handler.LastRequest!.Headers.TryGetValues("api-key", out var values) && values.Contains("test-key"));

        var body = await handler.ReadLastRequestBodyAsync();
        using var payloadDocument = JsonDocument.Parse(body);
        var messages = payloadDocument.RootElement.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("Jesteś pomocnym asystentem.", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("Cześć", messages[1].GetProperty("content").GetString());
        Assert.Equal(100, payloadDocument.RootElement.GetProperty("max_output_tokens").GetInt32());
    }

    [Fact]
    public async Task GetCompletionAsync_ShouldThrow_WhenPromptEmpty()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler);
        var options = Options.Create(new AzureAIOptions
        {
            Endpoint = "https://example.openai.azure.com/",
            ApiKey = "test-key",
            ModelName = "gpt-4.1",
            ModelVersion = "2025-01-01-preview",
            MaxTokens = 100,
            Temperature = 0.7
        });

        var service = new AzureAIService(httpClient, options, NullLogger<AzureAIService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCompletionAsync(new AzureAICompletionRequest(string.Empty), CancellationToken.None));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public Task<string> ReadLastRequestBodyAsync() => Task.FromResult(LastRequestBody ?? string.Empty);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return _responseFactory(request);
        }
    }
}


