using System.Text;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using AITS.Api.Services.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace AITS.Tests;

public class RealtimeTranscriptionSessionTests
{
    private static byte[] CreateSilenceChunk(int milliseconds)
    {
        var samples = 16 * milliseconds; // 16 samples per ms at 16kHz
        return new byte[samples * 2];
    }

    [Fact]
    public async Task CompleteAsync_ShouldInvokeAzureSpeechAndReturnResult()
    {
        var speechMock = new Mock<IAzureSpeechService>();
        var clientProxyMock = new Mock<IClientProxy>();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
        var logger = loggerFactory.CreateLogger<RealtimeTranscriptionSession>();

        var segments = new[]
        {
            new TranscriptionSegmentDto("Speaker1", TimeSpan.Zero, TimeSpan.FromSeconds(1), "Witaj")
        };

        speechMock.Setup(s => s.TranscribeAudioAsync(It.IsAny<string>(), "audio/wav", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechTranscriptionResult("Witaj", segments));

        var session = new RealtimeTranscriptionSession(
            sessionId: 42,
            connectionId: "conn-1",
            userId: "user-1",
            clientProxy: clientProxyMock.Object,
            speechService: speechMock.Object,
            logger: logger);

        await session.InitializeAsync(CancellationToken.None);

        var chunk = CreateSilenceChunk(1000);
        await session.AppendAudioAsync(chunk, CancellationToken.None);

        var result = await session.CompleteAsync(CancellationToken.None);

        Assert.Equal("Witaj", result.Transcript);
        Assert.Single(result.Segments);

        speechMock.Verify(s => s.TranscribeAudioAsync(It.IsAny<string>(), "audio/wav", It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        await session.DisposeAsync();
    }

    [Fact]
    public async Task AppendAudioAsync_ShouldIgnoreCancellation()
    {
        var speechMock = new Mock<IAzureSpeechService>(MockBehavior.Strict);
        var clientProxyMock = new Mock<IClientProxy>(MockBehavior.Strict);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
        var logger = loggerFactory.CreateLogger<RealtimeTranscriptionSession>();

        var session = new RealtimeTranscriptionSession(
            sessionId: 7,
            connectionId: "conn-cancel",
            userId: "user-cancel",
            clientProxy: clientProxyMock.Object,
            speechService: speechMock.Object,
            logger: logger);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await session.AppendAudioAsync(CreateSilenceChunk(250), cts.Token);

        speechMock.Verify(s => s.TranscribeAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        await session.DisposeAsync();
    }
}

