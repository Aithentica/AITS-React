using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace AITS.Api.Services;

public sealed class PaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _apiUrl;
    private readonly AppDbContext _db;

    public PaymentService(HttpClient httpClient, AppDbContext db, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _db = db;
        _clientId = configuration["Tpay:ClientId"] ?? throw new InvalidOperationException("Tpay:ClientId not configured");
        _clientSecret = configuration["Tpay:ClientSecret"] ?? throw new InvalidOperationException("Tpay:ClientSecret not configured");
        _apiUrl = configuration["Tpay:ApiUrl"] ?? "https://api.sandbox.tpay.com";
    }

    private string GenerateMd5(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public async Task<(string? PaymentUrl, string? TransactionId)> CreatePaymentAsync(int sessionId, decimal amount)
    {
        try
        {
            // Tpay API v3 - OAuth2 authentication
            var tokenResponse = await _httpClient.PostAsJsonAsync($"{_apiUrl}/oauth/auth", new
            {
                client_id = _clientId,
                client_secret = _clientSecret,
                grant_type = "client_credentials"
            });
            
            if (!tokenResponse.IsSuccessStatusCode) return (null, null);
            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenData.GetProperty("access_token").GetString();
            
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            var amountString = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var requestData = new
            {
                amount = amountString,
                description = $"Płatność za sesję {sessionId}",
                currency = "PLN",
                callback = new { success = $"{_apiUrl}/payments/notify" }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/transactions", requestData);
            if (!response.IsSuccessStatusCode) return (null, null);
            
            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var transactionId = responseData.GetProperty("transactionId").GetString();
            var paymentUrl = responseData.GetProperty("paymentUrl").GetString();
            
            return (paymentUrl, transactionId);
        }
        catch
        {
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
            
            if (status == "paid")
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
            else if (status == "fail")
            {
                payment.StatusId = (int)PaymentStatus.Failed;
            }
            
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

