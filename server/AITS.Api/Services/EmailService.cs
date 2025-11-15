using System.Net;
using System.Net.Mail;
using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.Extensions.Options;

namespace AITS.Api.Services;

public sealed class EmailService(IOptions<EmailConfiguration> configuration, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailConfiguration _configuration = configuration.Value;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task<bool> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtp = CreateSmtpClient();
            using var message = BuildMessage(request);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await smtp.SendMailAsync(message, linkedCts.Token);

            _logger.LogInformation("Email wysłany do {Recipient} z tematem {Subject}", request.To, request.Subject);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Wysyłanie emaila przerwane dla odbiorcy {Recipient}", request.To);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wysyłania emaila do {Recipient}", request.To);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtp = CreateSmtpClient();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await smtp.SendMailAsync(new MailMessage(_configuration.FromEmail, _configuration.FromEmail)
            {
                Subject = "SMTP connectivity test",
                Body = "Testing SMTP connectivity.",
                IsBodyHtml = false
            }, linkedCts.Token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nie udało się przetestować połączenia SMTP");
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        return new SmtpClient(_configuration.SmtpHost, _configuration.SmtpPort)
        {
            EnableSsl = _configuration.UseSsl,
            Credentials = new NetworkCredential(_configuration.Username, _configuration.Password)
        };
    }

    private MailMessage BuildMessage(EmailSendRequest request)
    {
        var from = new MailAddress(
            request.FromEmailOverride ?? _configuration.FromEmail,
            request.FromNameOverride ?? _configuration.FromName);

        var to = new MailAddress(request.To);
        var message = new MailMessage(from, to)
        {
            Subject = request.Subject,
            Body = request.Body,
            IsBodyHtml = request.IsHtml
        };

        return message;
    }
}



