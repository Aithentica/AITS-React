using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SessionTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SessionTypesController(AppDbContext db) => _db = db;

    public sealed record SessionTypeTipDto(int Id, string Content, int DisplayOrder, bool IsActive);
    public sealed record SessionTypeQuestionDto(int Id, string Content, int DisplayOrder, bool IsActive);
    public sealed record SessionTypeDto(
        int Id,
        string Name,
        string? Description,
        bool IsActive,
        bool IsSystem,
        int SessionsCount,
        IReadOnlyList<SessionTypeTipDto> Tips,
        IReadOnlyList<SessionTypeQuestionDto> Questions);

    public sealed record UpsertSessionTypeTipRequest(
        int? Id,
        [Required, StringLength(2000, MinimumLength = 1)] string Content,
        int DisplayOrder,
        bool IsActive);

    public sealed record UpsertSessionTypeQuestionRequest(
        int? Id,
        [Required, StringLength(2000, MinimumLength = 1)] string Content,
        int DisplayOrder,
        bool IsActive);

    public sealed record UpsertSessionTypeRequest(
        [Required, StringLength(200)] string Name,
        [StringLength(1000)] string? Description,
        bool IsActive,
        bool? IsSystem,
        IReadOnlyList<UpsertSessionTypeTipRequest>? Tips,
        IReadOnlyList<UpsertSessionTypeQuestionRequest>? Questions);

    public sealed record CreateUserVersionRequest(
        [Required] int BaseSessionTypeId,
        [Required, StringLength(200)] string Name,
        [StringLength(1000)] string? Description,
        IReadOnlyList<UpsertSessionTypeTipRequest>? Tips,
        IReadOnlyList<UpsertSessionTypeQuestionRequest>? Questions);

    [HttpGet]
    [Authorize(Policy = "IsAdministrator")]
    public async Task<ActionResult<IEnumerable<SessionTypeDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var sessionTypes = await _db.SessionTypes
            .Where(t => includeInactive || t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SessionTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsActive,
                t.IsSystem,
                t.Sessions.Count,
                t.Tips
                    .OrderBy(tip => tip.DisplayOrder)
                    .Select(tip => new SessionTypeTipDto(tip.Id, tip.Content, tip.DisplayOrder, tip.IsActive))
                    .ToList(),
                t.Questions
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new SessionTypeQuestionDto(q.Id, q.Content, q.DisplayOrder, q.IsActive))
                    .ToList()))
            .ToListAsync();

        return Ok(sessionTypes);
    }

    [HttpGet("available")]
    [Authorize(Policy = "IsTherapistOrAdmin")]
    public async Task<ActionResult<IEnumerable<SessionTypeDto>>> GetAvailable()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);

        var sessionTypes = await _db.SessionTypes
            .Where(t => t.IsActive && (
                t.IsSystem || // Typy systemowe dostępne dla wszystkich
                t.CreatedByUserId == null || // Globalne typy (np. utworzone przez admina)
                (!t.IsSystem && t.CreatedByUserId == userId) || // Własne typy użytkownika
                isAdmin // Administrator widzi wszystko
            ))
            .OrderBy(t => t.IsSystem ? 0 : 1) // Najpierw systemowe
            .ThenBy(t => t.Name)
            .Select(t => new SessionTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsActive,
                t.IsSystem,
                t.Sessions.Count,
                t.Tips
                    .Where(tip => tip.IsActive)
                    .OrderBy(tip => tip.DisplayOrder)
                    .Select(tip => new SessionTypeTipDto(tip.Id, tip.Content, tip.DisplayOrder, tip.IsActive))
                    .ToList(),
                t.Questions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new SessionTypeQuestionDto(q.Id, q.Content, q.DisplayOrder, q.IsActive))
                    .ToList()))
            .ToListAsync();

        return Ok(sessionTypes);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "IsAdministrator")]
    public async Task<ActionResult<SessionTypeDto>> Get(int id)
    {
        var sessionType = await _db.SessionTypes
            .Where(t => t.Id == id)
            .Select(t => new SessionTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsActive,
                t.IsSystem,
                t.Sessions.Count,
                t.Tips
                    .OrderBy(tip => tip.DisplayOrder)
                    .Select(tip => new SessionTypeTipDto(tip.Id, tip.Content, tip.DisplayOrder, tip.IsActive))
                    .ToList(),
                t.Questions
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new SessionTypeQuestionDto(q.Id, q.Content, q.DisplayOrder, q.IsActive))
                    .ToList()))
            .FirstOrDefaultAsync();

        if (sessionType is null) return NotFound();
        return Ok(sessionType);
    }

    [HttpGet("{id:int}/details")]
    [Authorize]
    public async Task<ActionResult<SessionTypeDto>> GetDetails(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);

        var sessionType = await _db.SessionTypes
            .Where(t => t.Id == id && t.IsActive && (
                t.IsSystem ||
                t.CreatedByUserId == null ||
                (!t.IsSystem && t.CreatedByUserId == userId) ||
                isAdmin
            ))
            .Select(t => new SessionTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsActive,
                t.IsSystem,
                t.Sessions.Count,
                t.Tips
                    .Where(tip => tip.IsActive)
                    .OrderBy(tip => tip.DisplayOrder)
                    .Select(tip => new SessionTypeTipDto(tip.Id, tip.Content, tip.DisplayOrder, tip.IsActive))
                    .ToList(),
                t.Questions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new SessionTypeQuestionDto(q.Id, q.Content, q.DisplayOrder, q.IsActive))
                    .ToList()))
            .FirstOrDefaultAsync();

        if (sessionType is null) return NotFound();
        return Ok(sessionType);
    }

    [HttpPost]
    [Authorize(Policy = "IsAdministrator")]
    public async Task<ActionResult<SessionTypeDto>> Create([FromBody] UpsertSessionTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Nazwa jest wymagana.");
            return ValidationProblem(ModelState);
        }

        if (!ValidateTips(request.Tips) | !ValidateQuestions(request.Questions))
        {
            return ValidationProblem(ModelState);
        }

        if (!await IsNameUniqueAsync(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Podana nazwa jest już zajęta.");
            return ValidationProblem(ModelState);
        }

        var sessionType = new SessionType
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            IsSystem = request.IsSystem ?? false,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Tips is not null)
        {
            foreach (var tip in request.Tips)
            {
                sessionType.Tips.Add(new SessionTypeTip
                {
                    Content = tip.Content.Trim(),
                    DisplayOrder = tip.DisplayOrder,
                    IsActive = tip.IsActive
                });
            }
        }

        if (request.Questions is not null)
        {
            foreach (var question in request.Questions)
            {
                sessionType.Questions.Add(new SessionTypeQuestion
                {
                    Content = question.Content.Trim(),
                    DisplayOrder = question.DisplayOrder,
                    IsActive = question.IsActive
                });
            }
        }

        _db.SessionTypes.Add(sessionType);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = sessionType.Id }, await MapToDto(sessionType.Id));
    }

    [HttpPost("user-version")]
    [Authorize(Policy = "IsTherapistOrAdmin")]
    public async Task<ActionResult<SessionTypeDto>> CreateUserVersion([FromBody] CreateUserVersionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Nazwa jest wymagana.");
            return ValidationProblem(ModelState);
        }

        // Sprawdzenie czy bazowy typ istnieje i jest systemowy
        var baseType = await _db.SessionTypes
            .FirstOrDefaultAsync(t => t.Id == request.BaseSessionTypeId && t.IsSystem && t.IsActive);
        
        if (baseType is null)
        {
            return BadRequest("Bazowy typ sesji nie istnieje lub nie jest typem systemowym.");
        }

        if (!ValidateTips(request.Tips) | !ValidateQuestions(request.Questions))
        {
            return ValidationProblem(ModelState);
        }

        if (!await IsNameUniqueAsync(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Podana nazwa jest już zajęta.");
            return ValidationProblem(ModelState);
        }

        var sessionType = new SessionType
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true,
            IsSystem = false,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Jeśli nie podano własnych podpowiedzi i pytań, skopiuj z bazowego typu
        if (request.Tips is null || request.Tips.Count == 0)
        {
            var baseTips = await _db.SessionTypeTips
                .Where(t => t.SessionTypeId == baseType.Id && t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
            
            foreach (var tip in baseTips)
            {
                sessionType.Tips.Add(new SessionTypeTip
                {
                    Content = tip.Content,
                    DisplayOrder = tip.DisplayOrder,
                    IsActive = tip.IsActive
                });
            }
        }
        else
        {
            foreach (var tip in request.Tips)
            {
                sessionType.Tips.Add(new SessionTypeTip
                {
                    Content = tip.Content.Trim(),
                    DisplayOrder = tip.DisplayOrder,
                    IsActive = tip.IsActive
                });
            }
        }

        if (request.Questions is null || request.Questions.Count == 0)
        {
            var baseQuestions = await _db.SessionTypeQuestions
                .Where(q => q.SessionTypeId == baseType.Id && q.IsActive)
                .OrderBy(q => q.DisplayOrder)
                .ToListAsync();
            
            foreach (var question in baseQuestions)
            {
                sessionType.Questions.Add(new SessionTypeQuestion
                {
                    Content = question.Content,
                    DisplayOrder = question.DisplayOrder,
                    IsActive = question.IsActive
                });
            }
        }
        else
        {
            foreach (var question in request.Questions)
            {
                sessionType.Questions.Add(new SessionTypeQuestion
                {
                    Content = question.Content.Trim(),
                    DisplayOrder = question.DisplayOrder,
                    IsActive = question.IsActive
                });
            }
        }

        _db.SessionTypes.Add(sessionType);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = sessionType.Id }, await MapToDto(sessionType.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "IsAdministrator")]
    public async Task<ActionResult<SessionTypeDto>> Update(int id, [FromBody] UpsertSessionTypeRequest request)
    {
        var sessionType = await _db.SessionTypes
            .Include(t => t.Tips)
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (sessionType is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Nazwa jest wymagana.");
            return ValidationProblem(ModelState);
        }

        if (!ValidateTips(request.Tips) | !ValidateQuestions(request.Questions))
        {
            return ValidationProblem(ModelState);
        }

        if (!string.Equals(sessionType.Name, request.Name, StringComparison.OrdinalIgnoreCase) && !await IsNameUniqueAsync(request.Name, id))
        {
            ModelState.AddModelError(nameof(request.Name), "Podana nazwa jest już zajęta.");
            return ValidationProblem(ModelState);
        }

        sessionType.Name = request.Name.Trim();
        sessionType.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        sessionType.IsActive = request.IsActive;
        if (request.IsSystem.HasValue)
        {
            sessionType.IsSystem = request.IsSystem.Value;
        }
        sessionType.UpdatedAt = DateTime.UtcNow;

        SyncTips(sessionType, request.Tips);
        SyncQuestions(sessionType, request.Questions);

        await _db.SaveChangesAsync();

        return Ok(await MapToDto(sessionType.Id));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "IsAdministrator")]
    public async Task<IActionResult> Delete(int id)
    {
        var sessionType = await _db.SessionTypes
            .Include(t => t.Sessions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (sessionType is null) return NotFound();
        
        if (sessionType.IsSystem)
        {
            return BadRequest("Nie można usunąć typu systemowego.");
        }
        
        if (sessionType.Sessions.Any())
        {
            return BadRequest("Nie można usunąć typu sesji powiązanego z istniejącymi sesjami.");
        }

        _db.SessionTypes.Remove(sessionType);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<SessionTypeDto> MapToDto(int id)
    {
        return await _db.SessionTypes
            .Where(t => t.Id == id)
            .Select(t => new SessionTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsActive,
                t.IsSystem,
                t.Sessions.Count,
                t.Tips
                    .OrderBy(tip => tip.DisplayOrder)
                    .Select(tip => new SessionTypeTipDto(tip.Id, tip.Content, tip.DisplayOrder, tip.IsActive))
                    .ToList(),
                t.Questions
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new SessionTypeQuestionDto(q.Id, q.Content, q.DisplayOrder, q.IsActive))
                    .ToList()))
            .FirstAsync();
    }

    private async Task<bool> IsNameUniqueAsync(string name, int? id = null)
    {
        var normalized = name.Trim();
        return !await _db.SessionTypes.AnyAsync(t => t.Name == normalized && (!id.HasValue || t.Id != id.Value));
    }

    private bool ValidateTips(IReadOnlyList<UpsertSessionTypeTipRequest>? tips)
    {
        if (tips is null) return true;

        var isValid = true;
        var displayOrders = new HashSet<int>();

        for (var i = 0; i < tips.Count; i++)
        {
            var tip = tips[i];
            if (string.IsNullOrWhiteSpace(tip.Content))
            {
                ModelState.AddModelError($"Tips[{i}].Content", "Treść podpowiedzi jest wymagana.");
                isValid = false;
            }

            if (!displayOrders.Add(tip.DisplayOrder))
            {
                ModelState.AddModelError($"Tips[{i}].DisplayOrder", "Duplikat kolejności wyświetlania dla podpowiedzi.");
                isValid = false;
            }
        }

        return isValid;
    }

    private bool ValidateQuestions(IReadOnlyList<UpsertSessionTypeQuestionRequest>? questions)
    {
        if (questions is null) return true;

        var isValid = true;
        var displayOrders = new HashSet<int>();

        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            if (string.IsNullOrWhiteSpace(question.Content))
            {
                ModelState.AddModelError($"Questions[{i}].Content", "Treść pytania jest wymagana.");
                isValid = false;
            }

            if (!displayOrders.Add(question.DisplayOrder))
            {
                ModelState.AddModelError($"Questions[{i}].DisplayOrder", "Duplikat kolejności wyświetlania dla pytań.");
                isValid = false;
            }
        }

        return isValid;
    }

    private static void SyncTips(SessionType sessionType, IReadOnlyList<UpsertSessionTypeTipRequest>? tips)
    {
        tips ??= Array.Empty<UpsertSessionTypeTipRequest>();

        var existingById = sessionType.Tips.ToDictionary(t => t.Id);
        var idsToKeep = new HashSet<int>();

        foreach (var tipDto in tips)
        {
            if (tipDto.Id.HasValue && tipDto.Id.Value > 0 && existingById.TryGetValue(tipDto.Id.Value, out var existing))
            {
                existing.Content = tipDto.Content.Trim();
                existing.DisplayOrder = tipDto.DisplayOrder;
                existing.IsActive = tipDto.IsActive;
                idsToKeep.Add(existing.Id);
            }
            else
            {
                sessionType.Tips.Add(new SessionTypeTip
                {
                    Content = tipDto.Content.Trim(),
                    DisplayOrder = tipDto.DisplayOrder,
                    IsActive = tipDto.IsActive
                });
            }
        }

        var toRemove = sessionType.Tips
            .Where(t => t.Id != 0 && !idsToKeep.Contains(t.Id))
            .ToList();

        foreach (var tip in toRemove)
        {
            sessionType.Tips.Remove(tip);
        }
    }

    private static void SyncQuestions(SessionType sessionType, IReadOnlyList<UpsertSessionTypeQuestionRequest>? questions)
    {
        questions ??= Array.Empty<UpsertSessionTypeQuestionRequest>();

        var existingById = sessionType.Questions.ToDictionary(q => q.Id);
        var idsToKeep = new HashSet<int>();

        foreach (var questionDto in questions)
        {
            if (questionDto.Id.HasValue && questionDto.Id.Value > 0 && existingById.TryGetValue(questionDto.Id.Value, out var existing))
            {
                existing.Content = questionDto.Content.Trim();
                existing.DisplayOrder = questionDto.DisplayOrder;
                existing.IsActive = questionDto.IsActive;
                idsToKeep.Add(existing.Id);
            }
            else
            {
                sessionType.Questions.Add(new SessionTypeQuestion
                {
                    Content = questionDto.Content.Trim(),
                    DisplayOrder = questionDto.DisplayOrder,
                    IsActive = questionDto.IsActive
                });
            }
        }

        var toRemove = sessionType.Questions
            .Where(q => q.Id != 0 && !idsToKeep.Contains(q.Id))
            .ToList();

        foreach (var question in toRemove)
        {
            sessionType.Questions.Remove(question);
        }
    }
}

