using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace AITS.Api.Services.Realtime;

public sealed class RealtimeTranscriptionSession : IAsyncDisposable
{
    private const int TargetSampleRate = 16000;
    private static readonly TimeSpan MinUpdateInterval = TimeSpan.FromSeconds(4);

    private readonly int _sessionId;
    private readonly string _connectionId;
    private readonly string _userId;
    private readonly IClientProxy _clientProxy;
    private readonly IAzureSpeechService _speechService;
    private readonly ILogger<RealtimeTranscriptionSession> _logger;
    private readonly string _rawFilePath;
    private readonly FileStream _rawStream;
    private readonly SemaphoreSlim _transcriptionLock = new(1, 1);

    private SpeechTranscriptionResult? _latestResult;
    private DateTime _lastUpdate = DateTime.MinValue;
    private bool _disposed;

    public RealtimeTranscriptionSession(
        int sessionId,
        string connectionId,
        string userId,
        IClientProxy clientProxy,
        IAzureSpeechService speechService,
        ILogger<RealtimeTranscriptionSession> logger)
    {
        _sessionId = sessionId;
        _connectionId = connectionId;
        _userId = userId;
        _clientProxy = clientProxy;
        _speechService = speechService;
        _logger = logger;

        _rawFilePath = Path.Combine(Path.GetTempPath(), $"aits-realtime-{Guid.NewGuid():N}.pcm");
        _rawStream = new FileStream(_rawFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    }

    public int SessionId => _sessionId;
    public string UserId => _userId;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _clientProxy.SendAsync("RealtimeStatus", new { status = "started" }, cancellationToken);
    }

    public async Task AppendAudioAsync(byte[] pcmBytes, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Odrzucono próbę dopisania audio dla połączenia {ConnectionId} z powodu anulowania.", _connectionId);
            return;
        }

        try
        {
            await _rawStream.WriteAsync(pcmBytes, 0, pcmBytes.Length, cancellationToken);
            await _rawStream.FlushAsync(cancellationToken);
            
            _logger.LogTrace("Dopisano {ByteCount} bajtów audio dla połączenia {ConnectionId}", pcmBytes.Length, _connectionId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Anulowano zapis audio dla połączenia {ConnectionId}.", _connectionId);
            return;
        }

        if (DateTime.UtcNow - _lastUpdate >= MinUpdateInterval)
        {
            _logger.LogDebug("Minął minimalny interwał aktualizacji, uruchamiam transkrypcję dla połączenia {ConnectionId}", _connectionId);
            await TryUpdateTranscriptionAsync(force: false, cancellationToken);
        }
    }

    public async Task<SpeechTranscriptionResult> CompleteAsync(CancellationToken cancellationToken)
    {
        await TryUpdateTranscriptionAsync(force: true, cancellationToken);
        return _latestResult ?? new SpeechTranscriptionResult(string.Empty, Array.Empty<TranscriptionSegmentDto>());
    }

    private async Task TryUpdateTranscriptionAsync(bool force, CancellationToken cancellationToken)
    {
        bool acquired;
        if (force)
        {
            _logger.LogDebug("Wymuszono aktualizację transkrypcji dla połączenia {ConnectionId}", _connectionId);
            await _transcriptionLock.WaitAsync(cancellationToken);
            acquired = true;
        }
        else
        {
            acquired = await _transcriptionLock.WaitAsync(0, cancellationToken);
        }

        if (!acquired)
        {
            _logger.LogTrace("Nie można uzyskać blokady dla połączenia {ConnectionId}, pomijam aktualizację", _connectionId);
            return;
        }

        try
        {
            if (!force && DateTime.UtcNow - _lastUpdate < MinUpdateInterval)
            {
                _logger.LogTrace("Minimalny interwał nie upłynął dla połączenia {ConnectionId}, pomijam aktualizację", _connectionId);
                return;
            }

            var wavPath = Path.Combine(Path.GetTempPath(), $"aits-realtime-{Guid.NewGuid():N}.wav");
            try
            {
                await _rawStream.FlushAsync(cancellationToken);

                var fileInfo = new FileInfo(_rawFilePath);
                _logger.LogDebug("Rozmiar pliku PCM dla połączenia {ConnectionId}: {FileSize} bajtów", _connectionId, fileInfo.Length);
                
                if (fileInfo.Length == 0)
                {
                    _logger.LogWarning("Plik PCM jest pusty dla połączenia {ConnectionId}", _connectionId);
                    _latestResult = new SpeechTranscriptionResult(string.Empty, Array.Empty<TranscriptionSegmentDto>());
                    _lastUpdate = DateTime.UtcNow;
                    return;
                }

                _logger.LogDebug("Konwersja PCM do WAV dla połączenia {ConnectionId}: {WavPath}", _connectionId, wavPath);
                await using (var readStream = new FileStream(_rawFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var waveFormat = new WaveFormat(TargetSampleRate, 16, 1);
                    using var rawSource = new RawSourceWaveStream(readStream, waveFormat);
                    WaveFileWriter.CreateWaveFile(wavPath, rawSource);
                }

                var wavFileInfo = new FileInfo(wavPath);
                _logger.LogDebug("Utworzono plik WAV o rozmiarze {FileSize} bajtów, rozpoczynam transkrypcję dla połączenia {ConnectionId}", 
                    wavFileInfo.Length, _connectionId);

                _latestResult = await _speechService.TranscribeAudioAsync(wavPath, "audio/wav", cancellationToken);
                _lastUpdate = DateTime.UtcNow;

                _logger.LogInformation("Transkrypcja zakończona dla połączenia {ConnectionId}: {SegmentCount} segmentów, {TextLength} znaków",
                    _connectionId, _latestResult.Segments.Count, _latestResult.Transcript.Length);

                await _clientProxy.SendAsync("RealtimeUpdate", new
                {
                    transcript = _latestResult.Transcript,
                    segments = _latestResult.Segments.Select(seg => new
                    {
                        seg.SpeakerTag,
                        StartOffset = seg.StartOffset,
                        EndOffset = seg.EndOffset,
                        seg.Text
                    })
                }, cancellationToken);
            }
            finally
            {
                if (File.Exists(wavPath))
                {
                    try
                    {
                        File.Delete(wavPath);
                        _logger.LogTrace("Usunięto tymczasowy plik WAV: {WavPath}", wavPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Nie udało się usunąć tymczasowego pliku WAV: {WavPath}", wavPath);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Błąd aktualizacji transkrypcji w czasie rzeczywistym dla połączenia {ConnectionId}", _connectionId);
            await _clientProxy.SendAsync("RealtimeStatus", new { status = "error", message = ex.Message });
        }
        finally
        {
            if (acquired)
            {
                _transcriptionLock.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _rawStream.DisposeAsync();
        _transcriptionLock.Dispose();

        if (File.Exists(_rawFilePath))
        {
            try
            {
                File.Delete(_rawFilePath);
            }
            catch
            {
                // ignorujemy
            }
        }
    }
}

