using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/admin/activity")]
[Authorize(Policy = "IsAdministrator")]
public sealed class AdminActivityController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminActivityController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public sealed record ActivityLogItemDto(
        int Id,
        string UserId,
        string? UserEmail,
        string Path,
        DateTime StartedAtUtc,
        DateTime EndedAtUtc,
        int DurationSeconds,
        DateTime CreatedAtUtc);

    public sealed record ActivityLogPageDto(
        IReadOnlyList<ActivityLogItemDto> Items,
        int TotalCount,
        long TotalDurationSeconds,
        int Page,
        int PageSize,
        DateTime FromUtc,
        DateTime ToUtc);

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? userId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _dbContext.UserActivityLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(x => x.UserId == userId);
        }

        var defaultFrom = DateTime.UtcNow.AddDays(-30);
        var effectiveFrom = fromUtc.HasValue ? EnsureUtc(fromUtc.Value) : defaultFrom;
        var effectiveTo = toUtc.HasValue ? EnsureUtc(toUtc.Value) : DateTime.UtcNow.AddMinutes(1);

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        query = query.Where(x => x.StartedAtUtc >= effectiveFrom && x.StartedAtUtc <= effectiveTo);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalDuration = await query.SumAsync(x => (long?)x.DurationSeconds, cancellationToken).ConfigureAwait(false) ?? 0L;

        var items = await query
            .Include(x => x.User)
            .OrderByDescending(x => x.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemsDto = items.Select(x => new ActivityLogItemDto(
            x.Id,
            x.UserId,
            x.User != null ? (x.User.Email ?? x.User.UserName ?? x.UserId) : null,
            x.Path,
            x.StartedAtUtc,
            x.EndedAtUtc,
            x.DurationSeconds,
            x.CreatedAtUtc))
            .ToList();

        return Ok(new ActivityLogPageDto(itemsDto, totalCount, totalDuration, page, pageSize, effectiveFrom, effectiveTo));
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
