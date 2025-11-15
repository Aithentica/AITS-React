using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/integrations/google-calendar")]
[Authorize(Policy = "IsTherapistOrAdmin")]
public sealed class IntegrationsController : ControllerBase
{
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly GoogleOAuthOptions _oAuthOptions;
    private readonly byte[] _stateKey;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        IGoogleOAuthService googleOAuthService,
        IOptions<GoogleOAuthOptions> oAuthOptions,
        IConfiguration configuration,
        ILogger<IntegrationsController> logger)
    {
        _googleOAuthService = googleOAuthService;
        _oAuthOptions = oAuthOptions.Value;
        var stateSecret = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
        _stateKey = Encoding.UTF8.GetBytes(stateSecret);
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult BeginLogin([FromQuery] string? callbackUri = null, [FromQuery] string? returnUrl = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User id not found");

        var redirectUri = string.IsNullOrWhiteSpace(callbackUri)
            ? _oAuthOptions.DefaultRedirectUri ?? BuildDefaultCallbackUri()
            : callbackUri;

        var state = ProtectState(userId, returnUrl);
        var authorizationUrl = _googleOAuthService.BuildAuthorizationUrl(userId, redirectUri, state);

        return Ok(new { authorizationUrl });
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? redirectUri,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Google OAuth returned error: {Error}", error);
            return BuildRedirectResult(null, false, error);
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return BuildRedirectResult(null, false, "Missing code/state");
        }

        if (!TryUnprotectState(state, out var therapistId, out var returnUrl))
        {
            _logger.LogWarning("Google OAuth state validation failed");
            return BuildRedirectResult(null, false, "Invalid state");
        }

        var finalRedirect = string.IsNullOrWhiteSpace(redirectUri)
            ? _oAuthOptions.DefaultRedirectUri ?? BuildDefaultCallbackUri()
            : redirectUri;

        var exchangeResult = await _googleOAuthService.ExchangeCodeAsync(therapistId, code, finalRedirect, cancellationToken);
        if (!exchangeResult.Success)
        {
            return BuildRedirectResult(returnUrl, false, exchangeResult.Error ?? "Google token exchange failed");
        }

        return BuildRedirectResult(returnUrl, true, null);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User id not found");
        var token = await _googleOAuthService.GetTokenAsync(userId, cancellationToken);
        if (token is null)
        {
            return Ok(new { connected = false });
        }

        return Ok(new
        {
            connected = true,
            expiresAt = token.ExpiresAt,
            scope = token.Scope
        });
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User id not found");
        var removed = await _googleOAuthService.DisconnectAsync(userId, cancellationToken);
        return Ok(new { disconnected = removed });
    }

    private string BuildDefaultCallbackUri()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}/api/integrations/google-calendar/callback";
    }

    private string ProtectState(string userId, string? returnUrl)
    {
        var payload = JsonSerializer.Serialize(new StatePayload(userId, returnUrl));
        var signatureBytes = ComputeSignature(payload);
        var envelope = new StateEnvelope(payload, WebEncoders.Base64UrlEncode(signatureBytes));
        var json = JsonSerializer.Serialize(envelope);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private bool TryUnprotectState(string state, out string therapistId, out string? returnUrl)
    {
        therapistId = string.Empty;
        returnUrl = null;

        try
        {
            var json = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(state));
            var envelope = JsonSerializer.Deserialize<StateEnvelope>(json);
            if (envelope is null)
            {
                return false;
            }

            var expectedSignature = WebEncoders.Base64UrlEncode(ComputeSignature(envelope.Payload));
            var providedSignatureBytes = WebEncoders.Base64UrlDecode(envelope.Signature);
            var expectedSignatureBytes = WebEncoders.Base64UrlDecode(expectedSignature);

            if (providedSignatureBytes.Length != expectedSignatureBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(providedSignatureBytes, expectedSignatureBytes))
            {
                return false;
            }

            var payload = JsonSerializer.Deserialize<StatePayload>(envelope.Payload);
            if (payload is null || string.IsNullOrWhiteSpace(payload.UserId))
            {
                return false;
            }

            therapistId = payload.UserId;
            returnUrl = string.IsNullOrWhiteSpace(payload.ReturnUrl) ? null : payload.ReturnUrl;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unprotect Google OAuth state");
            return false;
        }
    }

    private byte[] ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_stateKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private IActionResult BuildRedirectResult(string? returnUrl, bool success, string? error)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (Uri.TryCreate(returnUrl, UriKind.Relative, out _))
            {
                var query = new Dictionary<string, string?>
                {
                    ["googleCalendar"] = success ? "connected" : "error"
                };
                if (!success && !string.IsNullOrWhiteSpace(error))
                {
                    query["message"] = error;
                }

                var target = QueryHelpers.AddQueryString(returnUrl, query);
                return Redirect(target);
            }

            if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute) &&
                absolute.Host.Equals(HttpContext.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                var query = new Dictionary<string, string?>
                {
                    ["googleCalendar"] = success ? "connected" : "error"
                };
                if (!success && !string.IsNullOrWhiteSpace(error))
                {
                    query["message"] = error;
                }

                var target = QueryHelpers.AddQueryString(absolute.ToString(), query);
                return Redirect(target);
            }
        }

        if (success)
        {
            return Ok(new { success = true });
        }

        return BadRequest(new { success = false, error = error ?? "Unknown error" });
    }

    private sealed record StatePayload(string UserId, string? ReturnUrl);
    private sealed record StateEnvelope(string Payload, string Signature);
}

