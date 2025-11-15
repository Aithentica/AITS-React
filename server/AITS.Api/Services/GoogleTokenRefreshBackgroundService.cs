using AITS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AITS.Api.Services;

/// <summary>
/// Background service do automatycznego odświeżania tokenów Google OAuth przed ich wygaśnięciem
/// </summary>
public sealed class GoogleTokenRefreshBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GoogleTokenRefreshBackgroundService> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30); // Sprawdzaj co 30 minut
    private readonly TimeSpan _refreshBeforeExpiry = TimeSpan.FromMinutes(15); // Odśwież 15 minut przed wygaśnięciem

    public GoogleTokenRefreshBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<GoogleTokenRefreshBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GoogleTokenRefreshBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleTokenRefreshBackgroundService");
            }

            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("GoogleTokenRefreshBackgroundService stopped");
    }

    private async Task RefreshTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var googleOAuthService = scope.ServiceProvider.GetRequiredService<IGoogleOAuthService>();

        // Oblicz wartość przed zapytaniem (EF Core nie może przetłumaczyć DateTime.Add na SQL)
        var expiryThreshold = DateTime.UtcNow.Add(_refreshBeforeExpiry);
        
        // Znajdź wszystkie tokeny, które wygasną w ciągu najbliższych 15 minut
        var tokensToRefresh = await db.TherapistGoogleTokens
            .Where(t => t.ExpiresAt <= expiryThreshold)
            .ToListAsync(cancellationToken);

        if (tokensToRefresh.Count == 0)
        {
            _logger.LogDebug("No tokens need refreshing");
            return;
        }

        _logger.LogInformation("Refreshing {Count} Google OAuth tokens", tokensToRefresh.Count);

        foreach (var token in tokensToRefresh)
        {
            try
            {
                var result = await googleOAuthService.EnsureValidAccessTokenAsync(token.TerapeutaId, cancellationToken);
                if (result.Success)
                {
                    _logger.LogDebug("Successfully refreshed token for therapist {TherapistId}", token.TerapeutaId);
                }
                else
                {
                    _logger.LogWarning("Failed to refresh token for therapist {TherapistId}: {Error}", 
                        token.TerapeutaId, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while refreshing token for therapist {TherapistId}", token.TerapeutaId);
            }
        }
    }
}


