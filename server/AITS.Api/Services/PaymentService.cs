using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AITS.Api.Services;

public sealed class PaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _apiUrl;
    private readonly string? _notificationUrl;
    private readonly AppDbContext _db;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(HttpClient httpClient, AppDbContext db, IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;
        _clientId = configuration["Tpay:ClientId"] ?? throw new InvalidOperationException("Tpay:ClientId not configured");
        _clientSecret = configuration["Tpay:ClientSecret"] ?? throw new InvalidOperationException("Tpay:ClientSecret not configured");
        _apiUrl = configuration["Tpay:ApiUrl"] ?? "https://openapi.sandbox.tpay.com";
        _notificationUrl = configuration["Tpay:NotificationUrl"];
    }

    // Metoda GenerateMd5 nie jest już używana - Tpay API v3 używa OAuth2 zamiast MD5 hash
    // Zachowana dla kompatybilności wstecznej, jeśli będzie potrzebna w przyszłości

    public async Task<(string? PaymentUrl, string? TransactionId)> CreatePaymentAsync(int sessionId, decimal amount, string payerEmail, string payerName)
    {
        try
        {
            // Tpay API v3 - OAuth2 authentication
            _logger.LogInformation("Initiating Tpay OAuth2 authentication for session {SessionId}", sessionId);
            
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/oauth/auth")
            {
                Content = JsonContent.Create(new
                {
                    client_id = _clientId,
                    client_secret = _clientSecret,
                    grant_type = "client_credentials"
                }, options: new System.Text.Json.JsonSerializerOptions())
            };
            tokenRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("Tpay OAuth2 authentication failed. Status: {Status}, Response: {Response}", 
                    tokenResponse.StatusCode, errorContent);
                return (null, null);
            }
            
            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            
            if (!tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                _logger.LogError("Tpay OAuth2 response missing access_token. Response: {Response}", 
                    await tokenResponse.Content.ReadAsStringAsync());
                return (null, null);
            }
            
            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Tpay OAuth2 access_token is null or empty");
                return (null, null);
            }
            
            _logger.LogInformation("Tpay OAuth2 authentication successful");
            
            var notificationUrl = !string.IsNullOrEmpty(_notificationUrl) 
                ? _notificationUrl 
                : $"https://your-domain.com/api/payments/notify"; // TODO: Configure proper domain
            
            // Format zgodny z Tpay API v3 - /transactions endpoint
            var requestData = new
            {
                amount = amount,
                description = $"Płatność za sesję {sessionId}",
                hiddenDescription = $"session_{sessionId}",
                lang = "pl",
                payer = new
                {
                    email = payerEmail,
                    name = payerName
                },
                pay = new
                {
                    blikPaymentData = (object?)null,
                    cardPaymentData = (object?)null,
                    tokenPaymentData = (object?)null
                },
                callbacks = new
                {
                    payerUrls = new
                    {
                        success = (string?)null,
                        error = (string?)null
                    },
                    notification = new
                    {
                        url = notificationUrl,
                        email = (string?)null
                    }
                }
            };

            var paymentRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/transactions")
            {
                Content = JsonContent.Create(requestData)
            };
            paymentRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            _logger.LogInformation("Creating Tpay transaction for session {SessionId}, amount: {Amount}, payer: {PayerEmail}", 
                sessionId, amount, payerEmail);
            
            var response = await _httpClient.SendAsync(paymentRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Tpay transaction creation failed. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);
                return (null, null);
            }
            
            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            // Sprawdź różne możliwe nazwy pól w odpowiedzi
            string? transactionId = null;
            string? paymentUrl = null;
            
            if (responseData.TryGetProperty("transactionId", out var transactionIdElement))
                transactionId = transactionIdElement.GetString();
            else if (responseData.TryGetProperty("transaction_id", out var transactionIdElement2))
                transactionId = transactionIdElement2.GetString();
            
            if (responseData.TryGetProperty("transactionPaymentUrl", out var paymentUrlElement))
                paymentUrl = paymentUrlElement.GetString();
            else if (responseData.TryGetProperty("paymentUrl", out var paymentUrlElement2))
                paymentUrl = paymentUrlElement2.GetString();
            else if (responseData.TryGetProperty("payment_url", out var paymentUrlElement3))
                paymentUrl = paymentUrlElement3.GetString();
            
            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(paymentUrl))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Tpay transaction response missing required fields. Response: {Response}", responseContent);
                return (null, null);
            }
            
            _logger.LogInformation("Tpay transaction created successfully. TransactionId: {TransactionId}", transactionId);
            
            return (paymentUrl, transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Tpay payment for session {SessionId}", sessionId);
            return (null, null);
        }
    }

    public async Task<bool> ProcessNotificationAsync(string transactionId, string status)
    {
        try
        {
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.TpayTransactionId == transactionId);
            
            if (payment is null) return false;
            
            // Tpay API v3 statusy: "paid", "correct", "pending", "refund", "canceled"
            if (status == "paid" || status == "correct")
            {
                payment.StatusId = (int)PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                
                var session = await _db.Sessions
                    .FirstOrDefaultAsync(s => s.PaymentId == payment.Id);
                if (session != null && session.StatusId == (int)SessionStatus.Scheduled)
                {
                    session.StatusId = (int)SessionStatus.Confirmed;
                }
            }
            else if (status == "canceled" || status == "fail")
            {
                payment.StatusId = (int)PaymentStatus.Failed;
            }
            // status == "pending" - pozostawiamy jako Pending
            // status == "refund" - można dodać obsługę zwrotów w przyszłości
            
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

