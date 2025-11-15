using System.Security.Claims;
using AITS.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/therapist/profile")]
[Authorize(Policy = "IsTherapistOrAdmin")]
public sealed class TherapistProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public TherapistProfileController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var profile = await _db.TherapistProfiles
            .Where(p => p.TherapistId == therapistId)
            .Select(p => new
            {
                p.TherapistId,
                p.FirstName,
                p.LastName,
                p.CompanyName,
                p.TaxId,
                p.Regon,
                p.BusinessAddress,
                p.BusinessCity,
                p.BusinessPostalCode,
                p.BusinessCountry,
                p.IsCompany,
                p.CreatedAt,
                p.UpdatedAt,
                DocumentsCount = p.Documents.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return NotFound(new { error = "Profil terapeuty nie istnieje." });
        }

        return Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTherapistProfileRequest request, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest(new { error = "Imię jest wymagane." });
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { error = "Nazwisko jest wymagane." });
        }

        // Walidacja NIP jeśli został podany
        if (!string.IsNullOrWhiteSpace(request.TaxId) && !PolishTaxIdValidator.IsValidNip(request.TaxId))
        {
            return BadRequest(new { error = "Nieprawidłowy numer NIP." });
        }

        // Walidacja REGON jeśli został podany
        if (!string.IsNullOrWhiteSpace(request.Regon) && !PolishTaxIdValidator.IsValidRegon(request.Regon))
        {
            return BadRequest(new { error = "Nieprawidłowy numer REGON." });
        }

        var profileExists = await _db.TherapistProfiles
            .AnyAsync(p => p.TherapistId == therapistId, cancellationToken);

        if (profileExists)
        {
            return BadRequest(new { error = "Profil terapeuty już istnieje. Użyj PUT do aktualizacji." });
        }

        var profile = new TherapistProfile
        {
            TherapistId = therapistId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim(),
            TaxId = string.IsNullOrWhiteSpace(request.TaxId) ? null : PolishTaxIdValidator.FormatNip(request.TaxId),
            Regon = string.IsNullOrWhiteSpace(request.Regon) ? null : PolishTaxIdValidator.FormatRegon(request.Regon),
            BusinessAddress = string.IsNullOrWhiteSpace(request.BusinessAddress) ? null : request.BusinessAddress.Trim(),
            BusinessCity = string.IsNullOrWhiteSpace(request.BusinessCity) ? null : request.BusinessCity.Trim(),
            BusinessPostalCode = string.IsNullOrWhiteSpace(request.BusinessPostalCode) ? null : request.BusinessPostalCode.Trim(),
            BusinessCountry = string.IsNullOrWhiteSpace(request.BusinessCountry) ? "Polska" : request.BusinessCountry.Trim(),
            IsCompany = request.IsCompany,
            CreatedAt = DateTime.UtcNow
        };

        _db.TherapistProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { therapistId = profile.TherapistId }, new
        {
            profile.TherapistId,
            profile.FirstName,
            profile.LastName,
            profile.CompanyName,
            profile.TaxId,
            profile.Regon,
            profile.BusinessAddress,
            profile.BusinessCity,
            profile.BusinessPostalCode,
            profile.BusinessCountry,
            profile.IsCompany,
            profile.CreatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTherapistProfileRequest request, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var profile = await _db.TherapistProfiles
            .FirstOrDefaultAsync(p => p.TherapistId == therapistId, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { error = "Profil terapeuty nie istnieje. Użyj POST do utworzenia." });
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest(new { error = "Imię jest wymagane." });
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { error = "Nazwisko jest wymagane." });
        }

        // Walidacja NIP jeśli został podany
        if (!string.IsNullOrWhiteSpace(request.TaxId) && !PolishTaxIdValidator.IsValidNip(request.TaxId))
        {
            return BadRequest(new { error = "Nieprawidłowy numer NIP." });
        }

        // Walidacja REGON jeśli został podany
        if (!string.IsNullOrWhiteSpace(request.Regon) && !PolishTaxIdValidator.IsValidRegon(request.Regon))
        {
            return BadRequest(new { error = "Nieprawidłowy numer REGON." });
        }

        profile.FirstName = request.FirstName.Trim();
        profile.LastName = request.LastName.Trim();
        profile.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim();
        profile.TaxId = string.IsNullOrWhiteSpace(request.TaxId) ? null : PolishTaxIdValidator.FormatNip(request.TaxId);
        profile.Regon = string.IsNullOrWhiteSpace(request.Regon) ? null : PolishTaxIdValidator.FormatRegon(request.Regon);
        profile.BusinessAddress = string.IsNullOrWhiteSpace(request.BusinessAddress) ? null : request.BusinessAddress.Trim();
        profile.BusinessCity = string.IsNullOrWhiteSpace(request.BusinessCity) ? null : request.BusinessCity.Trim();
        profile.BusinessPostalCode = string.IsNullOrWhiteSpace(request.BusinessPostalCode) ? null : request.BusinessPostalCode.Trim();
        profile.BusinessCountry = string.IsNullOrWhiteSpace(request.BusinessCountry) ? "Polska" : request.BusinessCountry.Trim();
        profile.IsCompany = request.IsCompany;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            profile.TherapistId,
            profile.FirstName,
            profile.LastName,
            profile.CompanyName,
            profile.TaxId,
            profile.Regon,
            profile.BusinessAddress,
            profile.BusinessCity,
            profile.BusinessPostalCode,
            profile.BusinessCountry,
            profile.IsCompany,
            profile.CreatedAt,
            profile.UpdatedAt
        });
    }

    private string? GetTherapistId()
    {
        if (User.IsInRole(Roles.Administrator))
        {
            // Administrator może działać w imieniu terapeuty - można rozszerzyć o parametr
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return null;
        }

        // Sprawdź czy użytkownik ma rolę terapeuty
        var isTherapist = User.IsInRole(Roles.Terapeuta) || User.IsInRole(Roles.TerapeutaFreeAccess);
        return isTherapist ? userId : null;
    }

    public sealed record CreateTherapistProfileRequest(
        string FirstName,
        string LastName,
        string? CompanyName,
        string? TaxId,
        string? Regon,
        string? BusinessAddress,
        string? BusinessCity,
        string? BusinessPostalCode,
        string? BusinessCountry,
        bool IsCompany);

    public sealed record UpdateTherapistProfileRequest(
        string FirstName,
        string LastName,
        string? CompanyName,
        string? TaxId,
        string? Regon,
        string? BusinessAddress,
        string? BusinessCity,
        string? BusinessPostalCode,
        string? BusinessCountry,
        bool IsCompany);
}

