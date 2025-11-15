using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/therapist/documents")]
[Authorize(Policy = "IsTherapistOrAdmin")]
public sealed class TherapistDocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public TherapistDocumentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var documents = await _db.TherapistDocuments
            .Where(d => d.TherapistId == therapistId)
            .OrderByDescending(d => d.UploadDate)
            .Select(d => new
            {
                d.Id,
                d.Description,
                d.FileName,
                d.ContentType,
                d.FileSize,
                d.UploadDate,
                d.CreatedAt,
                d.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var document = await _db.TherapistDocuments
            .Where(d => d.Id == id && d.TherapistId == therapistId)
            .Select(d => new
            {
                d.Id,
                d.Description,
                d.FileName,
                d.ContentType,
                d.FileSize,
                d.UploadDate,
                d.CreatedAt,
                d.UpdatedAt,
                FileContent = Convert.ToBase64String(d.FileContent)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return NotFound(new { error = "Nie znaleziono dokumentu." });
        }

        return Ok(document);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var document = await _db.TherapistDocuments
            .Where(d => d.Id == id && d.TherapistId == therapistId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return NotFound(new { error = "Nie znaleziono dokumentu." });
        }

        return File(document.FileContent, document.ContentType, document.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTherapistDocumentRequest request, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { error = "Plik jest wymagany." });
        }

        if (request.File.Length > MaxFileSize)
        {
            return BadRequest(new { error = $"Rozmiar pliku przekracza maksymalny limit {MaxFileSize / (1024 * 1024)} MB." });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Opis dokumentu jest wymagany." });
        }

        // Upewnij się, że profil terapeuty istnieje
        var profileExists = await _db.TherapistProfiles
            .AnyAsync(p => p.TherapistId == therapistId, cancellationToken);

        if (!profileExists)
        {
            return BadRequest(new { error = "Profil terapeuty nie istnieje. Najpierw utwórz profil terapeuty." });
        }

        byte[] fileContent;
        await using (var memoryStream = new MemoryStream())
        {
            await request.File.CopyToAsync(memoryStream, cancellationToken);
            fileContent = memoryStream.ToArray();
        }

        var document = new TherapistDocument
        {
            TherapistId = therapistId,
            Description = request.Description.Trim(),
            FileName = request.File.FileName ?? "unnamed",
            ContentType = request.File.ContentType ?? "application/octet-stream",
            FileSize = request.File.Length,
            FileContent = fileContent,
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.TherapistDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = document.Id }, new
        {
            document.Id,
            document.Description,
            document.FileName,
            document.ContentType,
            document.FileSize,
            document.UploadDate,
            document.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTherapistDocumentRequest request, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var document = await _db.TherapistDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.TherapistId == therapistId, cancellationToken);

        if (document is null)
        {
            return NotFound(new { error = "Nie znaleziono dokumentu." });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Opis dokumentu jest wymagany." });
        }

        document.Description = request.Description.Trim();
        document.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            document.Id,
            document.Description,
            document.FileName,
            document.ContentType,
            document.FileSize,
            document.UploadDate,
            document.CreatedAt,
            document.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var therapistId = GetTherapistId();
        if (therapistId is null)
        {
            return Unauthorized(new { error = "Nie znaleziono identyfikatora terapeuty." });
        }

        var document = await _db.TherapistDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.TherapistId == therapistId, cancellationToken);

        if (document is null)
        {
            return NotFound(new { error = "Nie znaleziono dokumentu." });
        }

        _db.TherapistDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
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

    public sealed record CreateTherapistDocumentRequest(
        string Description,
        IFormFile File);

    public sealed record UpdateTherapistDocumentRequest(
        string Description);
}


