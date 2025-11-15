using AITS.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AITS.Tests;

public class TherapistProfileControllerTests
{
    private static (AppDbContext Context, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole> RoleManager) BuildManagers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        var identityOptions = new IdentityOptions();
        identityOptions.Password.RequireDigit = true;
        identityOptions.Password.RequireLowercase = true;
        identityOptions.Password.RequireUppercase = true;
        identityOptions.Password.RequireNonAlphanumeric = false;
        identityOptions.Password.RequiredLength = 6;

        var userStore = new UserStore<ApplicationUser>(context);
        var roleStore = new RoleStore<IdentityRole>(context);

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

        var roleManager = new RoleManager<IdentityRole>(
            roleStore,
            new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);

        _ = roleManager.CreateAsync(new IdentityRole(Roles.Terapeuta)).Result;
        _ = roleManager.CreateAsync(new IdentityRole(Roles.TerapeutaFreeAccess)).Result;
        _ = roleManager.CreateAsync(new IdentityRole(Roles.Administrator)).Result;

        return (context, userManager, roleManager);
    }

    private static TherapistProfileController CreateControllerWithUser(AppDbContext context, ApplicationUser user, string role)
    {
        var controller = new TherapistProfileController(context);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    [Fact]
    public async Task CreateProfile_ShouldCreateProfile()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var request = new TherapistProfileController.CreateTherapistProfileRequest(
            "Jan",
            "Kowalski",
            "Firma Sp. z o.o.",
            null, // NIP - pomijamy walidację w teście
            null, // REGON - pomijamy walidację w teście
            "ul. Testowa 1",
            "Warszawa",
            "00-001",
            "Polska",
            true);

        var result = await controller.Create(request, CancellationToken.None) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal(201, (result!.StatusCode ?? 0));

        var profile = await context.TherapistProfiles.FirstOrDefaultAsync(p => p.TherapistId == user.Id);
        Assert.NotNull(profile);
        Assert.Equal("Jan", profile!.FirstName);
        Assert.Equal("Kowalski", profile.LastName);
        Assert.Equal("Firma Sp. z o.o.", profile.CompanyName);
        Assert.True(profile.IsCompany);
    }

    [Fact]
    public async Task GetProfile_ShouldReturnProfile()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var result = await controller.Get(CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldUpdateProfile()
    {
        var (context, userManager, _) = BuildManagers();
        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        await userManager.CreateAsync(user, "Therapist123!");
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var profile = new TherapistProfile
        {
            TherapistId = user.Id,
            FirstName = "Jan",
            LastName = "Kowalski",
            CreatedAt = DateTime.UtcNow
        };
        context.TherapistProfiles.Add(profile);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context, user, Roles.Terapeuta);
        var request = new TherapistProfileController.UpdateTherapistProfileRequest(
            "Jan",
            "Nowak",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false);

        var result = await controller.Update(request, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);

        var updatedProfile = await context.TherapistProfiles.FirstOrDefaultAsync(p => p.TherapistId == user.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal("Nowak", updatedProfile!.LastName);
        Assert.NotNull(updatedProfile.UpdatedAt);
    }
}

