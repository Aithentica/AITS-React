using AITS.Api.Controllers;
using AITS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace AITS.Tests;

public class AdminTherapistsControllerTests
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

        return (context, userManager, roleManager);
    }

    private static Mock<IUserRoleService> CreateMockUserRoleService(UserManager<ApplicationUser>? userManager = null, RoleManager<IdentityRole>? roleManager = null)
    {
        var mock = new Mock<IUserRoleService>();
        mock.Setup(s => s.AssignRoleAsync(It.IsAny<string>(), It.IsAny<UserRole>(), It.IsAny<string>()))
            .Returns(async (string userId, UserRole role, string? assignedBy) =>
            {
                if (userManager != null && roleManager != null)
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        var roleName = role switch
                        {
                            UserRole.Terapeuta => Roles.Terapeuta,
                            UserRole.TerapeutaFreeAccess => Roles.TerapeutaFreeAccess,
                            UserRole.Administrator => Roles.Administrator,
                            UserRole.Pacjent => Roles.Pacjent,
                            _ => null
                        };
                        if (!string.IsNullOrEmpty(roleName) && await roleManager.RoleExistsAsync(roleName))
                        {
                            if (!await userManager.IsInRoleAsync(user, roleName))
                            {
                                await userManager.AddToRoleAsync(user, roleName);
                            }
                        }
                    }
                }
                return true;
            });
        mock.Setup(s => s.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<UserRole>()))
            .Returns(async (string userId, UserRole role) =>
            {
                if (userManager != null)
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        var roleName = role switch
                        {
                            UserRole.Terapeuta => Roles.Terapeuta,
                            UserRole.TerapeutaFreeAccess => Roles.TerapeutaFreeAccess,
                            UserRole.Administrator => Roles.Administrator,
                            UserRole.Pacjent => Roles.Pacjent,
                            _ => null
                        };
                        if (!string.IsNullOrEmpty(roleName) && await userManager.IsInRoleAsync(user, roleName))
                        {
                            await userManager.RemoveFromRoleAsync(user, roleName);
                        }
                    }
                }
                return true;
            });
        mock.Setup(s => s.GetUserRolesAsync(It.IsAny<string>()))
            .ReturnsAsync(Enumerable.Empty<UserRole>());
        return mock;
    }

    private static AdminTherapistsController CreateControllerWithUser(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager, 
        IUserRoleService userRoleService,
        string userId = "admin-user")
    {
        var controller = new AdminTherapistsController(userManager, roleManager, userRoleService);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, Roles.Administrator)
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

    [Fact]
    public async Task AssignTherapist_ShouldAddRole()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        var controller = CreateControllerWithUser(userManager, roleManager, userRoleServiceMock.Object);

        var user = new ApplicationUser { Email = "therapist@example.com", UserName = "therapist@example.com", LockoutEnabled = true };
        var createResult = await userManager.CreateAsync(user, "Therapist123!");
        Assert.True(createResult.Succeeded);

        var assignRequest = new AdminTherapistsController.AssignTherapistRequest(user.Id, false);
        var result = await controller.AssignTherapist(assignRequest) as IStatusCodeActionResult;
        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);

        var roles = await userManager.GetRolesAsync(user);
        Assert.Contains(Roles.Terapeuta, roles);
        Assert.DoesNotContain(Roles.TerapeutaFreeAccess, roles);
    }

    [Fact]
    public async Task UpdateTherapist_ShouldSwitchToFreeAccess()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        userRoleServiceMock.Setup(s => s.GetUserRolesAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { UserRole.Terapeuta });
        var controller = CreateControllerWithUser(userManager, roleManager, userRoleServiceMock.Object);

        var user = new ApplicationUser { Email = "free@example.com", UserName = "free@example.com", LockoutEnabled = true };
        Assert.True((await userManager.CreateAsync(user, "Free123!")).Succeeded);
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var updateRequest = new AdminTherapistsController.UpdateTherapistRequest(null, null, null, true);
        var result = await controller.UpdateTherapist(user.Id, updateRequest) as IStatusCodeActionResult;
        Assert.NotNull(result);
        Assert.Equal(204, result!.StatusCode);

        var roles = await userManager.GetRolesAsync(user);
        Assert.Contains(Roles.TerapeutaFreeAccess, roles);
        Assert.DoesNotContain(Roles.Terapeuta, roles);
    }

    [Fact]
    public async Task RemoveTherapist_ShouldClearRoles()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        userRoleServiceMock.Setup(s => s.GetUserRolesAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { UserRole.Terapeuta });
        var controller = new AdminTherapistsController(userManager, roleManager, userRoleServiceMock.Object);

        var user = new ApplicationUser { Email = "remove@example.com", UserName = "remove@example.com", LockoutEnabled = true };
        Assert.True((await userManager.CreateAsync(user, "Remove123!")).Succeeded);
        await userManager.AddToRoleAsync(user, Roles.Terapeuta);

        var result = await controller.RemoveTherapist(user.Id) as IStatusCodeActionResult;
        Assert.NotNull(result);
        Assert.Equal(204, result!.StatusCode);

        var roles = await userManager.GetRolesAsync(user);
        Assert.DoesNotContain(Roles.Terapeuta, roles);
        Assert.DoesNotContain(Roles.TerapeutaFreeAccess, roles);
    }
}


