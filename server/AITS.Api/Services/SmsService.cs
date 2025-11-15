using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.Extensions.Options;

namespace AITS.Api.Services;

/// <summary>
/// Integracja z SMSAPI z użyciem oficjalnego klienta SDK.
/// </summary>
public sealed class SmsService(ISmsApiClient smsApiClient, IOptions<SmsConfiguration> configuration, ILogger<SmsService> logger) : ISmsService
{
    private readonly ISmsApiClient _smsApiClient = smsApiClient;
    private readonly SmsConfiguration _configuration = configuration.Value;
    private readonly ILogger<SmsService> _logger = logger;

    public async Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedNumber = NormalizeTo48(CleanPhoneNumber(request.PhoneNumber));
            var sender = ValidateSender(request.SenderName ?? _configuration.SenderName);

            var response = await _smsApiClient.SendAsync(normalizedNumber, request.Message, sender, _configuration.TestMode, cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("SMS wysłany: id={Id} points={Points}", response.MessageId, response.Points);
                return new SmsSendResult(true, response.MessageId, null, response.Points, 1);
            }

            _logger.LogError("SMS wysyłka nieudana: {Error}", response.ErrorMessage);
            return new SmsSendResult(false, response.MessageId, response.ErrorMessage, response.Points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS unexpected error: {Message}", ex.Message);
            return new SmsSendResult(false, null, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<SmsStatusResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _smsApiClient.GetStatusAsync(messageId, cancellationToken);
            return new SmsStatusResult(
                messageId,
                response.Status,
                response.PhoneNumber,
                TryParseErrorCode(response.ErrorCode),
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching SMS status {MessageId}", messageId);
            return new SmsStatusResult(messageId, "ERROR", null, 500);
        }
    }

    public async Task<SmsBalanceResult> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _smsApiClient.GetBalanceAsync(cancellationToken);
            return new SmsBalanceResult(response.Points, "PLN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching SMS balance");
            return new SmsBalanceResult(0m, "PLN");
        }
    }

    private static string CleanPhoneNumber(string phoneNumber)
    {
        return new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }

    private static string NormalizeTo48(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return phone;
        }

        if (phone.StartsWith("+48", StringComparison.Ordinal))
        {
            return phone;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("48", StringComparison.Ordinal))
        {
            return "+" + digits;
        }

        if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length >= 10)
        {
            return "+48" + digits[1..];
        }

        if (digits.Length == 9)
        {
            return "+48" + digits;
        }

        return "+" + digits;
    }

    private string ValidateSender(string? senderName)
    {
        if (string.IsNullOrWhiteSpace(senderName))
        {
            _logger.LogWarning("Pusta nazwa nadawcy, używam domyślnej");
            senderName = _configuration.SenderName;
        }

        var normalized = senderName
            .Replace("ą", "a").Replace("ć", "c").Replace("ę", "e")
            .Replace("ł", "l").Replace("ń", "n").Replace("ó", "o")
            .Replace("ś", "s").Replace("ź", "z").Replace("ż", "z")
            .Replace("Ą", "A").Replace("Ć", "C").Replace("Ę", "E")
            .Replace("Ł", "L").Replace("Ń", "N").Replace("Ó", "O")
            .Replace("Ś", "S").Replace("Ź", "Z").Replace("Ż", "Z");

        var cleaned = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        if (cleaned.Length > 11)
        {
            cleaned = cleaned[..11];
            _logger.LogInformation("Nazwa nadawcy skrócona do: {Sender}", cleaned);
        }

        if (string.IsNullOrEmpty(cleaned) || cleaned.Length < 2)
        {
            _logger.LogWarning("Nieprawidłowa nazwa nadawcy {SenderName}, używam awaryjnej wartości", senderName);
            return "SMSAPI";
        }

        return cleaned.ToUpperInvariant();
    }

    private static int? TryParseErrorCode(string? value)
    {
        if (int.TryParse(value, out var code))
        {
            return code;
        }

        return null;
    }
}

