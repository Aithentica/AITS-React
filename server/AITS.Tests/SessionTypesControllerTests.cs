using System;
using System.Linq;
using AITS.Api;
using AITS.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Tests;

public class SessionTypesControllerTests
{
    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Create_ShouldPersistSessionTypeWithTipsAndQuestions()
    {
        await using var context = BuildContext();
        var controller = new SessionTypesController(context);

        var request = new SessionTypesController.UpsertSessionTypeRequest(
            Name: "Sesja relaksacyjna",
            Description: "Sesja nastawiona na ćwiczenia oddechowe",
            IsActive: true,
            IsSystem: null,
            Tips: new[]
            {
                new SessionTypesController.UpsertSessionTypeTipRequest(null, "Pamiętaj o spokojnym oddechu", 1, true)
            },
            Questions: new[]
            {
                new SessionTypesController.UpsertSessionTypeQuestionRequest(null, "Jak się dzisiaj czujesz?", 1, true)
            });

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<SessionTypesController.SessionTypeDto>(created.Value);

        Assert.Equal("Sesja relaksacyjna", dto.Name);
        Assert.Single(dto.Tips);
        Assert.Single(dto.Questions);

        var stored = await context.SessionTypes
            .Include(t => t.Tips)
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == dto.Id);

        Assert.NotNull(stored);
        Assert.Equal("Sesja relaksacyjna", stored!.Name);
        Assert.Equal("Sesja nastawiona na ćwiczenia oddechowe", stored.Description);
        Assert.Single(stored.Tips);
        Assert.Single(stored.Questions);
    }

    [Fact]
    public async Task Update_ShouldSynchronizeTipsAndQuestions()
    {
        await using var context = BuildContext();
        var sessionType = new SessionType
        {
            Name = "Sesja poznawcza",
            Description = "Opis początkowy",
            IsActive = true,
            Tips =
            {
                new SessionTypeTip { Id = 1, Content = "Ustal cel spotkania", DisplayOrder = 1, IsActive = true }
            },
            Questions =
            {
                new SessionTypeQuestion { Id = 1, Content = "Jak oceniasz ostatnie postępy?", DisplayOrder = 1, IsActive = true }
            }
        };
        var existingTipId = sessionType.Tips.Single().Id;
        context.SessionTypes.Add(sessionType);
        await context.SaveChangesAsync();

        var controller = new SessionTypesController(context);

        var updateRequest = new SessionTypesController.UpsertSessionTypeRequest(
            Name: "Sesja poznawcza",
            Description: "Nowy opis",
            IsActive: false,
            IsSystem: null,
            Tips: new[]
            {
                new SessionTypesController.UpsertSessionTypeTipRequest(1, "Zdefiniuj cele spotkania", 2, true),
                new SessionTypesController.UpsertSessionTypeTipRequest(null, "Sprawdź nastrój pacjenta", 3, true)
            },
            Questions: Array.Empty<SessionTypesController.UpsertSessionTypeQuestionRequest>());

        var result = await controller.Update(sessionType.Id, updateRequest);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SessionTypesController.SessionTypeDto>(ok.Value);

        Assert.False(dto.IsActive);
        Assert.Equal("Nowy opis", dto.Description);
        Assert.Equal(2, dto.Tips.Count);
        Assert.Empty(dto.Questions);

        var stored = await context.SessionTypes
            .Include(t => t.Tips)
            .Include(t => t.Questions)
            .FirstAsync(t => t.Id == sessionType.Id);

        Assert.False(stored.IsActive);
        Assert.Equal(2, stored.Tips.Count);
        Assert.Equal("Zdefiniuj cele spotkania", stored.Tips.Single(t => t.DisplayOrder == 2).Content);
        Assert.Contains(stored.Tips, t => t.Id == existingTipId && t.DisplayOrder == 2);
        Assert.Contains(stored.Tips, t => t.Content == "Sprawdź nastrój pacjenta" && t.DisplayOrder == 3);
        Assert.Empty(stored.Questions);
    }

    [Fact]
    public async Task Delete_ShouldReturnBadRequest_WhenSessionTypeHasSessions()
    {
        await using var context = BuildContext();
        var sessionType = new SessionType { Id = 5, Name = "Sesja testowa", IsActive = true };
        var patient = new Patient
        {
            Id = 10,
            FirstName = "Jan",
            LastName = "Kowalski",
            Email = "jan@example.com",
            CreatedByUserId = "user-1",
            CreatedBy = new ApplicationUser { Id = "user-1", Email = "jan@example.com", UserName = "jan@example.com" }
        };

        context.Users.Add(patient.CreatedBy);
        context.Patients.Add(patient);
        context.SessionTypes.Add(sessionType);
        context.Sessions.Add(new Session
        {
            Id = 20,
            PatientId = patient.Id,
            Patient = patient,
            TerapeutaId = "therapist-1",
            Terapeuta = new ApplicationUser { Id = "therapist-1", Email = "therapist@example.com", UserName = "therapist@example.com" },
            SessionTypeId = sessionType.Id,
            SessionType = sessionType,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 100
        });
        await context.SaveChangesAsync();

        var controller = new SessionTypesController(context);

        var result = await controller.Delete(sessionType.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Nie można usunąć", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

