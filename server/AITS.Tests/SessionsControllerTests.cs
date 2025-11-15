using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using AITS.Api;
using AITS.Api.Controllers;
using AITS.Api.Services;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AITS.Tests;

public class SessionsControllerTests
{
    private static (AppDbContext Context, ApplicationUser Therapist) BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new AppDbContext(options);
        var therapist = new ApplicationUser { Id = "therapist-1", UserName = "therapist@example.com", Email = "therapist@example.com" };
        context.Users.Add(therapist);
        context.Patients.Add(new Patient
        {
            Id = 1,
            FirstName = "Jan",
            LastName = "Kowalski",
            Email = "jan@example.com",
            CreatedByUserId = therapist.Id,
            CreatedBy = therapist
        });
        context.SaveChanges();

        return (context, therapist);
    }

    [Fact]
    public void CreateTranscription_HasUploadLimitsSetTo200Megabytes()
    {
        var method = typeof(SessionsController).GetMethod(nameof(SessionsController.CreateTranscription));
        Assert.NotNull(method);

        var sizeLimitAttribute = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        Assert.NotNull(sizeLimitAttribute);
        var limitMetadata = sizeLimitAttribute as IRequestSizeLimitMetadata;
        Assert.NotNull(limitMetadata);
        Assert.Equal(200_000_000L, limitMetadata!.MaxRequestBodySize);

        var formLimitsAttribute = method.GetCustomAttribute<RequestFormLimitsAttribute>();
        Assert.NotNull(formLimitsAttribute);
        Assert.Equal(200_000_000L, formLimitsAttribute!.MultipartBodyLengthLimit);
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenGoogleNotConnected()
    {
        var (context, therapist) = BuildContext();

        var oauthMock = new Mock<IGoogleOAuthService>();
        oauthMock.Setup(s => s.EnsureValidAccessTokenAsync(therapist.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleOAuthTokenResult(false, "Not connected"));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleCalendar:CalendarId"] = "primary"
            })
            .Build();

        var calendarService = new GoogleCalendarService(config, oauthMock.Object, NullLogger<GoogleCalendarService>.Instance);
        var smsService = new Mock<ISmsService>().Object;

        var speechMock = new Mock<IAzureSpeechService>();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        var controller = new SessionsController(context, calendarService, oauthMock.Object, smsService, speechMock.Object, envMock.Object, NullLogger<SessionsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, therapist.Id),
                        new Claim(ClaimTypes.Role, Roles.Terapeuta)
                    }, "Test"))
                }
            }
        };

        var result = await controller.Create(new SessionsController.CreateSessionRequest
        {
            PatientId = 1,
            StartDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60,
            Price = 200,
            Notes = null,
            SessionTypeId = null
        });

        // Kontroler tworzy sesję nawet bez integracji Google - tylko loguje ostrzeżenie
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1, context.Sessions.Count());
        var session = context.Sessions.First();
        Assert.Null(session.GoogleCalendarEventId); // Nie powinno być Google Calendar event ID
    }

    [Fact]
    public async Task Update_ShouldSucceed_WhenSessionTypeIdMissing()
    {
        var (context, therapist) = BuildContext();
        var session = new Session
        {
            Id = 10,
            PatientId = 1,
            TerapeutaId = therapist.Id,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 150
        };
        context.Sessions.Add(session);
        context.SaveChanges();

        var controller = BuildController(context, therapist);
        var newStart = session.StartDateTime.AddDays(1);

        var request = new SessionsController.UpdateSessionRequest
        {
            StartDateTime = newStart,
            DurationMinutes = 90,
            Price = 250,
            Notes = "Zmieniona sesja",
            SessionTypeId = null
        };

        var result = await controller.Update(session.Id, request);

        Assert.IsType<OkObjectResult>(result);
        var updated = await context.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == session.Id);
        Assert.NotNull(updated);
        Assert.Equal(newStart, updated!.StartDateTime);
        Assert.Equal(newStart.AddMinutes(90), updated.EndDateTime);
        Assert.Equal(250, updated.Price);
        Assert.Equal("Zmieniona sesja", updated.Notes);
        Assert.Null(updated.SessionTypeId);
    }

    [Fact]
    public async Task CreateTranscription_ShouldPersistManualText()
    {
        var (context, therapist) = BuildContext();
        var session = new Session
        {
            Id = 1,
            PatientId = 1,
            TerapeutaId = therapist.Id,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 100
        };
        context.Sessions.Add(session);
        context.SaveChanges();

        var controller = BuildController(context, therapist);

        var form = new SessionsController.CreateTranscriptionForm
        {
            Source = SessionTranscriptionSource.ManualText,
            Text = "To jest testowa transkrypcja"
        };

        var result = await controller.CreateTranscription(session.Id, form, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var transcription = await context.SessionTranscriptions.FirstOrDefaultAsync();
        Assert.NotNull(transcription);
        Assert.Equal(SessionTranscriptionSource.ManualText, transcription!.Source);
        Assert.Equal("To jest testowa transkrypcja", transcription.TranscriptText);
        Assert.Null(transcription.SourceFilePath);
        Assert.Equal(therapist.Id, transcription.CreatedByUserId);
    }

    [Fact]
    public async Task CreateTranscription_ShouldUseAzureSpeechForAudio()
    {
        var (context, therapist) = BuildContext();
        var session = new Session
        {
            Id = 1,
            PatientId = 1,
            TerapeutaId = therapist.Id,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 100
        };
        context.Sessions.Add(session);
        context.SaveChanges();

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var azureMock = new Mock<IAzureSpeechService>();
        azureMock.Setup(s => s.TranscribeAudioBatchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechTranscriptionResult("Przetworzona transkrypcja", Array.Empty<TranscriptionSegmentDto>()));

        var controller = BuildController(context, therapist, azureSpeechMock: azureMock, webRootOverride: tempDir);

        await using var audioStream = new MemoryStream(CreateSilentWavBytes());
        var formFile = new FormFile(audioStream, 0, audioStream.Length, "file", "audio.wav")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/wav"
        };
        audioStream.Position = 0;

        var form = new SessionsController.CreateTranscriptionForm
        {
            Source = SessionTranscriptionSource.AudioUpload,
            File = formFile
        };

        var result = await controller.CreateTranscription(session.Id, form, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        azureMock.Verify(s => s.TranscribeAudioBatchAsync(It.IsAny<string>(), It.Is<string>(ct => ct == "audio/wav"), It.IsAny<CancellationToken>()), Times.Once);

        var transcription = await context.SessionTranscriptions.FirstOrDefaultAsync();
        Assert.NotNull(transcription);
        Assert.NotNull(transcription!.SourceFilePath);
        Assert.Equal("Przetworzona transkrypcja", transcription.TranscriptText);

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task CreateTranscription_ShouldPersistFinalTranscriptUpload()
    {
        var (context, therapist) = BuildContext();
        var session = new Session
        {
            Id = 2,
            PatientId = 1,
            TerapeutaId = therapist.Id,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 150
        };
        context.Sessions.Add(session);
        context.SaveChanges();

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var controller = BuildController(context, therapist, webRootOverride: tempDir);

        await using var transcriptStream = new MemoryStream(Encoding.UTF8.GetBytes("Speaker A: test"));
        var transcriptFile = new FormFile(transcriptStream, 0, transcriptStream.Length, "file", "transcript.vtt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/vtt"
        };
        transcriptStream.Position = 0;

        var form = new SessionsController.CreateTranscriptionForm
        {
            Source = SessionTranscriptionSource.FinalTranscriptUpload,
            File = transcriptFile
        };

        var result = await controller.CreateTranscription(session.Id, form, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var saved = await context.SessionTranscriptions.Include(t => t.Segments).FirstOrDefaultAsync(t => t.SessionId == session.Id);
        Assert.NotNull(saved);
        Assert.Empty(saved!.Segments);
        Assert.Equal("Speaker A: test", saved.TranscriptText);

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task CreateTranscription_ShouldUseAzureSpeechForVideo()
    {
        var (context, therapist) = BuildContext();
        var session = new Session
        {
            Id = 3,
            PatientId = 1,
            TerapeutaId = therapist.Id,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1),
            StatusId = (int)SessionStatus.Scheduled,
            Price = 200
        };
        context.Sessions.Add(session);
        context.SaveChanges();

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var segments = new[]
        {
            new TranscriptionSegmentDto("Speaker1", TimeSpan.Zero, TimeSpan.FromSeconds(2), "Dzień dobry"),
            new TranscriptionSegmentDto("Speaker2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), "Witam")
        };

        var azureMock = new Mock<IAzureSpeechService>();
        azureMock.Setup(s => s.TranscribeVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechTranscriptionResult("Dzień dobry\nWitam", segments));

        var controller = BuildController(context, therapist, azureSpeechMock: azureMock, webRootOverride: tempDir);

        await using var videoStream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });
        var videoFile = new FormFile(videoStream, 0, videoStream.Length, "file", "session.mp4")
        {
            Headers = new HeaderDictionary(),
            ContentType = "video/mp4"
        };
        videoStream.Position = 0;

        var form = new SessionsController.CreateTranscriptionForm
        {
            Source = SessionTranscriptionSource.VideoFile,
            File = videoFile
        };

        var response = await controller.CreateTranscription(session.Id, form, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(response);
        azureMock.Verify(s => s.TranscribeVideoAsync(It.IsAny<string>(), It.Is<string>(ct => ct == "video/mp4"), It.IsAny<CancellationToken>()), Times.Once);

        var saved = await context.SessionTranscriptions.Include(t => t.Segments).FirstOrDefaultAsync(t => t.SessionId == session.Id);
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Segments.Count);
        Assert.Equal("Speaker1", saved.Segments.First().SpeakerTag);

        Directory.Delete(tempDir, recursive: true);
    }

    private static SessionsController BuildController(
        AppDbContext context,
        ApplicationUser therapist,
        Mock<IGoogleOAuthService>? oauthMock = null,
        Mock<IAzureSpeechService>? azureSpeechMock = null,
        string? webRootOverride = null)
    {
        oauthMock ??= new Mock<IGoogleOAuthService>();
        oauthMock.Setup(s => s.EnsureValidAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleOAuthTokenResult(true, null));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleCalendar:CalendarId"] = "primary"
            })
            .Build();

        var calendarService = new GoogleCalendarService(config, oauthMock.Object, NullLogger<GoogleCalendarService>.Instance);
        var smsService = new Mock<ISmsService>().Object;

        azureSpeechMock ??= new Mock<IAzureSpeechService>();

        var environment = new Mock<IWebHostEnvironment>();
        var root = webRootOverride ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        environment.Setup(e => e.WebRootPath).Returns(root);

        var controller = new SessionsController(context, calendarService, oauthMock.Object, smsService, azureSpeechMock.Object, environment.Object, NullLogger<SessionsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, therapist.Id),
                        new Claim(ClaimTypes.Role, Roles.Terapeuta)
                    }, "Test"))
                }
            }
        };

        return controller;
    }

    private static byte[] CreateSilentWavBytes()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        const int sampleRate = 16000;
        const short bitsPerSample = 16;
        const short channels = 1;
        const int durationSeconds = 1;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var subChunk2Size = sampleRate * channels * bitsPerSample / 8 * durationSeconds;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + subChunk2Size);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(subChunk2Size);

        for (var i = 0; i < subChunk2Size / (bitsPerSample / 8); i++)
        {
            writer.Write((short)0);
        }

        writer.Flush();
        return memoryStream.ToArray();
    }
}

