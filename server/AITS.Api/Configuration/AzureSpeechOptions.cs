using System.ComponentModel.DataAnnotations;

namespace AITS.Api.Configuration;

public sealed class AzureSpeechOptions
{
    [Required]
    public string SubscriptionKey { get; set; } = string.Empty;

    [Required]
    public string Region { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    [Required]
    public string Language { get; set; } = "pl-PL";

    public int MaxSpeakerCount { get; set; } = 3;
}

