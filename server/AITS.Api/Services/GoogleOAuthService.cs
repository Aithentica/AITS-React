using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AITS.Api.Services;

public sealed class GoogleOAuthService : IGoogleOAuthService
{
    private static readonly string[] RequiredScopes = ["https://www.googleapis.com/auth/calendar"];
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleOAuthOptions _options;
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public GoogleOAuthService(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleOAuthOptions> options,
        ILogger<GoogleOAuthService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string BuildAuthorizationUrl(string terapeutaId, string redirectUri, string? state = null, string? prompt = "consent")
    {
        ValidateOptions();

        var finalRedirect = string.IsNullOrWhiteSpace(redirectUri)
            ? _options.DefaultRedirectUri ?? throw new InvalidOperationException("GoogleOAuth:DefaultRedirectUri not configured")
            : redirectUri;

        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = finalRedirect,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", RequiredScopes),
            ["access_type"] = "offline",
            ["prompt"] = string.IsNullOrWhiteSpace(prompt) ? "consent" : prompt,
            ["include_granted_scopes"] = "true",
            ["state"] = state ?? terapeutaId
        };

        return QueryHelpers.AddQueryString(AuthorizationEndpoint, queryParams!);
    }

    public async Task<GoogleOAuthTokenResult> ExchangeCodeAsync(string terapeutaId, string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var finalRedirect = string.IsNullOrWhiteSpace(redirectUri)
            ? _options.DefaultRedirectUri ?? throw new InvalidOperationException("GoogleOAuth:DefaultRedirectUri not configured")
            : redirectUri;

        var payload = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = finalRedirect,
            ["grant_type"] = "authorization_code"
        };

        return await ExchangeAsync(terapeutaId, payload, expectRefreshToken: true, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GoogleOAuthTokenResult> EnsureValidAccessTokenAsync(string terapeutaId, CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var entity = await _db.TherapistGoogleTokens
            .FirstOrDefaultAsync(x => x.TerapeutaId == terapeutaId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new GoogleOAuthTokenResult(false, "Therapist not connected to Google Calendar");
        }

        if (entity.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
        {
            return new GoogleOAuthTokenResult(true, null, entity);
        }

        return await RefreshAccessTokenAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DisconnectAsync(string terapeutaId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.TherapistGoogleTokens
            .FirstOrDefaultAsync(x => x.TerapeutaId == terapeutaId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        _db.TherapistGoogleTokens.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<TherapistGoogleToken?> GetTokenAsync(string terapeutaId, CancellationToken cancellationToken = default)
    {
        return _db.TherapistGoogleTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TerapeutaId == terapeutaId, cancellationToken);
    }

    private async Task<GoogleOAuthTokenResult> RefreshAccessTokenAsync(TherapistGoogleToken entity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.RefreshToken))
        {
            return new GoogleOAuthTokenResult(false, "Refresh token not available", entity);
        }

        var payload = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = entity.RefreshToken,
            ["grant_type"] = "refresh_token"
        };

        return await ExchangeAsync(entity.TerapeutaId, payload, expectRefreshToken: false, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<GoogleOAuthTokenResult> ExchangeAsync(string terapeutaId, IDictionary<string, string> payload, bool expectRefreshToken, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient("google-oauth");

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(payload)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google OAuth token exchange failed: {StatusCode} {Body}", response.StatusCode, content);
            
            // Jeśli to BadRequest, spróbuj zdeserializować odpowiedź błędu
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _serializerOptions);
                    var errorDescription = errorResponse?.ContainsKey("error_description") == true 
                        ? errorResponse["error_description"]?.ToString() 
                        : errorResponse?.ContainsKey("error") == true 
                            ? errorResponse["error"]?.ToString() 
                            : null;
                    
                    if (!string.IsNullOrWhiteSpace(errorDescription))
                    {
                        return new GoogleOAuthTokenResult(false, $"Google token error: {errorDescription}");
                    }
                }
                catch
                {
                    // Ignoruj błędy deserializacji
                }
            }
            
            return new GoogleOAuthTokenResult(false, $"Google token endpoint error: {response.StatusCode}");
        }

        GoogleTokenResponse? token;
        try
        {
            token = JsonSerializer.Deserialize<GoogleTokenResponse>(content, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Google token response: {Body}", content);
            return new GoogleOAuthTokenResult(false, "Invalid token response from Google");
        }

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            _logger.LogWarning("Google token response missing access token: {Body}", content);
            return new GoogleOAuthTokenResult(false, "Google token response invalid");
        }

        if (expectRefreshToken && string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            _logger.LogWarning("Google token response missing refresh token (first exchange)");
            return new GoogleOAuthTokenResult(false, "Google did not return refresh token");
        }

        var entity = await _db.TherapistGoogleTokens
            .FirstOrDefaultAsync(x => x.TerapeutaId == terapeutaId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new TherapistGoogleToken
            {
                TerapeutaId = terapeutaId,
                CreatedAt = now
            };
            _db.TherapistGoogleTokens.Add(entity);
        }

        entity.AccessToken = token.AccessToken;
        entity.TokenType = !string.IsNullOrWhiteSpace(token.TokenType) ? token.TokenType : "Bearer";
        entity.Scope = token.Scope ?? string.Join(" ", RequiredScopes);
        entity.ExpiresAt = now.AddSeconds(token.ExpiresIn <= 0 ? 3600 : token.ExpiresIn);
        entity.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            entity.RefreshToken = token.RefreshToken;
        }
        else if (expectRefreshToken)
        {
            // Bez refresh tokenu nie ma sensu kontynuować.
            return new GoogleOAuthTokenResult(false, "Missing refresh token in response");
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new GoogleOAuthTokenResult(true, null, entity);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("GoogleOAuth options are not configured");
        }
    }
}

