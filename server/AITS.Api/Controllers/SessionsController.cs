using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Terapeuta + "," + Roles.TerapeutaFreeAccess + "," + Roles.Administrator)]
public sealed class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly SmsService _smsService;

    public SessionsController(AppDbContext db, GoogleCalendarService googleCalendarService, SmsService smsService)
    {
        _db = db;
        _googleCalendarService = googleCalendarService;
        _smsService = smsService;
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
                s.GoogleMeetLink
            })
            .ToListAsync();
        
        return Ok(sessions);
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
                HasPayment = s.PaymentId.HasValue
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
                Payment = s.Payment != null ? new { s.Payment.StatusId, s.Payment.Amount, s.Payment.TpayTransactionId } : null,
                s.Notes,
                s.CreatedAt,
                s.UpdatedAt
            })
            .FirstOrDefaultAsync();
        
        if (session is null) return NotFound();
        return Ok(session);
    }

    public sealed record CreateSessionRequest(
        int PatientId,
        DateTime StartDateTime,
        int DurationMinutes,
        decimal Price,
        string? Notes);

    public sealed record UpdateSessionRequest(
        DateTime StartDateTime,
        int DurationMinutes,
        decimal Price,
        string? Notes);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var patient = await _db.Patients
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.Id == request.PatientId);
        if (patient is null) return BadRequest(new { error = "Patient not found" });
        
        var endDateTime = request.StartDateTime.AddMinutes(request.DurationMinutes);
        var session = new Session
        {
            PatientId = request.PatientId,
            TerapeutaId = userId,
            StartDateTime = request.StartDateTime,
            EndDateTime = endDateTime,
            StatusId = (int)SessionStatus.Scheduled,
            Price = request.Price,
            Notes = request.Notes
        };
        
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        
        // Utworzenie wydarzenia Google Calendar z linkiem Google Meet
        try
        {
            session.Patient = patient; // Ustawienie dla GoogleCalendarService
            var eventId = await _googleCalendarService.CreateEventAsync(session);
            if (!string.IsNullOrEmpty(eventId))
            {
                session.GoogleCalendarEventId = eventId;
                var meetLink = await _googleCalendarService.GetMeetLinkAsync(eventId);
                if (!string.IsNullOrEmpty(meetLink))
                {
                    session.GoogleMeetLink = meetLink;
                }
                await _db.SaveChangesAsync();
            }
        }
        catch
        {
            // Log error but continue - Google Calendar is optional
        }
        
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
        session.StartDateTime = request.StartDateTime;
        session.EndDateTime = endDateTime;
        session.Price = request.Price;
        session.Notes = request.Notes;
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
                await _googleCalendarService.DeleteEventAsync(session.GoogleCalendarEventId);
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
        
            // Wysłanie SMS
            var (smsSent, smsError) = await _smsService.SendSmsAsync(session.Patient.Phone, message);
            
            // TODO: Wysłanie Email - implementacja w kolejnym kroku
            
            return Ok(new { message = "Notification sent", sessionId = session.Id, smsSent, smsError });
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
            var (smsSent, smsError) = await _smsService.SendSmsAsync(request.PhoneNumber, request.Message);
            
            if (smsSent)
            {
                return Ok(new 
                { 
                    success = true, 
                    message = "SMS sent successfully", 
                    phoneNumber = request.PhoneNumber,
                    messageText = request.Message,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = "Failed to send SMS - SMSAPI returned error",
                    details = smsError ?? "Unknown error"
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
}

