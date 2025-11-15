using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AITS.Api.Configuration;
using AITS.Api.Services;
using AITS.Api.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AITS.Tests;

public class GoogleOAuthServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task EnsureValidAccessTokenAsync_ShouldFail_WhenTokenMissing()
    {
        await using var context = CreateContext();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(HttpStatusCode.BadRequest, "{}")));

        var service = new GoogleOAuthService(
            context,
            factory.Object,
            Options.Create(new GoogleOAuthOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                DefaultRedirectUri = "https://localhost/callback"
            }),
            NullLogger<GoogleOAuthService>.Instance);

        var result = await service.EnsureValidAccessTokenAsync("therapist-1");

        Assert.False(result.Success);
        Assert.Contains("Google", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldPersistToken()
    {
        await using var context = CreateContext();

        var payload = new
        {
            access_token = "access-token",
            refresh_token = "refresh-token",
            expires_in = 3600,
            token_type = "Bearer",
            scope = "scope"
        };

        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(payload));
        var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("google-oauth")).Returns(httpClient);

        var service = new GoogleOAuthService(
            context,
            factory.Object,
            Options.Create(new GoogleOAuthOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                DefaultRedirectUri = "https://localhost/callback"
            }),
            NullLogger<GoogleOAuthService>.Instance);

        var result = await service.ExchangeCodeAsync("therapist-1", "code", "https://localhost/callback");

        Assert.True(result.Success);
        var token = await context.TherapistGoogleTokens.FindAsync("therapist-1");
        Assert.NotNull(token);
        Assert.Equal("access-token", token!.AccessToken);
        Assert.Equal("refresh-token", token.RefreshToken);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}

