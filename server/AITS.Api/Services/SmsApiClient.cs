using System.Globalization;
using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using SMSApi.Api;

namespace AITS.Api.Services;

public sealed class SmsApiClient(IOptions<SmsConfiguration> configuration, ILogger<SmsApiClient> logger) : ISmsApiClient
{
    private readonly SmsConfiguration _configuration = configuration.Value;
    private readonly ILogger<SmsApiClient> _logger = logger;

    public async Task<SmsApiSendResponse> SendAsync(string phoneNumber, string message, string sender, bool testMode, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var factory = new SMSFactory(client, ProxyAddress.SmsApiPl);

        var action = factory.ActionSend()
            .SetTo(phoneNumber)
            .SetText(message)
            .SetSender(sender);

        if (testMode)
        {
            action.SetTest();
        }

        try
        {
            var result = await action.ExecuteAsync().WaitAsync(cancellationToken);
            return MapSendResult(result);
        }
        catch (ClientException clientEx)
        {
            _logger.LogError(clientEx, "SMSAPI ClientException: {Message}", clientEx.Message);
            return new SmsApiSendResponse(false, null, null, clientEx.Message);
        }
        catch (ActionException actionEx)
        {
            _logger.LogError(actionEx, "SMSAPI ActionException: {Message}", actionEx.Message);
            return new SmsApiSendResponse(false, null, null, actionEx.Message);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "SMSAPI unexpected error: {Message}", ex.Message);
            return new SmsApiSendResponse(false, null, null, ex.Message);
        }
    }

    public async Task<SmsApiStatusResponse> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var factory = new SMSFactory(client, ProxyAddress.SmsApiPl);

        try
        {
            var result = await factory.ActionGet(messageId).ExecuteAsync().WaitAsync(cancellationToken);

            if (result.Count > 0 && result.List.Count > 0)
            {
                var status = result.List[0];
                return new SmsApiStatusResponse(status.Status, status.Number, status.Error?.ToString());
            }

            _logger.LogWarning("SMSAPI status not found for {MessageId}", messageId);
            return new SmsApiStatusResponse("NOT_FOUND", null, "404");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "SMSAPI error while fetching status for {MessageId}", messageId);
            return new SmsApiStatusResponse("ERROR", null, "500");
        }
    }

    public async Task<SmsApiBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var userFactory = new UserFactory(client, ProxyAddress.SmsApiPl);

        try
        {
            var credits = await userFactory.ActionGetCredits().ExecuteAsync().WaitAsync(cancellationToken);
            return new SmsApiBalanceResponse((decimal)credits.Points);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "SMSAPI error while fetching balance");
            return new SmsApiBalanceResponse(0);
        }
    }

    private ClientOAuth CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_configuration.ApiToken))
        {
            throw new InvalidOperationException("Brak konfiguracji tokenu SMSAPI");
        }

        return new ClientOAuth(_configuration.ApiToken);
    }

    private SmsApiSendResponse MapSendResult(dynamic result)
    {
        if (result is null)
        {
            _logger.LogError("SMSAPI zwróciło null przy wysyłce");
            return new SmsApiSendResponse(false, null, null, "Null response");
        }

        if (result.Count == 0 || result.List.Count == 0)
        {
            _logger.LogError("SMSAPI zwróciło pustą odpowiedź przy wysyłce");
            return new SmsApiSendResponse(false, null, null, "Empty response");
        }

        dynamic message = result.List[0];
        var points = TryConvertToDecimal(message.Points);
        if (message.isError())
        {
            return new SmsApiSendResponse(false, (string?)message.ID, points, message.Error?.ToString(), (string?)message.Number);
        }

        return new SmsApiSendResponse(true, (string?)message.ID, points, null, (string?)message.Number);
    }

    private static decimal? TryConvertToDecimal(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is decimal dec)
        {
            return dec;
        }

        if (value is double dbl)
        {
            return (decimal)dbl;
        }

        if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }
}

