using AITS.Api.Configuration;
using AITS.Api.Services;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AITS.Tests;

public class SmsServiceTests
{
    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenClientReturnsSuccess()
    {
        var smsApiClientMock = new Mock<ISmsApiClient>();
        smsApiClientMock
            .Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsApiSendResponse(true, "msg-1", 0.3m, null));

        var options = Options.Create(new SmsConfiguration
        {
            ApiToken = "test-token",
            SenderName = "AITerapia",
            TestMode = true
        });

        var logger = new Mock<ILogger<SmsService>>();
        var service = new SmsService(smsApiClientMock.Object, options, logger.Object);

        var request = new SmsSendRequest("123456789", "Test message");

        var result = await service.SendAsync(request);

        Assert.True(result.Success);
        Assert.Equal("msg-1", result.MessageId);
        smsApiClientMock.Verify(c => c.SendAsync("+48123456789", "Test message", "AITERAPIA", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenClientReturnsError()
    {
        var smsApiClientMock = new Mock<ISmsApiClient>();
        smsApiClientMock
            .Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsApiSendResponse(false, null, null, "Invalid token"));

        var options = Options.Create(new SmsConfiguration
        {
            ApiToken = "test-token",
            SenderName = "AITerapia",
            TestMode = true
        });

        var logger = new Mock<ILogger<SmsService>>();
        var service = new SmsService(smsApiClientMock.Object, options, logger.Object);

        var result = await service.SendAsync(new SmsSendRequest("+48123456789", "Test message"));

        Assert.False(result.Success);
        Assert.Equal("Invalid token", result.Error);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnStatusFromClient()
    {
        var smsApiClientMock = new Mock<ISmsApiClient>();
        smsApiClientMock
            .Setup(c => c.GetStatusAsync("msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsApiStatusResponse("SENT", "+48123456789", null));

        var options = Options.Create(new SmsConfiguration
        {
            ApiToken = "test-token",
            SenderName = "AITerapia",
            TestMode = true
        });

        var logger = new Mock<ILogger<SmsService>>();
        var service = new SmsService(smsApiClientMock.Object, options, logger.Object);

        var result = await service.GetStatusAsync("msg-1");

        Assert.Equal("SENT", result.Status);
        Assert.Equal("+48123456789", result.PhoneNumber);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnBalanceFromClient()
    {
        var smsApiClientMock = new Mock<ISmsApiClient>();
        smsApiClientMock
            .Setup(c => c.GetBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsApiBalanceResponse(12.5m));

        var options = Options.Create(new SmsConfiguration
        {
            ApiToken = "test-token",
            SenderName = "AITerapia",
            TestMode = true
        });

        var logger = new Mock<ILogger<SmsService>>();
        var service = new SmsService(smsApiClientMock.Object, options, logger.Object);

        var result = await service.GetBalanceAsync();

        Assert.Equal(12.5m, result.Balance);
        Assert.Equal("PLN", result.Currency);
    }
}
