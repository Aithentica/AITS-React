using AITS.Api.Services.Models;

namespace AITS.Api.Services.Interfaces;

public interface ISmsService
{
    Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default);

    Task<SmsStatusResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default);

    Task<SmsBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default);
}

