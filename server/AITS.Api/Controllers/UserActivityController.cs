using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/activity")]
[Authorize]
public sealed class UserActivityController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UserActivityController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public sealed record LogActivityRequest(string Path, DateTime StartedAtUtc, DateTime EndedAtUtc);

    [HttpPost]
    public async Task<IActionResult> Log([FromBody] LogActivityRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (request is null)
        {
            return BadRequest(new { error = "Brak danych aktywności." });
        }

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return BadRequest(new { error = "Ścieżka jest wymagana." });
        }

        var sanitizedPath = request.Path.Trim();
        if (sanitizedPath.Length > 256)
        {
            sanitizedPath = sanitizedPath[..256];
        }

        var startedAt = EnsureUtc(request.StartedAtUtc);
        var endedAt = EnsureUtc(request.EndedAtUtc);

        if (endedAt < startedAt)
        {
            endedAt = startedAt;
        }

        var durationSeconds = (int)Math.Round((endedAt - startedAt).TotalSeconds, MidpointRounding.AwayFromZero);
        if (durationSeconds < 1)
        {
            durationSeconds = 1;
        }

        durationSeconds = Math.Clamp(durationSeconds, 1, 60 * 60 * 12);

        var log = new UserActivityLog
        {
            UserId = userId,
            Path = sanitizedPath,
            StartedAtUtc = startedAt,
            EndedAtUtc = endedAt,
            DurationSeconds = durationSeconds,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.UserActivityLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
