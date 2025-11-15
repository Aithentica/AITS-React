namespace AITS.Api.Services.Models;

public sealed record AzureAICompletionRequest(
    string Prompt,
    string? SystemPrompt = null,
    int? MaxTokens = null,
    double? Temperature = null);

public sealed record AzureAICompletionResult(
    string Content,
    string? FinishReason,
    AzureAIUsage Usage);

public sealed record AzureAIUsage(
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens);


