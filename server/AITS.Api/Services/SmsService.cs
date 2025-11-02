using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AITS.Api.Services;

public sealed class SmsService
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _senderName;
    private readonly string _apiUrl;
    private readonly ILogger<SmsService> _logger;

    public SmsService(HttpClient httpClient, IConfiguration configuration, ILogger<SmsService> logger)
    {
        _httpClient = httpClient;
        _token = configuration["SMS:ApiToken"] ?? throw new InvalidOperationException("SMS:ApiToken not configured");
        _senderName = configuration["SMS:SenderName"] ?? "AITerapia";
        _apiUrl = configuration["SMS:ApiUrl"] ?? "https://api.smsapi.pl/sms.do";
        _logger = logger;
        
        // SMSAPI może używać autoryzacji OAuth Bearer w nagłówku lub tokena w formularzu
        // Najpierw spróbujemy z tokenem w formularzu (standardowy sposób)
        _httpClient.DefaultRequestHeaders.Clear();
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            // SMSAPI wymaga form-data (application/x-www-form-urlencoded)
            // Autoryzacja może być:
            // 1. OAuth Bearer token w nagłówku (obecna implementacja)
            // 2. Token OAuth w formularzu jako parametr 'access_token'
            // Spróbujemy obu metod
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("to", phoneNumber),
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("from", _senderName),
                // Dodajemy token w formularzu jako alternatywa
                new KeyValuePair<string, string>("access_token", _token)
            };

            var content = new FormUrlEncodedContent(formData);
            
            // SMSAPI wymaga tokena w formularzu jako 'access_token' (nie w nagłówku Authorization)
            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("SMSAPI Response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SMSAPI Error: Status={StatusCode}, Content={Content}", 
                    response.StatusCode, responseContent);
                return (false, $"SMSAPI Error {response.StatusCode}: {responseContent}");
            }
            
            // SMSAPI zwraca tekst, sprawdzamy czy nie ma błędów w odpowiedzi
            if (responseContent.Contains("ERROR:") || responseContent.Contains("error"))
            {
                _logger.LogError("SMSAPI Error in response: {Content}", responseContent);
                return (false, $"SMSAPI Error: {responseContent}");
            }
            
            _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending SMS to {PhoneNumber}", phoneNumber);
            return (false, $"Exception: {ex.Message}");
        }
    }
}

