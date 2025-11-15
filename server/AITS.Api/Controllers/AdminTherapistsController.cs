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
[Route("api/admin/therapists")]
[Authorize(Policy = "IsAdministrator")]
public sealed class AdminTherapistsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUserRoleService _userRoleService;

    public AdminTherapistsController(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        IUserRoleService userRoleService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userRoleService = userRoleService;
    }

    public sealed record TherapistDto(
        string UserId,
        string Email,
        string? UserName,
        string? PhoneNumber,
        bool FreeAccess,
        IReadOnlyList<string> Roles);

    public sealed record AssignTherapistRequest(string UserId, bool FreeAccess);

    public sealed record UpdateTherapistRequest(
        string? Email,
        string? UserName,
        string? PhoneNumber,
        bool FreeAccess);

    [HttpGet]
    public async Task<IActionResult> GetTherapists(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<TherapistDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(Roles.Terapeuta) && !roles.Contains(Roles.TerapeutaFreeAccess))
            {
                continue;
            }

            var freeAccess = roles.Contains(Roles.TerapeutaFreeAccess);
            result.Add(new TherapistDto(
                user.Id,
                user.Email ?? user.UserName ?? user.Id,
                user.UserName,
                user.PhoneNumber,
                freeAccess,
                roles.ToImmutableArray()));
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AssignTherapist([FromBody] AssignTherapistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(new { error = "Wskaż użytkownika do przypisania." });
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return NotFound(new { error = "Nie znaleziono użytkownika." });
        }

        await EnsureRolesExistAsync();
        
        // Usuń istniejące role terapeuty używając IUserRoleService
        await RemoveTherapistRoles(user);

        // Przypisz nową rolę terapeuty używając IUserRoleService (automatycznie utworzy TherapistProfile)
        var userRole = request.FreeAccess ? UserRole.TerapeutaFreeAccess : UserRole.Terapeuta;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var assigned = await _userRoleService.AssignRoleAsync(user.Id, userRole, currentUserId);
        if (!assigned)
        {
            return BadRequest(new { error = "Nie udało się przypisać roli terapeuty." });
        }

        var roleName = request.FreeAccess ? Roles.TerapeutaFreeAccess : Roles.Terapeuta;
        return CreatedAtAction(nameof(GetTherapists), new { userId = user.Id }, new { user.Id, role = roleName });
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateTherapist(string userId, [FromBody] UpdateTherapistRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
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

        // Aktualizacja roli terapeuty używając IUserRoleService
        await EnsureRolesExistAsync();
        await RemoveTherapistRoles(user);

        var userRole = request.FreeAccess ? UserRole.TerapeutaFreeAccess : UserRole.Terapeuta;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var assigned = await _userRoleService.AssignRoleAsync(user.Id, userRole, currentUserId);
        if (!assigned)
        {
            return BadRequest(new { error = "Nie udało się zaktualizować roli terapeuty." });
        }

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveTherapist(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { error = "Nie znaleziono użytkownika." });
        }

        await RemoveTherapistRoles(user);
        return NoContent();
    }

    private async Task EnsureRolesExistAsync()
    {
        if (!await _roleManager.RoleExistsAsync(Roles.Terapeuta))
        {
            await _roleManager.CreateAsync(new IdentityRole(Roles.Terapeuta));
        }
        if (!await _roleManager.RoleExistsAsync(Roles.TerapeutaFreeAccess))
        {
            await _roleManager.CreateAsync(new IdentityRole(Roles.TerapeutaFreeAccess));
        }
    }

    private async Task RemoveTherapistRoles(ApplicationUser user)
    {
        // Usuń role terapeuty używając IUserRoleService
        var userRoles = await _userRoleService.GetUserRolesAsync(user.Id);
        foreach (var role in userRoles)
        {
            if (role == UserRole.Terapeuta || role == UserRole.TerapeutaFreeAccess)
            {
                await _userRoleService.RemoveRoleAsync(user.Id, role);
            }
        }
    }
}


