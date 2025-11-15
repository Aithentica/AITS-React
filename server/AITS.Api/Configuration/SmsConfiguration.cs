using System.ComponentModel.DataAnnotations;

namespace AITS.Api.Configuration;

public sealed class SmsConfiguration
{
    [Required]
    public string ApiToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(11)]
    public string SenderName { get; set; } = string.Empty;

    public string DefaultPhoneNumber { get; set; } = string.Empty;

    public bool TestMode { get; set; }
}



