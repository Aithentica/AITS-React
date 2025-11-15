using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITS.Api.Services;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PaymentService _paymentService;

    public PaymentsController(AppDbContext db, PaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    public sealed record CreatePaymentRequest(int SessionId, decimal Amount);

    [HttpPost("create")]
    [Authorize(Policy = "IsTherapistOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.Sessions
            .Include(s => s.Patient)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TerapeutaId == userId);
        
        if (session is null) return NotFound();
        if (session.PaymentId.HasValue) return BadRequest(new { error = "Payment already exists for this session" });
        
        // Utworzenie płatności przez Tpay
        var payerEmail = session.Patient.Email;
        var payerName = $"{session.Patient.FirstName} {session.Patient.LastName}".Trim();
        if (string.IsNullOrEmpty(payerName))
            payerName = payerEmail;
        
        var (paymentUrl, transactionId) = await _paymentService.CreatePaymentAsync(
            request.SessionId, 
            request.Amount, 
            payerEmail, 
            payerName);
        
        if (string.IsNullOrEmpty(paymentUrl) || string.IsNullOrEmpty(transactionId))
            return BadRequest(new { error = "Failed to create payment" });
        
        var payment = new Payment
        {
            SessionId = request.SessionId,
            Amount = request.Amount,
            StatusId = (int)PaymentStatus.Pending,
            TpayTransactionId = transactionId
        };
        
        _db.Payments.Add(payment);
        session.PaymentId = payment.Id;
        await _db.SaveChangesAsync();
        
        return Ok(new { paymentId = payment.Id, paymentUrl, transactionId = payment.TpayTransactionId });
    }

    [HttpPost("notify")]
    [AllowAnonymous]
    public async Task<IActionResult> Notify([FromBody] Dictionary<string, object> notification)
    {
        try
        {
            // Tpay API v3 wysyła transactionId (nie title) w notyfikacji
            var transactionId = notification.ContainsKey("transactionId") 
                ? notification["transactionId"]?.ToString() 
                : (notification.ContainsKey("title") ? notification["title"]?.ToString() : null);
            
            var status = notification.ContainsKey("status") ? notification["status"]?.ToString() : null;
            
            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(status))
                return BadRequest(new { error = "Invalid notification data" });
            
            var processed = await _paymentService.ProcessNotificationAsync(transactionId, status);
            
            return processed ? Ok(new { message = "Payment notification processed" }) 
                : BadRequest(new { error = "Failed to process notification" });
        }
        catch
        {
            return BadRequest(new { error = "Invalid notification format" });
        }
    }
}

