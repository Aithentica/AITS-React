using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public sealed record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized();

        var valid = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!valid.Succeeded)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = jwt, roles });
    }

    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin()
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = [Roles.Administrator, Roles.Pacjent, Roles.Terapeuta, Roles.TerapeutaFreeAccess];
        var createdRoles = new List<string>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                createdRoles.Add(role);
            }
        }

        var adminEmail = "admin@aits.local";
        var admin = await _userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
            var result = await _userManager.CreateAsync(admin, "Admin123!");
            if (!result.Succeeded)
                return BadRequest(new { 
                    message = "Błąd tworzenia użytkownika", 
                    errors = result.Errors.Select(e => e.Description).ToArray() 
                });
            await _userManager.AddToRoleAsync(admin, Roles.Administrator);
            return Ok(new { 
                message = $"Admin utworzony: {adminEmail} / Admin123!", 
                userId = admin.Id,
                rolesCreated = createdRoles 
            });
        }
        return Ok(new { 
            message = $"Admin już istnieje: {adminEmail}", 
            userId = admin.Id,
            rolesCreated = createdRoles 
        });
    }

    [HttpPost("seed-users")]
    public async Task<IActionResult> SeedUsers()
    {
        var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
        string[] allRoles = [Roles.Administrator, Roles.Pacjent, Roles.Terapeuta, Roles.TerapeutaFreeAccess];
        
        // Upewnij się, że wszystkie role istnieją
        foreach (var role in allRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var users = new[]
        {
            new { Email = "admin@aits.local", Password = "Admin123!", Role = Roles.Administrator },
            new { Email = "pacjent@aits.local", Password = "Pacjent123!", Role = Roles.Pacjent },
            new { Email = "terapeuta@aits.local", Password = "Terapeuta123!", Role = Roles.Terapeuta },
            new { Email = "terapeuta.free@aits.local", Password = "TerapeutaFree123!", Role = Roles.TerapeutaFreeAccess }
        };

        var results = new List<object>();

        foreach (var userInfo in users)
        {
            var existingUser = await _userManager.FindByEmailAsync(userInfo.Email);
            if (existingUser is null)
            {
                var user = new ApplicationUser { UserName = userInfo.Email, Email = userInfo.Email };
                var result = await _userManager.CreateAsync(user, userInfo.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, userInfo.Role);
                    results.Add(new { 
                        email = userInfo.Email, 
                        password = userInfo.Password, 
                        role = userInfo.Role, 
                        status = "utworzony",
                        userId = user.Id
                    });
                }
                else
                {
                    results.Add(new { 
                        email = userInfo.Email, 
                        role = userInfo.Role, 
                        status = "błąd",
                        errors = result.Errors.Select(e => e.Description).ToArray()
                    });
                }
            }
            else
            {
                results.Add(new { 
                    email = userInfo.Email, 
                    role = userInfo.Role, 
                    status = "już istnieje",
                    userId = existingUser.Id
                });
            }
        }

        return Ok(new { message = "Seeding użytkowników zakończony", users = results });
    }
}

