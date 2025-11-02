using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AITS.Tests;

public class SmsServiceTests
{
    [Fact]
    public async Task SendSmsAsync_ShouldReturnTrue_WhenSmsIsSent()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SMS:ApiToken", "test-token" },
                { "SMS:SenderName", "AITerapia" },
                { "SMS:ApiUrl", "https://api.smsapi.pl/sms.do" }
            })
            .Build();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var httpClient = new HttpClient(handler.Object);
        var logger = new Mock<ILogger<AITS.Api.Services.SmsService>>();
        var service = new AITS.Api.Services.SmsService(httpClient, configuration, logger.Object);

        // Act
        var result = await service.SendSmsAsync("+48123456789", "Test message");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SendSmsAsync_ShouldReturnFalse_WhenRequestFails()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SMS:ApiToken", "test-token" },
                { "SMS:SenderName", "AITerapia" },
                { "SMS:ApiUrl", "https://api.smsapi.pl/sms.do" }
            })
            .Build();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        var httpClient = new HttpClient(handler.Object);
        var logger = new Mock<ILogger<AITS.Api.Services.SmsService>>();
        var service = new AITS.Api.Services.SmsService(httpClient, configuration, logger.Object);

        // Act
        var result = await service.SendSmsAsync("+48123456789", "Test message");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Exception", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}


