using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "IsTherapistOrAdmin")]
public sealed class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly ISmsService _smsService;
    private readonly IAzureSpeechService _azureSpeechService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        AppDbContext db,
        GoogleCalendarService googleCalendarService,
        IGoogleOAuthService googleOAuthService,
        ISmsService smsService,
        IAzureSpeechService azureSpeechService,
        IWebHostEnvironment environment,
        ILogger<SessionsController> logger)
    {
        _db = db;
        _googleCalendarService = googleCalendarService;
        _googleOAuthService = googleOAuthService;
        _smsService = smsService;
        _azureSpeechService = azureSpeechService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var sessions = await _db.Sessions
            .Where(s => s.TerapeutaId == userId && 
                   s.StartDateTime >= today && 
                   s.StartDateTime < tomorrow &&
                   s.StatusId != (int)SessionStatus.Cancelled)
            .Include(s => s.Patient)
            .OrderBy(s => s.StartDateTime)
            .Select(s => new
            {
                s.Id,
                Patient = new { s.Patient.FirstName, s.Patient.LastName, s.Patient.Email },
                s.StartDateTime,
                s.EndDateTime,
                s.StatusId,
                s.Price,
                s.GoogleMeetLink,
            })
            .ToListAsync();
        
        return Ok(sessions);
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetActiveSessionTypes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);

        var types = await _db.SessionTypes
            .Where(t => t.IsActive && (
                t.IsSystem || // Typy systemowe dostępne dla wszystkich
                (!t.IsSystem && t.CreatedByUserId == userId) || // Własne typy użytkownika
                isAdmin // Administrator widzi wszystko
            ))
            .OrderBy(t => t.IsSystem ? 0 : 1) // Najpierw systemowe
            .ThenBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                Tips = t.Tips
                    .Where(tip => tip.IsActive)
                    .OrderBy(tip => tip.DisplayOrder)
                    .Select(tip => new { tip.Id, tip.Content, tip.DisplayOrder })
                    .ToList(),
                Questions = t.Questions
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new { q.Id, q.Content, q.DisplayOrder })
                    .ToList()
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? statusId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _db.Sessions
            .Where(s => s.TerapeutaId == userId || User.IsInRole(Roles.Administrator))
            .Include(s => s.Patient)
            .AsQueryable();
        
        if (statusId.HasValue)
            query = query.Where(s => s.StatusId == statusId.Value);
        if (fromDate.HasValue)
            query = query.Where(s => s.StartDateTime >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(s => s.StartDateTime <= toDate.Value);
        if (patientId.HasValue)
            query = query.Where(s => s.PatientId == patientId.Value);
        
        var total = await query.CountAsync();
        var sessions = await query
            .OrderByDescending(s => s.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                Patient = new { s.Patient.Id, s.Patient.FirstName, s.Patient.LastName, s.Patient.Email },
                s.StartDateTime,
                s.EndDateTime,
                s.StatusId,
                s.Price,
                s.GoogleMeetLink,
                HasPayment = s.PaymentId.HasValue,
                Payment = s.Payment != null ? new
                {
                    s.Payment.StatusId,
                    s.Payment.Amount,
                    s.Payment.CreatedAt,
                    s.Payment.CompletedAt,
                    PaymentDelayDays = s.Payment.StatusId == (int)PaymentStatus.Completed && s.Payment.CompletedAt.HasValue
                        ? (int?)((s.Payment.CompletedAt.Value - s.StartDateTime).TotalDays)
                        : s.Payment.StatusId != (int)PaymentStatus.Completed
                        ? (int?)((DateTime.UtcNow - s.StartDateTime).TotalDays)
                        : null
                } : null,
                IsPaid = s.Payment != null && s.Payment.StatusId == (int)PaymentStatus.Completed,
                PaymentDelayDays = s.Payment == null || s.Payment.StatusId != (int)PaymentStatus.Completed
                    ? (int?)((DateTime.UtcNow - s.StartDateTime).TotalDays)
                    : null,
                HasTranscriptions = s.Transcriptions.Any()
            })
            .ToListAsync();
        
        return Ok(new { sessions, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.Sessions
            .Where(s => s.Id == id && (s.TerapeutaId == userId || User.IsInRole(Roles.Administrator)))
            .Include(s => s.Patient)
            .Include(s => s.Payment)
            .Include(s => s.Transcriptions)
            .Include(s => s.Parameters)
            .Select(s => new
            {
                s.Id,
                Patient = new { s.Patient.Id, s.Patient.FirstName, s.Patient.LastName, s.Patient.Email, s.Patient.Phone },
                s.StartDateTime,
                s.EndDateTime,
                s.StatusId,
                s.Price,
                s.GoogleCalendarEventId,
                s.GoogleMeetLink,
                s.PaymentId,
                s.SessionTypeId,
                Payment = s.Payment != null ? new 
                { 
                    s.Payment.StatusId, 
                    s.Payment.Amount, 
                    s.Payment.TpayTransactionId,
                    s.Payment.CreatedAt,
                    s.Payment.CompletedAt,
                    PaymentDelayDays = s.Payment.StatusId == (int)PaymentStatus.Completed && s.Payment.CompletedAt.HasValue
                        ? (int?)((s.Payment.CompletedAt.Value - s.StartDateTime).TotalDays)
                        : s.Payment.StatusId != (int)PaymentStatus.Completed
                        ? (int?)((DateTime.UtcNow - s.StartDateTime).TotalDays)
                        : null
                } : null,
                IsPaid = s.Payment != null && s.Payment.StatusId == (int)PaymentStatus.Completed,
                PaymentDelayDays = s.Payment == null || s.Payment.StatusId != (int)PaymentStatus.Completed
                    ? (int?)((DateTime.UtcNow - s.StartDateTime).TotalDays)
                    : null,
                s.Notes,
                s.PreviousWeekEvents,
                s.PreviousSessionReflections,
                s.PersonalWorkDiscussion,
                s.TherapeuticIntervention,
                s.AgreedPersonalWork,
                s.SessionSummary,
                s.CreatedAt,
                s.UpdatedAt,
                Transcriptions = s.Transcriptions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        Source = t.Source,
                        t.TranscriptText,
                        t.CreatedAt,
                        t.SourceFileName,
                        t.SourceContentType,
                        FilePath = t.SourceFilePath,
                        t.CreatedByUserId,
                        Segments = t.Segments
                            .OrderBy(seg => seg.StartOffset)
                            .Select(seg => new
                            {
                                seg.Id,
                                seg.SpeakerTag,
                                seg.StartOffset,
                                seg.EndOffset,
                                Content = seg.Content
                            })
                    }),
                Parameters = s.Parameters
                    .Select(p => new
                    {
                        p.Id,
                        p.ParameterName,
                        p.Value,
                        p.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
        
        if (session is null) return NotFound();
        return Ok(session);
    }

    public sealed class CreateSessionRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PatientId must be a positive value.")]
        public int PatientId { get; init; }

        [Required]
        public DateTime StartDateTime { get; init; }

        [Required]
        [Range(1, 24 * 60, ErrorMessage = "DurationMinutes must be between 1 and 1440.")]
        public int DurationMinutes { get; init; }

        [Required]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; init; }

        public string? Notes { get; init; }

        public int? SessionTypeId { get; init; }

    }

    public sealed class UpdateSessionRequest
    {
        [Required]
        public DateTime StartDateTime { get; init; }

        [Required]
        [Range(1, 24 * 60, ErrorMessage = "DurationMinutes must be between 1 and 1440.")]
        public int DurationMinutes { get; init; }

        [Required]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; init; }

        public string? Notes { get; init; }

        public string? PreviousWeekEvents { get; init; }

        public string? PreviousSessionReflections { get; init; }

        public string? PersonalWorkDiscussion { get; init; }

        public string? TherapeuticIntervention { get; init; }

        public string? AgreedPersonalWork { get; init; }

        public string? SessionSummary { get; init; }

        public int? SessionTypeId { get; init; }

        [Range(1, 4, ErrorMessage = "StatusId must be between 1 and 4.")]
        public int? StatusId { get; init; }

    }

    public sealed class CreateTranscriptionForm
    {
        [FromForm(Name = "sourceType")]
        public SessionTranscriptionSource Source { get; set; }

        [FromForm(Name = "text")]
        public string? Text { get; set; }

        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Sprawdź token Google, ale nie blokuj tworzenia sesji jeśli nie jest dostępny
        var tokenCheck = await _googleOAuthService.EnsureValidAccessTokenAsync(userId);
        var hasGoogleIntegration = tokenCheck.Success;
        
        if (!hasGoogleIntegration)
        {
            _logger.LogWarning("Google Calendar integration not available for user {UserId}: {Error}", userId, tokenCheck.Error);
        }

        var patient = await _db.Patients
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.Id == request.PatientId);
        if (patient is null) return BadRequest(new { error = "Patient not found" });

        var endDateTime = request.StartDateTime.AddMinutes(request.DurationMinutes);
        
        // Sprawdź czy SessionTypeId istnieje (jeśli podano)
        if (request.SessionTypeId.HasValue)
        {
            var sessionTypeExists = await _db.SessionTypes.AnyAsync(st => st.Id == request.SessionTypeId.Value && st.IsActive);
            if (!sessionTypeExists)
            {
                return BadRequest(new { error = "SessionType not found or inactive" });
            }
        }
        
        var session = new Session
        {
            PatientId = request.PatientId,
            TerapeutaId = userId,
            StartDateTime = request.StartDateTime,
            EndDateTime = endDateTime,
            StatusId = (int)SessionStatus.Scheduled,
            Price = request.Price,
            Notes = request.Notes,
            SessionTypeId = request.SessionTypeId
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        session.Patient = patient;
        
        // Próbuj utworzyć wydarzenie Google Calendar tylko jeśli integracja jest dostępna
        if (hasGoogleIntegration)
        {
            try
            {
                var calendarResult = await _googleCalendarService.CreateEventAsync(session);
                if (calendarResult != null && !string.IsNullOrEmpty(calendarResult.EventId))
                {
                    session.GoogleCalendarEventId = calendarResult.EventId;
                    session.GoogleMeetLink = calendarResult.MeetLink ?? "https://meet.google.com";
                    await _db.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("Failed to create Google Calendar event for session {SessionId}, continuing without calendar integration", session.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Google Calendar event for session {SessionId}, continuing without calendar integration", session.Id);
                // Kontynuuj bez integracji Google Calendar
            }
        }
        else
        {
            _logger.LogInformation("Session {SessionId} created without Google Calendar integration (token not available)", session.Id);
        }

        await transaction.CommitAsync();

        return CreatedAtAction(nameof(Get), new { id = session.Id }, new { session.Id, session.StartDateTime, session.EndDateTime, session.GoogleMeetLink });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.Sessions
            .Include(s => s.Patient)
            .FirstOrDefaultAsync(s => s.Id == id && s.TerapeutaId == userId);
        
        if (session is null) return NotFound();
        
        var endDateTime = request.StartDateTime.AddMinutes(request.DurationMinutes);
        
        // Sprawdź czy SessionTypeId istnieje (jeśli podano)
        if (request.SessionTypeId.HasValue)
        {
            var sessionTypeExists = await _db.SessionTypes.AnyAsync(st => st.Id == request.SessionTypeId.Value && st.IsActive);
            if (!sessionTypeExists)
            {
                return BadRequest(new { error = "SessionType not found or inactive" });
            }
        }
        
        session.StartDateTime = request.StartDateTime;
        session.EndDateTime = endDateTime;
        session.Price = request.Price;
        session.Notes = request.Notes;
        session.PreviousWeekEvents = request.PreviousWeekEvents;
        session.PreviousSessionReflections = request.PreviousSessionReflections;
        session.PersonalWorkDiscussion = request.PersonalWorkDiscussion;
        session.TherapeuticIntervention = request.TherapeuticIntervention;
        session.AgreedPersonalWork = request.AgreedPersonalWork;
        session.SessionSummary = request.SessionSummary;
        session.SessionTypeId = request.SessionTypeId;
        if (request.StatusId.HasValue)
        {
            session.StatusId = request.StatusId.Value;
        }
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        
        // Aktualizacja wydarzenia Google Calendar
        if (!string.IsNullOrEmpty(session.GoogleCalendarEventId))
        {
            try
            {
                await _googleCalendarService.UpdateEventAsync(session);
            }
            catch
            {
                // Log error but continue
            }
        }
        
        return Ok(new { session.Id, session.StartDateTime, session.EndDateTime });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TerapeutaId == userId);
        
        if (session is null) return NotFound();
        
        // Usunięcie wydarzenia Google Calendar
        if (!string.IsNullOrEmpty(session.GoogleCalendarEventId))
        {
            try
            {
                await _googleCalendarService.DeleteEventAsync(session.TerapeutaId, session.GoogleCalendarEventId);
            }
            catch
            {
                // Log error but continue
            }
        }
        
        session.StatusId = (int)SessionStatus.Cancelled;
        session.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();
        return Ok(new { message = "Session cancelled" });
    }

    [HttpGet("{id}/transcriptions")]
    public async Task<IActionResult> GetTranscriptions(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);

        var hasAccess = await _db.Sessions
            .AnyAsync(s => s.Id == id && (s.TerapeutaId == userId || isAdmin));

        if (!hasAccess)
        {
            return NotFound();
        }

        var transcriptions = await _db.SessionTranscriptions
            .Where(t => t.SessionId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                Source = t.Source,
                t.TranscriptText,
                t.CreatedAt,
                t.SourceFileName,
                t.SourceContentType,
                FilePath = t.SourceFilePath,
                t.CreatedByUserId,
                Segments = t.Segments
                    .OrderBy(s => s.StartOffset)
                    .Select(s => new
                    {
                        s.Id,
                        s.SpeakerTag,
                        s.StartOffset,
                        s.EndOffset,
                        Content = s.Content
                    })
            })
            .ToListAsync();

        return Ok(transcriptions);
    }

    [HttpPost("{id}/transcriptions")]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public async Task<IActionResult> CreateTranscription(int id, [FromForm] CreateTranscriptionForm form, CancellationToken cancellationToken)
    {
        if (form is null)
        {
            return BadRequest(new { error = "Niepoprawne dane formularza." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(Roles.Administrator);

        var session = await _db.Sessions
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == id && (s.TerapeutaId == userId || isAdmin), cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        string transcriptText;
        string? relativePath = null;
        string? absolutePath = null;
        string? originalFileName = form.File?.FileName;
        string? originalContentType = form.File?.ContentType;
        List<TranscriptionSegmentDto> diarizedSegments = new();

        try
        {
            await RemoveExistingTranscriptionsAsync(session.Id, cancellationToken);

            switch (form.Source)
            {
                case SessionTranscriptionSource.ManualText:
                    if (string.IsNullOrWhiteSpace(form.Text))
                    {
                        return BadRequest(new { error = "Tekst transkrypcji jest wymagany." });
                    }
                    transcriptText = form.Text.Trim();
                    break;

                case SessionTranscriptionSource.TextFile:
                    if (form.File is null || form.File.Length == 0)
                    {
                        return BadRequest(new { error = "Plik tekstowy jest wymagany." });
                    }
                    if (!IsSupportedTextFile(form.File.ContentType, form.File.FileName))
                    {
                        return BadRequest(new { error = "Obsługiwane są wyłącznie pliki tekstowe (.txt, text/plain)." });
                    }
                    (relativePath, absolutePath) = await SaveUploadedFileAsync(form.File, id, cancellationToken);
                    transcriptText = await ReadTextFileAsync(absolutePath, cancellationToken);
                    break;

                case SessionTranscriptionSource.FinalTranscriptUpload:
                    if (form.File is null || form.File.Length == 0)
                    {
                        return BadRequest(new { error = "Plik transkryptu jest wymagany." });
                    }
                    if (!IsSupportedTranscriptFile(form.File.ContentType, form.File.FileName))
                    {
                        return BadRequest(new { error = "Obsługiwane są pliki .txt, .vtt lub .srt." });
                    }
                    (relativePath, absolutePath) = await SaveUploadedFileAsync(form.File, id, cancellationToken);
                    transcriptText = await ReadTextFileAsync(absolutePath, cancellationToken);
                    break;

                case SessionTranscriptionSource.AudioRecording:
                case SessionTranscriptionSource.AudioUpload:
                case SessionTranscriptionSource.AudioFile:
                    if (form.File is null || form.File.Length == 0)
                    {
                        return BadRequest(new { error = "Plik audio jest wymagany." });
                    }
                    if (!IsSupportedAudioFile(form.File.ContentType, form.File.FileName))
                    {
                        return BadRequest(new { error = "Obsługiwane są pliki audio WAV lub MP3." });
                    }
                    (relativePath, absolutePath) = await SaveUploadedFileAsync(form.File, id, cancellationToken);
                    var audioResult = await _azureSpeechService.TranscribeAudioBatchAsync(absolutePath, form.File.ContentType ?? string.Empty, cancellationToken);
                    transcriptText = audioResult.Transcript;
                    diarizedSegments = audioResult.Segments.ToList();
                    break;

                case SessionTranscriptionSource.VideoFile:
                    if (form.File is null || form.File.Length == 0)
                    {
                        return BadRequest(new { error = "Plik wideo jest wymagany." });
                    }
                    if (!IsSupportedVideoFile(form.File.ContentType, form.File.FileName))
                    {
                        return BadRequest(new { error = "Obsługiwane są pliki MP4, MOV, MKV lub AVI." });
                    }
                    (relativePath, absolutePath) = await SaveUploadedFileAsync(form.File, id, cancellationToken);
                    var videoResult = await _azureSpeechService.TranscribeVideoAsync(absolutePath, form.File.ContentType ?? string.Empty, cancellationToken);
                    transcriptText = videoResult.Transcript;
                    diarizedSegments = videoResult.Segments.ToList();
                    break;

                case SessionTranscriptionSource.RealtimeRecording:
                    return BadRequest(new { error = "Transkrypcja w czasie rzeczywistym dostępna jest poprzez kanał SignalR." });

                default:
                    return BadRequest(new { error = "Nieobsługiwany typ źródła transkrypcji." });
            }
        }
        catch (NotSupportedException ex)
        {
            CleanupTemporaryFile(absolutePath);
            _logger.LogWarning(ex, "Nieobsługiwany format podczas transkrypcji dla sesji {SessionId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            CleanupTemporaryFile(absolutePath);
            _logger.LogError(ex, "Błąd podczas przetwarzania transkrypcji dla sesji {SessionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Wystąpił błąd podczas przetwarzania transkrypcji." });
        }

        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            CleanupTemporaryFile(absolutePath);
            return BadRequest(new { error = "Wynikowa transkrypcja jest pusta." });
        }

        var transcription = new SessionTranscription
        {
            SessionId = session.Id,
            Source = form.Source,
            TranscriptText = transcriptText.Trim(),
            SourceFileName = originalFileName,
            SourceContentType = originalContentType,
            SourceFilePath = relativePath,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        if (diarizedSegments.Count > 0)
        {
            transcription.Segments = diarizedSegments
                .OrderBy(s => s.StartOffset)
                .Select(s => new SessionTranscriptionSegment
                {
                    SpeakerTag = s.SpeakerTag,
                    StartOffset = s.StartOffset,
                    EndOffset = s.EndOffset,
                    Content = s.Text
                })
                .ToList();
        }

        _db.SessionTranscriptions.Add(transcription);
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetTranscriptions), new { id }, new
        {
            transcription.Id,
            Source = transcription.Source,
            transcription.TranscriptText,
            transcription.CreatedAt,
            transcription.SourceFileName,
            transcription.SourceContentType,
            FilePath = transcription.SourceFilePath,
            transcription.CreatedByUserId,
            Segments = transcription.Segments
                .OrderBy(s => s.StartOffset)
                .Select(s => new
                {
                    s.Id,
                    s.SpeakerTag,
                    s.StartOffset,
                    s.EndOffset,
                    Content = s.Content
                })
        });
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == id && s.TerapeutaId == userId);
        
        if (session is null) return NotFound();
        
        session.StatusId = (int)SessionStatus.Confirmed;
        session.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();
        return Ok(new { message = "Session confirmed", sessionId = session.Id });
    }

    [HttpPost("{id}/send-notification")]
    public async Task<IActionResult> SendNotification(int id, [FromQuery] string culture = "pl")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.Sessions
            .Include(s => s.Patient)
            .FirstOrDefaultAsync(s => s.Id == id && s.TerapeutaId == userId);
        
        if (session is null) return NotFound();
        
        if (string.IsNullOrEmpty(session.Patient.Phone))
            return BadRequest(new { error = "Patient phone number not set" });
        
        // Pobranie szablonu SMS z tłumaczeń
        var smsKey = session.StatusId == (int)SessionStatus.Confirmed 
            ? "sms.session.confirmed"
            : "sms.session.changed";
        
        var smsTemplate = await _db.Translations
            .FirstOrDefaultAsync(t => t.Key == smsKey && t.Culture == culture);
        
        if (smsTemplate is null) return BadRequest(new { error = "SMS template not found" });
        
        // Formatowanie wiadomości SMS
        var dateStr = session.StartDateTime.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo(culture == "pl" ? "pl-PL" : "en-US"));
        var timeStr = session.StartDateTime.ToString("HH:mm");
        var meetLink = session.GoogleMeetLink ?? "https://meet.google.com";
        var message = smsTemplate.Value
            .Replace("{date}", dateStr)
            .Replace("{time}", timeStr)
            .Replace("{link}", meetLink);
        
        var smsResult = await _smsService.SendAsync(new SmsSendRequest(session.Patient.Phone, message));

        // TODO: Wysłanie Email - implementacja w kolejnym kroku

        return Ok(new
        {
            message = "Notification sent",
            sessionId = session.Id,
            smsSent = smsResult.Success,
            smsError = smsResult.Error
        });
    }

    /// <summary>
    /// Endpoint testowy do wysyłania SMS - dostępny w Swagger do testowania integracji SMSAPI
    /// </summary>
    [HttpPost("test-sms")]
    public async Task<IActionResult> TestSms([FromBody] TestSmsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { error = "Phone number is required" });
        
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required" });
        
        try
        {
            var smsResult = await _smsService.SendAsync(new SmsSendRequest(request.PhoneNumber, request.Message));
            
            if (smsResult.Success)
            {
                return Ok(new 
                { 
                    success = true, 
                    message = "SMS sent successfully", 
                    phoneNumber = request.PhoneNumber,
                    messageText = request.Message,
                    timestamp = DateTime.UtcNow,
                    messageId = smsResult.MessageId,
                    cost = smsResult.Cost
                });
            }
            else
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = "Failed to send SMS - SMSAPI returned error",
                    details = smsResult.Error ?? "Unknown error"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false, 
                error = "Exception while sending SMS", 
                details = ex.Message 
            });
        }
    }

    public sealed record TestSmsRequest(
        string PhoneNumber,
        string Message);

    private string EnsureTranscriptionsDirectory(int sessionId)
    {
        var root = GetUploadsRoot();
        var directory = Path.Combine(root, sessionId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private async Task<(string RelativePath, string AbsolutePath)> SaveUploadedFileAsync(IFormFile file, int sessionId, CancellationToken cancellationToken)
    {
        var directory = EnsureTranscriptionsDirectory(sessionId);
        var safeFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".dat";
        }

        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(directory, storedName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = Path.Combine("uploads", "transcriptions", sessionId.ToString(CultureInfo.InvariantCulture), storedName)
            .Replace("\\", "/");

        return (relativePath, absolutePath);
    }

    private static async Task<string> ReadTextFileAsync(string absolutePath, CancellationToken cancellationToken)
    {
        await using var stream = System.IO.File.OpenRead(absolutePath);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static bool IsSupportedTextFile(string? contentType, string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase) || extension == ".txt";
    }

    private static bool IsSupportedAudioFile(string? contentType, string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return string.Equals(contentType, "audio/wav", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "audio/x-wav", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "audio/mpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "audio/mp3", StringComparison.OrdinalIgnoreCase)
            || extension is ".wav" or ".mp3";
    }

    private static bool IsSupportedTranscriptFile(string? contentType, string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (extension is ".txt" or ".vtt" or ".srt")
        {
            return true;
        }

        return string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "text/vtt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/x-subrip", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedVideoFile(string? contentType, string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (extension is ".mp4" or ".mov" or ".mkv" or ".avi")
        {
            return true;
        }

        return string.Equals(contentType, "video/mp4", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "video/quicktime", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "video/x-matroska", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "video/x-msvideo", StringComparison.OrdinalIgnoreCase);
    }

    private static void CleanupTemporaryFile(string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath)) return;
        try
        {
            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }
        catch
        {
            // ignorujemy błąd sprzątania
        }
    }

    private async Task RemoveExistingTranscriptionsAsync(int sessionId, CancellationToken cancellationToken)
    {
        var existing = await _db.SessionTranscriptions
            .Where(t => t.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0) return;

        DeleteTranscriptionFiles(existing);
        _db.SessionTranscriptions.RemoveRange(existing);
    }

    private void DeleteTranscriptionFiles(IEnumerable<SessionTranscription> transcriptions)
    {
        foreach (var transcription in transcriptions)
        {
            if (string.IsNullOrWhiteSpace(transcription.SourceFilePath)) continue;

            try
            {
                var absolute = MapRelativePathToAbsolute(transcription.SourceFilePath);
                if (System.IO.File.Exists(absolute))
                {
                    System.IO.File.Delete(absolute);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się usunąć pliku transkrypcji {Path}", transcription.SourceFilePath);
            }
        }
    }

    private string GetUploadsRoot()
    {
        var webRoot = GetWebRoot();
        var uploadsRoot = Path.Combine(webRoot, "uploads", "transcriptions");
        Directory.CreateDirectory(uploadsRoot);
        return uploadsRoot;
    }

    private string GetWebRoot()
    {
        return !string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? _environment.WebRootPath!
            : Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }

    private string MapRelativePathToAbsolute(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var webRoot = GetWebRoot();
        return Path.Combine(webRoot, normalized);
    }

    [HttpGet("patient/{patientId}/parameters-chart")]
    public async Task<IActionResult> GetParametersChart(int patientId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Sprawdź czy użytkownik ma dostęp do tego pacjenta
        var patient = await _db.Patients
            .Where(p => p.Id == patientId && (p.CreatedByUserId == userId || User.IsInRole(Roles.Administrator)))
            .FirstOrDefaultAsync();
        
        if (patient == null)
        {
            return NotFound();
        }

        // Pobierz wszystkie sesje pacjenta posortowane po dacie
        var sessions = await _db.Sessions
            .Where(s => s.PatientId == patientId)
            .OrderBy(s => s.StartDateTime)
            .ToListAsync();

        if (!sessions.Any())
        {
            return Ok(new List<object>());
        }

        // Oblicz numer tygodnia dla każdej sesji (od pierwszej sesji)
        var firstSessionDate = sessions.First().StartDateTime;
        var chartData = new List<object>();

        foreach (var session in sessions)
        {
            var weekNumber = (int)Math.Floor((session.StartDateTime - firstSessionDate).TotalDays / 7) + 1;
            
            // Pobierz parametry dla tej sesji
            var parameters = await _db.SessionParameters
                .Where(p => p.SessionId == session.Id)
                .ToListAsync();

            var lek = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "lęk")?.Value ?? 0;
            var smutek = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "smutek")?.Value ?? 0;
            var zlosc = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "złość")?.Value ?? 0;
            var radosc = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "radość")?.Value ?? 0;
            var problem1 = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "problem 1")?.Value ?? 0;
            var problem2 = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "problem 2")?.Value ?? 0;
            var problem3 = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "problem 3")?.Value ?? 0;
            var problem4 = parameters.FirstOrDefault(p => p.ParameterName.ToLower() == "problem 4")?.Value ?? 0;

            chartData.Add(new
            {
                SessionId = session.Id,
                SessionDate = session.StartDateTime,
                WeekNumber = weekNumber,
                Lek = lek,
                Depresja = smutek,
                Samopoczucie = zlosc,
                SkalaBecka = radosc,
                Problem1 = problem1,
                Problem2 = problem2,
                Problem3 = problem3,
                Problem4 = problem4
            });
        }

        return Ok(chartData);
    }

    [HttpPost("{sessionId}/parameters")]
    public async Task<IActionResult> SaveParameters(int sessionId, [FromBody] List<CreateSessionParameterRequest> parameters)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Sprawdź czy sesja istnieje i użytkownik ma do niej dostęp
        var session = await _db.Sessions
            .Where(s => s.Id == sessionId && (s.TerapeutaId == userId || User.IsInRole(Roles.Administrator)))
            .FirstOrDefaultAsync();
        
        if (session == null)
        {
            return NotFound();
        }

        // Usuń istniejące parametry dla tej sesji
        var existingParameters = await _db.SessionParameters
            .Where(p => p.SessionId == sessionId)
            .ToListAsync();
        _db.SessionParameters.RemoveRange(existingParameters);

        // Dodaj nowe parametry
        foreach (var param in parameters)
        {
            if (param.Value < 0 || param.Value > 10)
            {
                return BadRequest($"Wartość parametru {param.ParameterName} musi być między 0 a 10");
            }

            var sessionParameter = new SessionParameter
            {
                SessionId = sessionId,
                ParameterName = param.ParameterName.ToLower(),
                Value = param.Value,
                CreatedAt = DateTime.UtcNow
            };
            _db.SessionParameters.Add(sessionParameter);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    public sealed class CreateSessionParameterRequest
    {
        [Required]
        public string ParameterName { get; set; } = string.Empty;

        [Required]
        [Range(0, 10, ErrorMessage = "Wartość musi być między 0 a 10")]
        public int Value { get; set; }
    }
}

