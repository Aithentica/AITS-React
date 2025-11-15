using AITS.Api.Services.Models;

namespace AITS.Api.Services.Interfaces;

public interface IGoogleOAuthService
{
    string BuildAuthorizationUrl(string terapeutaId, string redirectUri, string? state = null, string? prompt = "consent");

    Task<GoogleOAuthTokenResult> ExchangeCodeAsync(string terapeutaId, string code, string redirectUri, CancellationToken cancellationToken = default);

    Task<GoogleOAuthTokenResult> EnsureValidAccessTokenAsync(string terapeutaId, CancellationToken cancellationToken = default);

    Task<bool> DisconnectAsync(string terapeutaId, CancellationToken cancellationToken = default);

    Task<TherapistGoogleToken?> GetTokenAsync(string terapeutaId, CancellationToken cancellationToken = default);
}

