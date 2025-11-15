using AITS.Api.Controllers;
using AITS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace AITS.Tests;

public class PatientsControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkResult_WhenPatientsExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var user = new ApplicationUser { Id = "test-user", UserName = "test@test.com", Email = "test@test.com" };
        db.Users.Add(user);
        db.Patients.Add(new Patient { Id = 1, FirstName = "Jan", LastName = "Kowalski", Email = "jan@test.com", CreatedByUserId = "test-user" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, "test-user");

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var patients = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Single(patients);
    }

    [Fact]
    public async Task Create_ShouldPersistInformationEntries_ForAllActiveTypes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new ApplicationUser { Id = "creator", UserName = "creator@test.com", Email = "creator@test.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db, user.Id);
        var types = await db.PatientInformationTypes.OrderBy(t => t.DisplayOrder).ToListAsync();
        Assert.NotEmpty(types);
        Assert.True(types.Count >= 2);

        var request = new PatientsController.CreatePatientRequest(
            FirstName: "Anna",
            LastName: "Nowak",
            Email: "anna@test.com",
            Phone: null,
            DateOfBirth: null,
            Gender: null,
            Pesel: null,
            Street: null,
            StreetNumber: null,
            ApartmentNumber: null,
            City: null,
            PostalCode: null,
            Country: "Polska",
            LastSessionSummary: "Podsumowanie",
            InformationEntries: new[]
            {
                new PatientsController.PatientInformationEntryRequest(types[0].Id, "Wartość 1"),
                new PatientsController.PatientInformationEntryRequest(types[1].Id, "Wartość 2")
            });

        var result = await controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PatientsController.Get), createdResult.ActionName);

        var patient = await db.Patients
            .Include(p => p.InformationEntries)
            .FirstAsync();

        Assert.Equal("Podsumowanie", patient.LastSessionSummary);
        Assert.Equal(types.Count, patient.InformationEntries.Count);
        Assert.Contains(patient.InformationEntries, entry => entry.PatientInformationTypeId == types[0].Id && entry.Content == "Wartość 1");
        Assert.Contains(patient.InformationEntries, entry => entry.PatientInformationTypeId == types[1].Id && entry.Content == "Wartość 2");
    }

    [Fact]
    public async Task Update_ShouldUpdateLastSessionSummary_AndSelectedInformationEntries()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new ApplicationUser { Id = "author", UserName = "author@test.com", Email = "author@test.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateController(db, user.Id);
        var types = await db.PatientInformationTypes.OrderBy(t => t.DisplayOrder).ToListAsync();
        Assert.True(types.Count >= 2);

        var createRequest = new PatientsController.CreatePatientRequest(
            FirstName: "Anna",
            LastName: "Nowak",
            Email: "anna@test.com",
            Phone: null,
            DateOfBirth: null,
            Gender: null,
            Pesel: null,
            Street: null,
            StreetNumber: null,
            ApartmentNumber: null,
            City: null,
            PostalCode: null,
            Country: "Polska",
            LastSessionSummary: "Podsumowanie",
            InformationEntries: new[]
            {
                new PatientsController.PatientInformationEntryRequest(types[0].Id, "Wartość 1"),
                new PatientsController.PatientInformationEntryRequest(types[1].Id, "Wartość 2")
            });

        await controller.Create(createRequest);

        var patient = await db.Patients
            .Include(p => p.InformationEntries)
            .FirstAsync();

        var updateRequest = new PatientsController.UpdatePatientRequest(
            FirstName: "Anna",
            LastName: "Nowak",
            Email: "anna@test.com",
            Phone: null,
            DateOfBirth: null,
            Gender: null,
            Pesel: null,
            Street: null,
            StreetNumber: null,
            ApartmentNumber: null,
            City: null,
            PostalCode: null,
            Country: "Polska",
            LastSessionSummary: "Nowe podsumowanie",
            InformationEntries: new[]
            {
                new PatientsController.PatientInformationEntryRequest(types[0].Id, "Zaktualizowana wartość")
            });

        var updateResult = await controller.Update(patient.Id, updateRequest);

        Assert.IsType<OkObjectResult>(updateResult);

        var updatedPatient = await db.Patients
            .Include(p => p.InformationEntries)
            .FirstAsync(p => p.Id == patient.Id);

        Assert.Equal("Nowe podsumowanie", updatedPatient.LastSessionSummary);
        var updatedEntry = updatedPatient.InformationEntries.First(entry => entry.PatientInformationTypeId == types[0].Id);
        Assert.Equal("Zaktualizowana wartość", updatedEntry.Content);
        var untouchedEntry = updatedPatient.InformationEntries.First(entry => entry.PatientInformationTypeId == types[1].Id);
        Assert.Equal("Wartość 2", untouchedEntry.Content);
    }

    private static PatientsController CreateController(AppDbContext db, string userId)
    {
        var identityOptions = new IdentityOptions();
        identityOptions.Password.RequireDigit = true;
        identityOptions.Password.RequireLowercase = true;
        identityOptions.Password.RequireUppercase = true;
        identityOptions.Password.RequireNonAlphanumeric = false;
        identityOptions.Password.RequiredLength = 6;

        var userStore = new UserStore<ApplicationUser>(db);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(identityOptions),
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
            new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var userRoleServiceMock = new Mock<IUserRoleService>();
        userRoleServiceMock.Setup(s => s.AssignRoleAsync(It.IsAny<string>(), It.IsAny<UserRole>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        userRoleServiceMock.Setup(s => s.GetUserRolesAsync(It.IsAny<string>()))
            .ReturnsAsync(Enumerable.Empty<UserRole>());

        var controller = new PatientsController(db, userManager, userRoleServiceMock.Object);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Terapeuta")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return controller;
    }
}




