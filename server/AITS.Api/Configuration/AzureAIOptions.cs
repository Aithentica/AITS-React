using System.ComponentModel.DataAnnotations;

namespace AITS.Api.Configuration;

public sealed class AzureAIOptions
{
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string ModelName { get; set; } = string.Empty;

    [Required]
    public string ModelVersion { get; set; } = "2024-08-01-preview";

    [Range(1, 64000)]
    public int MaxTokens { get; set; } = 1500;

    [Range(0, 2)]
    public double Temperature { get; set; } = 0.7;

    public string? DefaultSystemPrompt { get; set; }
}


