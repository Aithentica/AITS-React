using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/patient/sessions")]
[Authorize(Policy = "IsPatient")]
public sealed class PatientSessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PaymentService _paymentService;

    public PatientSessionsController(AppDbContext db, PaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMySessions()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var sessions = await _db.Sessions
            .Where(s => s.Patient.Email == userEmail)
            .Include(s => s.Patient)
            .Include(s => s.Payment)
            .OrderByDescending(s => s.StartDateTime)
            .Select(s => new
            {
                s.Id,
                Patient = new { s.Patient.Id, s.Patient.FirstName, s.Patient.LastName, s.Patient.Email },
                s.StartDateTime,
                s.EndDateTime,
                s.StatusId,
                s.Price,
                s.GoogleMeetLink,
                Payment = s.Payment != null ? new
                {
                    s.Payment.Id,
                    s.Payment.StatusId,
                    s.Payment.Amount,
                    s.Payment.CreatedAt,
                    s.Payment.CompletedAt
                } : null,
                IsPaid = s.Payment != null && s.Payment.StatusId == (int)PaymentStatus.Completed
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var session = await _db.Sessions
            .Where(s => s.Id == id && s.Patient.Email == userEmail)
            .Include(s => s.Patient)
            .Include(s => s.Payment)
            .Select(s => new
            {
                s.Id,
                Patient = new { s.Patient.Id, s.Patient.FirstName, s.Patient.LastName, s.Patient.Email },
                s.StartDateTime,
                s.EndDateTime,
                s.StatusId,
                s.Price,
                s.GoogleMeetLink,
                Payment = s.Payment != null ? new
                {
                    s.Payment.Id,
                    s.Payment.StatusId,
                    s.Payment.Amount,
                    s.Payment.CreatedAt,
                    s.Payment.CompletedAt,
                    s.Payment.TpayTransactionId
                } : null,
                IsPaid = s.Payment != null && s.Payment.StatusId == (int)PaymentStatus.Completed,
                CanPay = s.Payment == null || s.Payment.StatusId == (int)PaymentStatus.Pending || s.Payment.StatusId == (int)PaymentStatus.Failed
            })
            .FirstOrDefaultAsync();

        if (session is null) return NotFound();
        return Ok(session);
    }

    [HttpPost("{id}/initiate-payment")]
    public async Task<IActionResult> InitiatePayment(int id)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var session = await _db.Sessions
            .Include(s => s.Patient)
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == id && s.Patient.Email == userEmail);

        if (session is null) return NotFound();

        // Sprawdź czy sesja już ma płatność zrealizowaną
        if (session.Payment != null && session.Payment.StatusId == (int)PaymentStatus.Completed)
            return BadRequest(new { error = "Sesja jest już opłacona" });

        // Jeśli płatność istnieje ale jest pending/failed, użyj istniejącej
        Payment? payment = null;
        string? paymentUrl = null;
        string? transactionId = null;

        var payerEmail = session.Patient.Email;
        var payerName = $"{session.Patient.FirstName} {session.Patient.LastName}".Trim();
        if (string.IsNullOrEmpty(payerName))
            payerName = payerEmail;

        if (session.PaymentId.HasValue && session.Payment != null)
        {
            payment = session.Payment;
            // Jeśli płatność jest pending lub failed, utwórz nową
            if (payment.StatusId == (int)PaymentStatus.Pending || payment.StatusId == (int)PaymentStatus.Failed)
            {
                // Utwórz nową płatność przez Tpay
                (paymentUrl, transactionId) = await _paymentService.CreatePaymentAsync(
                    session.Id, 
                    session.Price, 
                    payerEmail, 
                    payerName);
                
                if (string.IsNullOrEmpty(paymentUrl) || string.IsNullOrEmpty(transactionId))
                    return BadRequest(new { error = "Nie udało się utworzyć płatności" });

                payment.StatusId = (int)PaymentStatus.Pending;
                payment.TpayTransactionId = transactionId;
                payment.Amount = session.Price;
                await _db.SaveChangesAsync();
            }
            else
            {
                return BadRequest(new { error = "Nie można zainicjować płatności dla tej sesji" });
            }
        }
        else
        {
            // Utwórz nową płatność
            (paymentUrl, transactionId) = await _paymentService.CreatePaymentAsync(
                session.Id, 
                session.Price, 
                payerEmail, 
                payerName);
            
            if (string.IsNullOrEmpty(paymentUrl) || string.IsNullOrEmpty(transactionId))
                return BadRequest(new { error = "Nie udało się utworzyć płatności" });

            payment = new Payment
            {
                SessionId = session.Id,
                Amount = session.Price,
                StatusId = (int)PaymentStatus.Pending,
                TpayTransactionId = transactionId
            };

            _db.Payments.Add(payment);
            session.PaymentId = payment.Id;
            await _db.SaveChangesAsync();
        }

        return Ok(new { paymentId = payment.Id, paymentUrl, transactionId = payment.TpayTransactionId });
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMyMetrics()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var patient = await _db.Patients
            .Where(p => p.Email == userEmail)
            .FirstOrDefaultAsync();

        if (patient == null)
            return NotFound();

        var sessions = await _db.Sessions
            .Where(s => s.PatientId == patient.Id)
            .OrderBy(s => s.StartDateTime)
            .ToListAsync();

        if (!sessions.Any())
        {
            return Ok(new List<object>());
        }

        var firstSessionDate = sessions.First().StartDateTime;
        var chartData = new List<object>();

        foreach (var session in sessions)
        {
            var weekNumber = (int)Math.Floor((session.StartDateTime - firstSessionDate).TotalDays / 7) + 1;
            
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

    [HttpGet("tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var patient = await _db.Patients
            .Where(p => p.Email == userEmail)
            .FirstOrDefaultAsync();

        if (patient == null)
            return NotFound();

        var tasks = await _db.PatientTasks
            .Where(t => t.PatientId == patient.Id)
            .Include(t => t.Therapist)
            .Include(t => t.Session)
            .OrderByDescending(t => t.DueDate)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.DueDate,
                t.IsCompleted,
                t.CompletedAt,
                t.CreatedAt,
                TherapistName = t.Therapist.UserName ?? t.Therapist.Email ?? "Nieznany",
                SessionDate = t.Session != null ? t.Session.StartDateTime : (DateTime?)null
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("diaries")]
    public async Task<IActionResult> GetMyDiaries([FromQuery] int? limit = 10)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var patient = await _db.Patients
            .Where(p => p.Email == userEmail)
            .FirstOrDefaultAsync();

        if (patient == null)
            return NotFound();

        var query = _db.PatientDiaries
            .Where(d => d.PatientId == patient.Id)
            .OrderByDescending(d => d.EntryDate)
            .ThenByDescending(d => d.CreatedAt);

        if (limit.HasValue && limit.Value > 0)
        {
            query = (IOrderedQueryable<PatientDiary>)query.Take(limit.Value);
        }

        var diaries = await query
            .Select(d => new
            {
                d.Id,
                d.EntryDate,
                d.Title,
                d.Content,
                d.Mood,
                d.MoodRating,
                d.CreatedAt,
                d.UpdatedAt
            })
            .ToListAsync();

        return Ok(diaries);
    }
}
