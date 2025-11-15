using System.ComponentModel.DataAnnotations;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/azure-ai")]
[Authorize(Policy = "IsTherapistOrAdmin")]
public sealed class AzureAiController : ControllerBase
{
    private readonly IAzureAIService _azureAiService;
    private readonly ILogger<AzureAiController> _logger;

    public AzureAiController(IAzureAIService azureAiService, ILogger<AzureAiController> logger)
    {
        _azureAiService = azureAiService;
        _logger = logger;
    }

    [HttpPost("prompt")]
    public async Task<IActionResult> SendPrompt([FromBody] PromptRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Brak treści zapytania.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _azureAiService.GetCompletionAsync(
                new AzureAICompletionRequest(
                    request.Prompt,
                    request.SystemPrompt,
                    request.MaxTokens,
                    request.Temperature),
                cancellationToken);

            return Ok(new PromptResponse(
                result.Content,
                result.FinishReason,
                new UsageDto(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens)));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Niepoprawne dane promptu.");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Błąd podczas komunikacji z Azure AI.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nieoczekiwany błąd podczas obsługi zapytania AI.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Wystąpił nieoczekiwany błąd." });
        }
    }

    public sealed record PromptRequest(
        [property: Required(ErrorMessage = "Prompt jest wymagany")]
        [property: MinLength(1, ErrorMessage = "Prompt jest wymagany")]
        string Prompt,
        string? SystemPrompt,
        [property: Range(1, 64000, ErrorMessage = "Maksymalna liczba tokenów musi być większa od zera")]
        int? MaxTokens,
        [property: Range(0, 2, ErrorMessage = "Temperatura musi mieścić się w zakresie 0-2")]
        double? Temperature);

    public sealed record PromptResponse(string Content, string? FinishReason, UsageDto Usage);

    public sealed record UsageDto(int? PromptTokens, int? CompletionTokens, int? TotalTokens);
}


