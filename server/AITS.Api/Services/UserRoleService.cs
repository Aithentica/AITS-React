using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services.Interfaces;

namespace AITS.Api.Services;

public sealed class UserRoleService : IUserRoleService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserRoleService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> AssignRoleAsync(string userId, UserRole role, string? assignedByUserId = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Sprawdź czy rola już istnieje
        var existingMapping = await _db.UserRoleMappings
            .FirstOrDefaultAsync(urm => urm.UserId == userId && urm.RoleId == (int)role);

        if (existingMapping != null)
        {
            // Aktywuj istniejącą rolę jeśli była nieaktywna
            if (!existingMapping.IsActive)
            {
                existingMapping.IsActive = true;
                existingMapping.AssignedAt = DateTime.UtcNow;
                existingMapping.AssignedByUserId = assignedByUserId;
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            // Utwórz nowe mapowanie
            var mapping = new UserRoleMapping
            {
                UserId = userId,
                RoleId = (int)role,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = assignedByUserId,
                IsActive = true
            };
            _db.UserRoleMappings.Add(mapping);
            await _db.SaveChangesAsync();
        }

        // Synchronizuj z ASP.NET Identity
        var roleName = GetRoleName(role);
        if (!string.IsNullOrEmpty(roleName) && await _roleManager.RoleExistsAsync(roleName))
        {
            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }

        // Upewnij się że użytkownik ma odpowiedni profil
        await EnsureUserProfileExistsAsync(userId);

        return true;
    }

    public async Task<bool> RemoveRoleAsync(string userId, UserRole role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var mapping = await _db.UserRoleMappings
            .FirstOrDefaultAsync(urm => urm.UserId == userId && urm.RoleId == (int)role && urm.IsActive);

        if (mapping != null)
        {
            mapping.IsActive = false;
            await _db.SaveChangesAsync();
        }

        // Usuń z ASP.NET Identity
        var roleName = GetRoleName(role);
        if (!string.IsNullOrEmpty(roleName) && await _userManager.IsInRoleAsync(user, roleName))
        {
            await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        return true;
    }

    public async Task<IEnumerable<UserRole>> GetUserRolesAsync(string userId)
    {
        var mappings = await _db.UserRoleMappings
            .Where(urm => urm.UserId == userId && urm.IsActive)
            .Select(urm => (UserRole)urm.RoleId)
            .ToListAsync();

        return mappings;
    }

    public async Task<bool> HasRoleAsync(string userId, UserRole role)
    {
        return await _db.UserRoleMappings
            .AnyAsync(urm => urm.UserId == userId && urm.RoleId == (int)role && urm.IsActive);
    }

    public async Task EnsureUserProfileExistsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        var roles = await GetUserRolesAsync(userId);

        // Jeśli użytkownik ma rolę Terapeuta, upewnij się że ma TherapistProfile
        if (roles.Contains(UserRole.Terapeuta) || roles.Contains(UserRole.TerapeutaFreeAccess))
        {
            var hasProfile = await _db.TherapistProfiles.AnyAsync(p => p.TherapistId == userId);
            if (!hasProfile)
            {
                // Utwórz pusty profil - użytkownik będzie musiał go uzupełnić
                _db.TherapistProfiles.Add(new TherapistProfile
                {
                    TherapistId = userId,
                    FirstName = user.UserName ?? user.Email ?? "",
                    LastName = "",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }

        // Jeśli użytkownik ma rolę Pacjent, upewnij się że ma Patient
        if (roles.Contains(UserRole.Pacjent))
        {
            var hasPatient = await _db.Patients.AnyAsync(p => p.UserId == userId);
            if (!hasPatient)
            {
                // Utwórz pusty rekord Patient - użytkownik będzie musiał go uzupełnić
                _db.Patients.Add(new Patient
                {
                    UserId = userId,
                    FirstName = user.UserName ?? user.Email ?? "",
                    LastName = "",
                    Email = user.Email ?? "",
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task SyncRolesFromIdentityAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        var identityRoles = await _userManager.GetRolesAsync(user);
        var currentMappings = await _db.UserRoleMappings
            .Where(urm => urm.UserId == userId)
            .ToListAsync();

        // Dezaktywuj wszystkie istniejące mapowania
        foreach (var mapping in currentMappings)
        {
            mapping.IsActive = false;
        }

        // Aktywuj mapowania dla ról z Identity
        foreach (var roleName in identityRoles)
        {
            var role = GetRoleFromName(roleName);
            if (role.HasValue)
            {
                var mapping = currentMappings.FirstOrDefault(m => m.RoleId == (int)role.Value);
                if (mapping != null)
                {
                    mapping.IsActive = true;
                }
                else
                {
                    _db.UserRoleMappings.Add(new UserRoleMapping
                    {
                        UserId = userId,
                        RoleId = (int)role.Value,
                        AssignedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    private static string GetRoleName(UserRole role)
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

    private static UserRole? GetRoleFromName(string roleName)
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
}

