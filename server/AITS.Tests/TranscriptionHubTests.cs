using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AITS.Api.Hubs;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AITS.Tests;

public class TranscriptionHubTests
{
    [Fact]
    public async Task OnDisconnectedAsync_ShouldPersistTranscription_WhenAudioWasCaptured()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var therapist = new ApplicationUser
        {
            Id = "therapist-rt",
            UserName = "therapist@example.com",
            Email = "therapist@example.com"
        };

        db.Users.Add(therapist);

        var patient = new Patient
        {
            Id = 42,
            FirstName = "Jan",
            LastName = "Kowalski",
            Email = "jan@example.com",
            CreatedByUserId = therapist.Id,
            CreatedBy = therapist
        };

        db.Patients.Add(patient);

        db.Sessions.Add(new Session
        {
            Id = 101,
            PatientId = patient.Id,
            Patient = patient,
            TerapeutaId = therapist.Id,
            Terapeuta = therapist,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 150m
        });

        await db.SaveChangesAsync();

        var transcriptionResult = new SpeechTranscriptionResult(
            "To jest wynik transkrypcji",
            new[]
            {
                new TranscriptionSegmentDto("Speaker1", TimeSpan.Zero, TimeSpan.FromSeconds(5), "To jest wynik transkrypcji")
            });

        var speechMock = new Mock<IAzureSpeechService>();
        speechMock
            .Setup(s => s.TranscribeAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transcriptionResult);

        var callerProxy = new Mock<ISingleClientProxy>();
        callerProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var groupProxy = new Mock<IClientProxy>();
        groupProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubCallerClients>();
        clientsMock.Setup(c => c.Caller).Returns(callerProxy.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxy.Object);

        var groupsMock = new Mock<IGroupManager>();
        groupsMock
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groupsMock
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var connectionId = "conn-realtime-1";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, therapist.Id),
            new Claim(ClaimTypes.Role, Roles.Terapeuta)
        }, "TestAuth"));

        var hubContextMock = new Mock<HubCallerContext>();
        hubContextMock.SetupGet(c => c.ConnectionId).Returns(connectionId);
        hubContextMock.SetupGet(c => c.User).Returns(principal);
        hubContextMock.SetupGet(c => c.ConnectionAborted).Returns(CancellationToken.None);

        var hub = new TranscriptionHub(db, speechMock.Object, NullLoggerFactory.Instance, NullLogger<TranscriptionHub>.Instance)
        {
            Clients = clientsMock.Object,
            Groups = groupsMock.Object,
            Context = hubContextMock.Object
        };

        await hub.StartRealtime(101);

        var chunk = Enumerable.Repeat((byte)0x01, 32000).ToArray();
        await hub.UploadChunk(101, Convert.ToBase64String(chunk));

        await hub.OnDisconnectedAsync(null);

        var transcription = await db.SessionTranscriptions.Include(t => t.Segments).SingleOrDefaultAsync();
        Assert.NotNull(transcription);
        Assert.Equal(SessionTranscriptionSource.RealtimeRecording, transcription!.Source);
        Assert.Equal("To jest wynik transkrypcji", transcription.TranscriptText);
        Assert.Equal(therapist.Id, transcription.CreatedByUserId);
        Assert.Single(transcription.Segments);
        Assert.Equal("Speaker1", transcription.Segments.First().SpeakerTag);

        speechMock.Verify(
            s => s.TranscribeAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}

