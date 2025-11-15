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

public class AdminUsersControllerTests
{
    private static (AppDbContext Context, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole> RoleManager) BuildManagers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        var identityOptions = new IdentityOptions();
        identityOptions.Lockout.AllowedForNewUsers = true;
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

        // Ensure required roles exist
        _ = roleManager.CreateAsync(new IdentityRole(Roles.Administrator)).Result;
        _ = roleManager.CreateAsync(new IdentityRole(Roles.Pacjent)).Result;
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

    private static AdminUsersController CreateControllerWithUser(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager, 
        IUserRoleService userRoleService,
        string userId = "admin-user")
    {
        var controller = new AdminUsersController(userManager, roleManager, userRoleService);
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
    public async Task GetUsers_ShouldReturnExistingUsers()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        var controller = new AdminUsersController(userManager, roleManager, userRoleServiceMock.Object);

        var user = new ApplicationUser { Email = "test@example.com", UserName = "test@example.com", LockoutEnabled = true };
        var createResult = await userManager.CreateAsync(user, "Test123!");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(user, Roles.Pacjent);

        var response = await controller.GetUsers(CancellationToken.None) as IStatusCodeActionResult;
        Assert.NotNull(response);
        Assert.Equal(200, response!.StatusCode);

        var ok = Assert.IsType<OkObjectResult>(response);
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminUsersController.AdminUserDto>>(ok.Value);
        Assert.Single(payload);
        Assert.Contains(payload, dto => dto.Email == "test@example.com" && dto.Roles.Contains(Roles.Pacjent));
    }

    [Fact]
    public async Task CreateUser_ShouldPersistWithRoles()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        var controller = CreateControllerWithUser(userManager, roleManager, userRoleServiceMock.Object);

        var request = new AdminUsersController.CreateAdminUserRequest(
            Email: "new@example.com",
            Password: "Secure123!",
            Roles: new List<string> { Roles.Terapeuta });

        var result = await controller.CreateUser(request, CancellationToken.None) as IStatusCodeActionResult;
        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);

        var user = await userManager.FindByEmailAsync("new@example.com");
        Assert.NotNull(user);
        var roles = await userManager.GetRolesAsync(user!);
        Assert.Contains(Roles.Terapeuta, roles);
    }

    [Fact]
    public async Task UpdateRoles_ShouldReplaceExistingRoles()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        userRoleServiceMock.Setup(s => s.GetUserRolesAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { UserRole.Pacjent });
        var controller = CreateControllerWithUser(userManager, roleManager, userRoleServiceMock.Object);

        var user = new ApplicationUser { Email = "edit@example.com", UserName = "edit@example.com", LockoutEnabled = true };
        var createResult = await userManager.CreateAsync(user, "Edit123!");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(user, Roles.Pacjent);

        var request = new AdminUsersController.UpdateRolesRequest(new List<string> { Roles.Administrator });
        var result = await controller.UpdateRoles(user.Id, request) as IStatusCodeActionResult;
        Assert.NotNull(result);
        Assert.Equal(204, result!.StatusCode);

        var roles = await userManager.GetRolesAsync(user);
        Assert.Single(roles);
        Assert.Contains(Roles.Administrator, roles);
        Assert.DoesNotContain(Roles.Pacjent, roles);
    }

    [Fact]
    public async Task ToggleLock_ShouldLockAndUnlockUser()
    {
        var (context, userManager, roleManager) = BuildManagers();
        var userRoleServiceMock = CreateMockUserRoleService(userManager, roleManager);
        var controller = new AdminUsersController(userManager, roleManager, userRoleServiceMock.Object);

        var user = new ApplicationUser { Email = "lock@example.com", UserName = "lock@example.com", LockoutEnabled = true };
        var createResult = await userManager.CreateAsync(user, "Lock123!");
        Assert.True(createResult.Succeeded);

        var lockRequest = new AdminUsersController.LockUserRequest(true);
        var lockResult = await controller.ToggleLock(user.Id, lockRequest) as IStatusCodeActionResult;
        Assert.NotNull(lockResult);
        Assert.Equal(204, lockResult!.StatusCode);

        user = await userManager.FindByIdAsync(user.Id) ?? throw new InvalidOperationException();
        Assert.True(user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow);

        var unlockRequest = new AdminUsersController.LockUserRequest(false);
        var unlockResult = await controller.ToggleLock(user.Id, unlockRequest) as IStatusCodeActionResult;
        Assert.NotNull(unlockResult);
        Assert.Equal(204, unlockResult!.StatusCode);

        user = await userManager.FindByIdAsync(user.Id) ?? throw new InvalidOperationException();
        Assert.False(user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow);
    }
}


