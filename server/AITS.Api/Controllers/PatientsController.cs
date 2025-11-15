using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services.Interfaces;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "IsTherapistOrAdmin")]
public sealed class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleService _userRoleService;

    public PatientsController(
        AppDbContext db, 
        UserManager<ApplicationUser> userManager,
        IUserRoleService userRoleService)
    {
        _db = db;
        _userManager = userManager;
        _userRoleService = userRoleService;
    }

    [HttpGet("information-types")]
    public async Task<IActionResult> GetInformationTypes()
    {
        var types = await _db.PatientInformationTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new
            {
                t.Id,
                t.Code,
                t.Name,
                t.Description,
                t.DisplayOrder
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patients = await _db.Patients
            .Where(p => p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.Email,
                p.Phone,
                p.DateOfBirth,
                p.Gender,
                p.City,
                p.LastSessionSummary,
                p.CreatedAt
            })
            .ToListAsync();
        return Ok(patients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _db.Patients
            .Where(p => p.Id == id && (p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator)))
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.Email,
                p.Phone,
                p.DateOfBirth,
                p.Gender,
                p.Pesel,
                p.Street,
                p.StreetNumber,
                p.ApartmentNumber,
                p.City,
                p.PostalCode,
                p.Country,
                p.LastSessionSummary,
                p.CreatedAt,
                p.UserId,
                HasUserAccount = !string.IsNullOrEmpty(p.UserId),
                SessionsCount = p.Sessions.Count,
                InformationEntries = p.InformationEntries
                    .OrderBy(x => x.PatientInformationType.DisplayOrder)
                    .Select(x => new
                    {
                        x.Id,
                        x.PatientInformationTypeId,
                        TypeCode = x.PatientInformationType.Code,
                        TypeName = x.PatientInformationType.Name,
                        x.Content,
                        x.CreatedAt,
                        x.UpdatedAt
                    })
            })
            .FirstOrDefaultAsync();
        
        if (patient is null) return NotFound();
        return Ok(patient);
    }

    public sealed record PatientInformationEntryRequest(
        int PatientInformationTypeId,
        string? Content);

    public sealed record CreatePatientRequest(
        string FirstName, 
        string LastName, 
        string Email, 
        string? Phone, 
        DateTime? DateOfBirth,
        string? Gender,
        string? Pesel,
        string? Street,
        string? StreetNumber,
        string? ApartmentNumber,
        string? City,
        string? PostalCode,
        string? Country,
        string? LastSessionSummary,
        IEnumerable<PatientInformationEntryRequest>? InformationEntries,
        string? Password = null, // Opcjonalne hasło - jeśli podane, utworzy konto użytkownika
        bool CreateUserAccount = false); // Flaga czy utworzyć konto użytkownika
        
    public sealed record UpdatePatientRequest(
        string FirstName, 
        string LastName, 
        string Email, 
        string? Phone,
        DateTime? DateOfBirth,
        string? Gender,
        string? Pesel,
        string? Street,
        string? StreetNumber,
        string? ApartmentNumber,
        string? City,
        string? PostalCode,
        string? Country,
        string? LastSessionSummary,
        IEnumerable<PatientInformationEntryRequest>? InformationEntries,
        string? Password = null, // Opcjonalne hasło - jeśli podane i pacjent nie ma konta, utworzy konto
        string? NewPassword = null, // Opcjonalne nowe hasło - jeśli podane i pacjent ma konto, zmieni hasło
        bool CreateUserAccount = false); // Flaga czy utworzyć konto użytkownika jeśli nie istnieje

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        string? applicationUserId = null;
        
        // Jeśli ma być utworzone konto użytkownika
        if (request.CreateUserAccount || !string.IsNullOrWhiteSpace(request.Password))
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Hasło jest wymagane przy tworzeniu konta użytkownika." });
            }

            // Sprawdź czy użytkownik z tym emailem już istnieje
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Użytkownik z tym adresem email już istnieje." });
            }

            // Utwórz konto użytkownika
            var user = new ApplicationUser
            {
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                LockoutEnabled = true,
                StatusId = (int)UserStatus.Active
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    error = "Nie udało się utworzyć konta użytkownika.",
                    details = createResult.Errors.Select(e => e.Description).ToArray()
                });
            }

            applicationUserId = user.Id;

            // Przypisz rolę Pacjent
            await _userManager.AddToRoleAsync(user, Roles.Pacjent);
            await _userRoleService.AssignRoleAsync(user.Id, UserRole.Pacjent);
        }

        var patient = new Patient
        {
            UserId = applicationUserId, // Połącz z kontem użytkownika jeśli zostało utworzone
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Pesel = request.Pesel,
            Street = request.Street,
            StreetNumber = request.StreetNumber,
            ApartmentNumber = request.ApartmentNumber,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country ?? "Polska",
            LastSessionSummary = request.LastSessionSummary,
            CreatedByUserId = userId
        };
        
        var informationEntriesPayload = (request.InformationEntries ?? Enumerable.Empty<PatientInformationEntryRequest>())
            .GroupBy(x => x.PatientInformationTypeId)
            .ToDictionary(g => g.Key, g => g.Last().Content);
        var now = DateTime.UtcNow;

        var informationTypes = await _db.PatientInformationTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        foreach (var type in informationTypes)
        {
            patient.InformationEntries.Add(new PatientInformation
            {
                PatientInformationTypeId = type.Id,
                Content = informationEntriesPayload.TryGetValue(type.Id, out var content) ? content : null,
                CreatedAt = now,
                UpdatedAt = informationEntriesPayload.ContainsKey(type.Id) ? now : null
            });
        }

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(Get), new { id = patient.Id }, new 
        { 
            patient.Id, 
            patient.FirstName, 
            patient.LastName, 
            patient.Email,
            userId = applicationUserId,
            hasUserAccount = applicationUserId != null
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _db.Patients
            .Include(p => p.InformationEntries)
            .FirstOrDefaultAsync(p => p.Id == id && (p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator)));
        
        if (patient is null) return NotFound();
        
        // Jeśli pacjent nie ma konta użytkownika i ma być utworzone
        if (string.IsNullOrEmpty(patient.UserId) && (request.CreateUserAccount || !string.IsNullOrWhiteSpace(request.Password)))
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Hasło jest wymagane przy tworzeniu konta użytkownika." });
            }

            // Sprawdź czy użytkownik z tym emailem już istnieje
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Użytkownik z tym adresem email już istnieje." });
            }

            // Utwórz konto użytkownika
            var user = new ApplicationUser
            {
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                LockoutEnabled = true,
                StatusId = (int)UserStatus.Active
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    error = "Nie udało się utworzyć konta użytkownika.",
                    details = createResult.Errors.Select(e => e.Description).ToArray()
                });
            }

            patient.UserId = user.Id;

            // Przypisz rolę Pacjent
            await _userManager.AddToRoleAsync(user, Roles.Pacjent);
            await _userRoleService.AssignRoleAsync(user.Id, UserRole.Pacjent);
        }
        
        // Jeśli pacjent ma konto użytkownika i podano nowe hasło, zmień hasło
        if (!string.IsNullOrEmpty(patient.UserId) && !string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var user = await _userManager.FindByIdAsync(patient.UserId);
            if (user != null)
            {
                // Usuń stare hasło i dodaj nowe (nie wymagamy starego hasła przy edycji przez terapeutę/admina)
                var hasPassword = await _userManager.HasPasswordAsync(user);
                if (hasPassword)
                {
                    var removeResult = await _userManager.RemovePasswordAsync(user);
                    if (!removeResult.Succeeded)
                    {
                        return BadRequest(new
                        {
                            error = "Nie udało się usunąć starego hasła.",
                            details = removeResult.Errors.Select(e => e.Description).ToArray()
                        });
                    }
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
                if (!addPasswordResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Nie udało się ustawić nowego hasła.",
                        details = addPasswordResult.Errors.Select(e => e.Description).ToArray()
                    });
                }
            }
        }
        
        // Jeśli pacjent ma konto użytkownika i zmienił się email, zaktualizuj konto
        if (!string.IsNullOrEmpty(patient.UserId) && request.Email != patient.Email)
        {
            var user = await _userManager.FindByIdAsync(patient.UserId);
            if (user != null)
            {
                // Sprawdź czy nowy email nie jest już używany przez innego użytkownika
                var existingUserWithEmail = await _userManager.FindByEmailAsync(request.Email);
                if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
                {
                    return BadRequest(new { error = "Użytkownik z tym adresem email już istnieje." });
                }

                user.Email = request.Email.Trim();
                user.UserName = request.Email.Trim();
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Nie udało się zaktualizować adresu email w koncie użytkownika.",
                        details = updateResult.Errors.Select(e => e.Description).ToArray()
                    });
                }
            }
        }
        
        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.Email = request.Email;
        patient.Phone = request.Phone;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Pesel = request.Pesel;
        patient.Street = request.Street;
        patient.StreetNumber = request.StreetNumber;
        patient.ApartmentNumber = request.ApartmentNumber;
        patient.City = request.City;
        patient.PostalCode = request.PostalCode;
        patient.Country = request.Country ?? "Polska";
        patient.LastSessionSummary = request.LastSessionSummary;

        var informationEntriesPayload = (request.InformationEntries ?? Enumerable.Empty<PatientInformationEntryRequest>())
            .GroupBy(x => x.PatientInformationTypeId)
            .ToDictionary(g => g.Key, g => g.Last().Content);
        var now = DateTime.UtcNow;

        var activeTypeIds = await _db.PatientInformationTypes
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync();
        var existingTypeIds = patient.InformationEntries
            .Select(x => x.PatientInformationTypeId)
            .ToHashSet();
        foreach (var typeId in activeTypeIds)
        {
            if (!existingTypeIds.Contains(typeId))
            {
                patient.InformationEntries.Add(new PatientInformation
                {
                    PatientInformationTypeId = typeId,
                    Content = informationEntriesPayload.TryGetValue(typeId, out var content) ? content : null,
                    CreatedAt = now,
                    UpdatedAt = informationEntriesPayload.ContainsKey(typeId) ? now : null
                });
            }
        }

        foreach (var entry in patient.InformationEntries)
        {
            if (informationEntriesPayload.TryGetValue(entry.PatientInformationTypeId, out var content))
            {
                entry.Content = content;
                entry.UpdatedAt = now;
            }
        }
        
        await _db.SaveChangesAsync();
        return Ok(new 
        { 
            patient.Id, 
            patient.FirstName, 
            patient.LastName, 
            patient.Email,
            userId = patient.UserId,
            hasUserAccount = !string.IsNullOrEmpty(patient.UserId)
        });
    }
}

