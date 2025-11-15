using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using AITS.Api.Services.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AITS.Api.Hubs;

[Authorize(Roles = Roles.Terapeuta + "," + Roles.TerapeutaFreeAccess + "," + Roles.Administrator)]
public sealed class TranscriptionHub : Hub
{
    private static readonly ConcurrentDictionary<string, RealtimeTranscriptionSession> ActiveSessions = new();

    private readonly AppDbContext _db;
    private readonly IAzureSpeechService _speechService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TranscriptionHub> _logger;

    public TranscriptionHub(
        AppDbContext db,
        IAzureSpeechService speechService,
        ILoggerFactory loggerFactory,
        ILogger<TranscriptionHub> logger)
    {
        _db = db;
        _speechService = speechService;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ActiveSessions.TryRemove(Context.ConnectionId, out var session))
        {
            try
            {
                var result = await session.CompleteAsync(CancellationToken.None);
                var isAdmin = Context.User?.IsInRole(Roles.Administrator) == true;
                await PersistTranscriptionAsync(session.SessionId, session.UserId, isAdmin, result, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się zapisać transkrypcji przy rozłączeniu połączenia {ConnectionId}", Context.ConnectionId);
            }
            finally
            {
                await session.DisposeAsync();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartRealtime(int sessionId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("Nie rozpoznano użytkownika.");
        }

        var isAdmin = Context.User?.IsInRole(Roles.Administrator) == true;

        var hasAccess = await _db.Sessions
            .AnyAsync(s => s.Id == sessionId && (s.TerapeutaId == userId || isAdmin), Context.ConnectionAborted);

        if (!hasAccess)
        {
            throw new HubException("Brak uprawnień do wskazanej sesji.");
        }

        if (ActiveSessions.TryRemove(Context.ConnectionId, out var existing))
        {
            await existing.DisposeAsync();
        }

        var session = new RealtimeTranscriptionSession(
            sessionId,
            Context.ConnectionId,
            userId,
            Clients.Caller,
            _speechService,
            _loggerFactory.CreateLogger<RealtimeTranscriptionSession>());

        ActiveSessions[Context.ConnectionId] = session;

        await session.InitializeAsync(Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(sessionId));
    }

    public async Task UploadChunk(int sessionId, string base64Chunk)
    {
        if (!ActiveSessions.TryGetValue(Context.ConnectionId, out var session) || session.SessionId != sessionId)
        {
            throw new HubException("Sesja w czasie rzeczywistym nie została zainicjowana.");
        }

        var bytes = Convert.FromBase64String(base64Chunk);
        await session.AppendAudioAsync(bytes, Context.ConnectionAborted);
    }

    public async Task StopRealtime(int sessionId)
    {
        if (!ActiveSessions.TryRemove(Context.ConnectionId, out var session) || session.SessionId != sessionId)
        {
            throw new HubException("Sesja w czasie rzeczywistym nie została zainicjowana.");
        }

        var result = await session.CompleteAsync(Context.ConnectionAborted);
        await session.DisposeAsync();

        var isAdmin = Context.User?.IsInRole(Roles.Administrator) == true;
        await PersistTranscriptionAsync(sessionId, session.UserId, isAdmin, result, Context.ConnectionAborted);

        await Clients.Caller.SendAsync("RealtimeStatus", new { status = "stopped" }, Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildGroupName(sessionId));
    }

    private async Task PersistTranscriptionAsync(int sessionId, string userId, bool callerIsAdmin, SpeechTranscriptionResult result, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .Include(s => s.Transcriptions)
            .ThenInclude(t => t.Segments)
            .FirstOrDefaultAsync(s => s.Id == sessionId && (s.TerapeutaId == userId || callerIsAdmin), cancellationToken);

        if (session is null)
        {
            throw new HubException("Sesja nie została znaleziona lub brak uprawnień.");
        }

        var existing = await _db.SessionTranscriptions
            .Where(t => t.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _db.SessionTranscriptions.RemoveRange(existing);
        }

        if (string.IsNullOrWhiteSpace(result.Transcript))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var transcription = new SessionTranscription
        {
            SessionId = sessionId,
            Source = SessionTranscriptionSource.RealtimeRecording,
            TranscriptText = result.Transcript.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            Segments = result.Segments
                .OrderBy(s => s.StartOffset)
                .Select(s => new SessionTranscriptionSegment
                {
                    SpeakerTag = s.SpeakerTag,
                    StartOffset = s.StartOffset,
                    EndOffset = s.EndOffset,
                    Content = s.Text
                })
                .ToList()
        };

        _db.SessionTranscriptions.Add(transcription);
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await Clients.Group(BuildGroupName(sessionId)).SendAsync("RealtimePersisted", new
        {
            transcriptionId = transcription.Id,
            transcript = transcription.TranscriptText
        }, cancellationToken);
    }

    private static string BuildGroupName(int sessionId) => $"session-{sessionId}";
}

