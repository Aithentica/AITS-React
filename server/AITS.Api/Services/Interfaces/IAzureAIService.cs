using AITS.Api.Services.Models;

namespace AITS.Api.Services.Interfaces;

public interface IAzureAIService
{
    Task<AzureAICompletionResult> GetCompletionAsync(AzureAICompletionRequest request, CancellationToken cancellationToken);
}


