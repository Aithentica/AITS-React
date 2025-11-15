using AITS.Api.Services.Models;

namespace AITS.Api.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

