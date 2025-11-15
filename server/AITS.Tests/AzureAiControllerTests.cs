using AITS.Api.Controllers;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AITS.Tests;

public sealed class AzureAiControllerTests
{
    [Fact]
    public async Task SendPrompt_ShouldReturnOk_WithResponse()
    {
        var serviceMock = new Mock<IAzureAIService>();
        serviceMock.Setup(s => s.GetCompletionAsync(It.IsAny<AzureAICompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AzureAICompletionResult("Odpowiedź", "stop", new AzureAIUsage(10, 20, 30)));

        var controller = new AzureAiController(serviceMock.Object, NullLogger<AzureAiController>.Instance);

        var result = await controller.SendPrompt(new AzureAiController.PromptRequest("Witaj", null, null, null), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AzureAiController.PromptResponse>(ok.Value);
        Assert.Equal("Odpowiedź", payload.Content);
        Assert.Equal("stop", payload.FinishReason);
        Assert.Equal(30, payload.Usage.TotalTokens);
    }

    [Fact]
    public async Task SendPrompt_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        var serviceMock = new Mock<IAzureAIService>();
        var controller = new AzureAiController(serviceMock.Object, NullLogger<AzureAiController>.Instance);

        var result = await controller.SendPrompt(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Brak treści zapytania.", badRequest.Value);
    }

    [Fact]
    public async Task SendPrompt_ShouldForwardParameters()
    {
        var capturedRequest = default(AzureAICompletionRequest);

        var serviceMock = new Mock<IAzureAIService>();
        serviceMock.Setup(s => s.GetCompletionAsync(It.IsAny<AzureAICompletionRequest>(), It.IsAny<CancellationToken>()))
            .Callback((AzureAICompletionRequest req, CancellationToken _) => capturedRequest = req)
            .ReturnsAsync(new AzureAICompletionResult("Odpowiedź", "stop", new AzureAIUsage(null, null, null)));

        var controller = new AzureAiController(serviceMock.Object, NullLogger<AzureAiController>.Instance);

        var request = new AzureAiController.PromptRequest("Test", "System", 123, 0.3);
        await controller.SendPrompt(request, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal("Test", capturedRequest!.Prompt);
        Assert.Equal("System", capturedRequest.SystemPrompt);
        Assert.Equal(123, capturedRequest.MaxTokens);
        Assert.Equal(0.3, capturedRequest.Temperature);
    }
}


