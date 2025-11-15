using System.Security.Claims;
using AITS.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Tests;

public class UserActivityLoggingTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, authenticationType: "Test"))
        };

        return new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Log_ShouldPersistActivityEntry()
    {
        using var context = CreateContext();
        var user = new ApplicationUser { Id = "user-1", Email = "user1@example.com", UserName = "user1@example.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new UserActivityController(context)
        {
            ControllerContext = CreateControllerContext(user.Id)
        };

        var start = DateTime.SpecifyKind(new DateTime(2025, 1, 1, 10, 0, 0), DateTimeKind.Utc);
        var end = start.AddMinutes(5);

        var result = await controller.Log(
            new UserActivityController.LogActivityRequest("/dashboard", start, end),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var persisted = await context.UserActivityLogs.SingleAsync();
        Assert.Equal(user.Id, persisted.UserId);
        Assert.Equal("/dashboard", persisted.Path);
        Assert.Equal(300, persisted.DurationSeconds);
        Assert.Equal(start, persisted.StartedAtUtc);
        Assert.Equal(end, persisted.EndedAtUtc);
    }

    [Fact]
    public async Task Log_ShouldReturnBadRequestWhenPathMissing()
    {
        using var context = CreateContext();
        var user = new ApplicationUser { Id = "user-2", Email = "user2@example.com", UserName = "user2@example.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new UserActivityController(context)
        {
            ControllerContext = CreateControllerContext(user.Id)
        };

        var request = new UserActivityController.LogActivityRequest("   ", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(1));

        var result = await controller.Log(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Ścieżka", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.UserActivityLogs);
    }

    [Fact]
    public async Task GetLogs_ShouldFilterByUserAndDateRange()
    {
        using var context = CreateContext();
        var admin = new ApplicationUser { Id = "admin", Email = "admin@example.com", UserName = "admin@example.com" };
        var userA = new ApplicationUser { Id = "user-a", Email = "usera@example.com", UserName = "usera@example.com" };
        var userB = new ApplicationUser { Id = "user-b", Email = "userb@example.com", UserName = "userb@example.com" };
        context.Users.AddRange(admin, userA, userB);

        var startA = DateTime.SpecifyKind(new DateTime(2025, 1, 2, 9, 0, 0), DateTimeKind.Utc);
        var startB = DateTime.SpecifyKind(new DateTime(2025, 1, 3, 12, 30, 0), DateTimeKind.Utc);

        context.UserActivityLogs.AddRange(
            new UserActivityLog
            {
                UserId = userA.Id,
                Path = "/dashboard",
                StartedAtUtc = startA,
                EndedAtUtc = startA.AddMinutes(2),
                DurationSeconds = 120,
                CreatedAtUtc = startA
            },
            new UserActivityLog
            {
                UserId = userB.Id,
                Path = "/sessions",
                StartedAtUtc = startB,
                EndedAtUtc = startB.AddMinutes(1),
                DurationSeconds = 60,
                CreatedAtUtc = startB
            });

        await context.SaveChangesAsync();

        var controller = new AdminActivityController(context)
        {
            ControllerContext = CreateControllerContext(admin.Id)
        };

        var okResult = await controller.GetLogs(
            userId: userA.Id,
            fromUtc: startA.AddMinutes(-10),
            toUtc: startA.AddMinutes(10),
            page: 1,
            pageSize: 10,
            CancellationToken.None) as OkObjectResult;

        Assert.NotNull(okResult);
        var payload = Assert.IsType<AdminActivityController.ActivityLogPageDto>(okResult!.Value);
        Assert.Equal(1, payload.TotalCount);
        Assert.Equal(120, payload.TotalDurationSeconds);
        var item = Assert.Single(payload.Items);
        Assert.Equal(userA.Id, item.UserId);
        Assert.Equal("/dashboard", item.Path);
        Assert.Equal(120, item.DurationSeconds);
        Assert.Equal(startA, item.StartedAtUtc);
    }
}
