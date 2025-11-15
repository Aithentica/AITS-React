using System.ComponentModel.DataAnnotations;

namespace AITS.Api.Services.Models;

public sealed record SmsSendRequest(
    [property: Required]
    string PhoneNumber,

    [property: Required]
    [property: MaxLength(160)]
    string Message,

    string? SenderName = null
);

public sealed record SmsSendResult(
    bool Success,
    string? MessageId,
    string? Error,
    decimal? Cost = null,
    int PartsCount = 1
);

public sealed record SmsStatusResult(
    string MessageId,
    string Status,
    string? PhoneNumber,
    int? ErrorCode = null,
    DateTime? DeliveredAt = null
);

public sealed record SmsBalanceResult(
    decimal Balance,
    string Currency
);

public sealed record EmailSendRequest(
    [property: Required]
    [property: EmailAddress]
    string To,

    [property: Required]
    string Subject,

    [property: Required]
    string Body,

    bool IsHtml = true,
    string? FromEmailOverride = null,
    string? FromNameOverride = null
);



