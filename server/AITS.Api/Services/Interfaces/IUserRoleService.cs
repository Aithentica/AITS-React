namespace AITS.Api.Services.Interfaces;

public interface IUserRoleService
{
    Task<bool> AssignRoleAsync(string userId, UserRole role, string? assignedByUserId = null);
    Task<bool> RemoveRoleAsync(string userId, UserRole role);
    Task<IEnumerable<UserRole>> GetUserRolesAsync(string userId);
    Task<bool> HasRoleAsync(string userId, UserRole role);
    Task EnsureUserProfileExistsAsync(string userId);
    Task SyncRolesFromIdentityAsync(string userId);
}

