using AITS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AITS.Tests;

public class PaymentServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _dbContext;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Tpay:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["Tpay:ClientSecret"]).Returns("test-client-secret");
        _configurationMock.Setup(c => c["Tpay:ApiUrl"]).Returns("https://openapi.sandbox.tpay.com");
        _configurationMock.Setup(c => c["Tpay:NotificationUrl"]).Returns("https://test.com/api/payments/notify");

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _paymentService = new PaymentService(
            _httpClient,
            _dbContext,
            _configurationMock.Object,
            NullLogger<PaymentService>.Instance
        );
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnPaymentUrlAndTransactionId_WhenOAuthAndTransactionSucceed()
    {
        // Arrange
        var accessToken = "test-access-token";
        var transactionId = "01K1C16DV2ZV6QZT11GNB4S3EA";
        var paymentUrl = "https://secure.tpay.com/marketplace?id=01K1C16DV2ZV6QZT11GNB4S3EA";

        // Mock OAuth response
        var oauthResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { access_token = accessToken }), Encoding.UTF8, "application/json")
        };

        // Mock transaction creation response
        var transactionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                transactionId = transactionId,
                paymentUrl = paymentUrl
            }), Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(oauthResponse)
            .ReturnsAsync(transactionResponse);

        // Act
        var result = await _paymentService.CreatePaymentAsync(1, 100.00m, "test@example.com", "Test User");

        // Assert
        Assert.NotNull(result.PaymentUrl);
        Assert.NotNull(result.TransactionId);
        Assert.Equal(paymentUrl, result.PaymentUrl);
        Assert.Equal(transactionId, result.TransactionId);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnNull_WhenOAuthFails()
    {
        // Arrange
        var oauthResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Invalid credentials", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(oauthResponse);

        // Act
        var result = await _paymentService.CreatePaymentAsync(1, 100.00m, "test@example.com", "Test User");

        // Assert
        Assert.Null(result.PaymentUrl);
        Assert.Null(result.TransactionId);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnNull_WhenTransactionCreationFails()
    {
        // Arrange
        var accessToken = "test-access-token";
        var oauthResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { access_token = accessToken }), Encoding.UTF8, "application/json")
        };

        var transactionResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid request", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(oauthResponse)
            .ReturnsAsync(transactionResponse);

        // Act
        var result = await _paymentService.CreatePaymentAsync(1, 100.00m, "test@example.com", "Test User");

        // Assert
        Assert.Null(result.PaymentUrl);
        Assert.Null(result.TransactionId);
    }

    [Fact]
    public async Task ProcessNotificationAsync_ShouldUpdatePaymentStatusToCompleted_WhenStatusIsPaid()
    {
        // Arrange
        var transactionId = "01K1C16DV2ZV6QZT11GNB4S3EA";
        var payment = new Payment
        {
            SessionId = 1,
            Amount = 100.00m,
            StatusId = (int)PaymentStatus.Pending,
            TpayTransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _paymentService.ProcessNotificationAsync(transactionId, "paid");

        // Assert
        Assert.True(result);
        var updatedPayment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.TpayTransactionId == transactionId);
        Assert.NotNull(updatedPayment);
        Assert.Equal((int)PaymentStatus.Completed, updatedPayment.StatusId);
        Assert.NotNull(updatedPayment.CompletedAt);
    }

    [Fact]
    public async Task ProcessNotificationAsync_ShouldUpdatePaymentStatusToCompleted_WhenStatusIsCorrect()
    {
        // Arrange
        var transactionId = "01K1C16DV2ZV6QZT11GNB4S3EA";
        var payment = new Payment
        {
            SessionId = 1,
            Amount = 100.00m,
            StatusId = (int)PaymentStatus.Pending,
            TpayTransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _paymentService.ProcessNotificationAsync(transactionId, "correct");

        // Assert
        Assert.True(result);
        var updatedPayment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.TpayTransactionId == transactionId);
        Assert.NotNull(updatedPayment);
        Assert.Equal((int)PaymentStatus.Completed, updatedPayment.StatusId);
    }

    [Fact]
    public async Task ProcessNotificationAsync_ShouldUpdatePaymentStatusToFailed_WhenStatusIsCanceled()
    {
        // Arrange
        var transactionId = "01K1C16DV2ZV6QZT11GNB4S3EA";
        var payment = new Payment
        {
            SessionId = 1,
            Amount = 100.00m,
            StatusId = (int)PaymentStatus.Pending,
            TpayTransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _paymentService.ProcessNotificationAsync(transactionId, "canceled");

        // Assert
        Assert.True(result);
        var updatedPayment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.TpayTransactionId == transactionId);
        Assert.NotNull(updatedPayment);
        Assert.Equal((int)PaymentStatus.Failed, updatedPayment.StatusId);
    }

    [Fact]
    public async Task ProcessNotificationAsync_ShouldUpdateSessionStatusToConfirmed_WhenPaymentIsCompleted()
    {
        // Arrange
        var transactionId = "01K1C16DV2ZV6QZT11GNB4S3EA";
        var payment = new Payment
        {
            SessionId = 1,
            Amount = 100.00m,
            StatusId = (int)PaymentStatus.Pending,
            TpayTransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        
        var session = new Session
        {
            Id = 1,
            PaymentId = payment.Id,
            StatusId = (int)SessionStatus.Scheduled,
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Price = 100.00m
        };
        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _paymentService.ProcessNotificationAsync(transactionId, "paid");

        // Assert
        Assert.True(result);
        var updatedSession = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == 1);
        Assert.NotNull(updatedSession);
        Assert.Equal((int)SessionStatus.Confirmed, updatedSession.StatusId);
    }

    [Fact]
    public async Task ProcessNotificationAsync_ShouldReturnFalse_WhenPaymentNotFound()
    {
        // Act
        var result = await _paymentService.ProcessNotificationAsync("non-existent-id", "paid");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessNotificationAsync_ShouldKeepPendingStatus_WhenStatusIsPending()
    {
        // Arrange
        var transactionId = "01K1C16DV2ZV6QZT11GNB4S3EA";
        var payment = new Payment
        {
            SessionId = 1,
            Amount = 100.00m,
            StatusId = (int)PaymentStatus.Pending,
            TpayTransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _paymentService.ProcessNotificationAsync(transactionId, "pending");

        // Assert
        Assert.True(result);
        var updatedPayment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.TpayTransactionId == transactionId);
        Assert.NotNull(updatedPayment);
        Assert.Equal((int)PaymentStatus.Pending, updatedPayment.StatusId);
    }
}

