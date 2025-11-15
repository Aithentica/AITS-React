using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services.Interfaces;
using System.Security.Claims;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "IsAdministrator")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUserRoleService _userRoleService;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        IUserRoleService userRoleService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userRoleService = userRoleService;
    }

    public sealed record AdminUserDto(
        string Id,
        string Email,
        string? UserName,
        string? PhoneNumber,
        IReadOnlyList<string> Roles,
        bool IsLockedOut);

    public sealed record CreateAdminUserRequest(
        string Email,
        string Password,
        IReadOnlyList<string>? Roles);

    public sealed record UpdateRolesRequest(IReadOnlyList<string> Roles);

    public sealed record UpdateUserRequest(
        string? Email,
        string? UserName,
        string? PhoneNumber);

    public sealed record LockUserRequest(bool Lock);

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<AdminUserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
            result.Add(new AdminUserDto(
                user.Id,
                user.Email ?? user.UserName ?? user.Id,
                user.UserName,
                user.PhoneNumber,
                roles.ToImmutableArray(),
                isLocked));
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email i hasło są wymagane." });
        }

        var normalizedRoles = (request.Roles?.Count ?? 0) == 0
            ? new List<string> { Roles.Pacjent }
            : request.Roles!.Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (var role in normalizedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest(new { error = $"Rola '{role}' nie istnieje." });
            }
        }

        var user = new ApplicationUser
        {
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            LockoutEnabled = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                error = "Nie udało się utworzyć użytkownika.",
                details = createResult.Errors.Select(e => e.Description).ToArray()
            });
        }

        // Przypisz role używając IUserRoleService dla synchronizacji
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        foreach (var roleName in normalizedRoles)
        {
            var role = GetUserRoleFromName(roleName);
            if (role.HasValue)
            {
                var assigned = await _userRoleService.AssignRoleAsync(user.Id, role.Value, currentUserId);
                if (!assigned)
                {
                    return BadRequest(new
                    {
                        error = $"Nie udało się przypisać roli '{roleName}'.",
                    });
                }
            }
        }

        return CreatedAtAction(nameof(GetUsers), new { userId = user.Id }, new { user.Id, user.Email, Roles = normalizedRoles });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, [FromBody] UpdateRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new { error = "Nie znaleziono użytkownika." });
        }

        var requestedRoles = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var role in requestedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest(new { error = $"Rola '{role}' nie istnieje." });
            }
        }

        // Pobierz aktualne role użytkownika z UserRoleService
        var currentUserRoles = await _userRoleService.GetUserRolesAsync(user.Id);
        var currentRoleNames = currentUserRoles.Select(r => GetRoleNameFromUserRole(r)).ToList();

        // Usuń role, które nie są w żądaniu
        var toRemove = currentRoleNames
            .Where(role => !requestedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToList();
        foreach (var roleName in toRemove)
        {
            var role = GetUserRoleFromName(roleName);
            if (role.HasValue)
            {
                await _userRoleService.RemoveRoleAsync(user.Id, role.Value);
            }
        }

        // Dodaj nowe role używając IUserRoleService
        var toAdd = requestedRoles
            .Where(role => !currentRoleNames.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        foreach (var roleName in toAdd)
        {
            var role = GetUserRoleFromName(roleName);
            if (role.HasValue)
            {
                var assigned = await _userRoleService.AssignRoleAsync(user.Id, role.Value, currentUserId);
                if (!assigned)
                {
                    return BadRequest(new
                    {
                        error = $"Nie udało się przypisać roli '{roleName}'.",
                    });
                }
            }
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new { error = "Nie znaleziono użytkownika." });
        }

        // Aktualizacja danych użytkownika
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            user.UserName = request.UserName.Trim();
        }

        if (request.PhoneNumber != user.PhoneNumber)
        {
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new { error = "Nie udało się zaktualizować danych użytkownika.", details = updateResult.Errors.Select(e => e.Description).ToArray() });
        }

        return NoContent();
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> ToggleLock(string id, [FromBody] LockUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new { error = "Nie znaleziono użytkownika." });
        }

        if (request.Lock)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        return NoContent();
    }

    private static UserRole? GetUserRoleFromName(string roleName)
    {
        return roleName switch
        {
            Roles.Administrator => UserRole.Administrator,
            Roles.Terapeuta => UserRole.Terapeuta,
            Roles.TerapeutaFreeAccess => UserRole.TerapeutaFreeAccess,
            Roles.Pacjent => UserRole.Pacjent,
            _ => null
        };
    }

    private static string GetRoleNameFromUserRole(UserRole role)
    {
        return role switch
        {
            UserRole.Administrator => Roles.Administrator,
            UserRole.Terapeuta => Roles.Terapeuta,
            UserRole.TerapeutaFreeAccess => Roles.TerapeutaFreeAccess,
            UserRole.Pacjent => Roles.Pacjent,
            _ => string.Empty
        };
    }
}


