namespace AITS.Api.Services.Interfaces;

public interface ISmsApiClient
{
    Task<SmsApiSendResponse> SendAsync(string phoneNumber, string message, string sender, bool testMode, CancellationToken cancellationToken = default);

    Task<SmsApiStatusResponse> GetStatusAsync(string messageId, CancellationToken cancellationToken = default);

    Task<SmsApiBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default);
}

public sealed record SmsApiSendResponse(bool Success, string? MessageId, decimal? Points, string? ErrorMessage, string? PhoneNumber = null);

public sealed record SmsApiStatusResponse(string Status, string? PhoneNumber, string? ErrorCode);

public sealed record SmsApiBalanceResponse(decimal Points);



