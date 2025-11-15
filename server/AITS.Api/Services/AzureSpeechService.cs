using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using AITS.Api.Configuration;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;

namespace AITS.Api.Services;

public sealed class AzureSpeechService : IAzureSpeechService
{
    private const int TargetSampleRate = 16000;
    private readonly AzureSpeechOptions _options;
    private readonly ILogger<AzureSpeechService> _logger;

    public AzureSpeechService(IOptions<AzureSpeechOptions> options, ILogger<AzureSpeechService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<SpeechTranscriptionResult> TranscribeAudioAsync(string filePath, string contentType, CancellationToken cancellationToken = default)
        => TranscribeAudioInternalAsync(filePath, contentType, cancellationToken, "stream");

    public Task<SpeechTranscriptionResult> TranscribeAudioBatchAsync(string filePath, string contentType, CancellationToken cancellationToken = default)
        => TranscribeAudioInternalAsync(filePath, contentType, cancellationToken, "batch");

    public async Task<SpeechTranscriptionResult> TranscribeVideoAsync(string filePath, string contentType, CancellationToken cancellationToken = default)
    {
        ValidateInputFile(filePath);

        var temporaryFiles = new List<string>();
        try
        {
            var audioPath = await ExtractAudioTrackAsync(filePath, contentType, temporaryFiles, cancellationToken);
            var normalizedPath = await EnsureWaveFormatAsync(audioPath, temporaryFiles, cancellationToken);

            return await TranscribeWaveFileAsync(normalizedPath, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Błąd podczas transkrypcji wideo {FilePath}", filePath);
            throw;
        }
        finally
        {
            CleanupTemporaryFiles(temporaryFiles);
        }
    }

    private async Task<SpeechTranscriptionResult> TranscribeAudioInternalAsync(string filePath, string contentType, CancellationToken cancellationToken, string mode)
    {
        ValidateInputFile(filePath);

        _logger.LogDebug("Tryb transkrypcji audio: {Mode}", mode);

        var temporaryFiles = new List<string>();
        try
        {
            var wavPath = PrepareAudioForRecognition(filePath, contentType, temporaryFiles);
            var normalizedPath = await EnsureWaveFormatAsync(wavPath, temporaryFiles, cancellationToken);

            var transcriptionResult = await TranscribeWaveFileAsync(normalizedPath, cancellationToken);
            if (string.Equals(mode, "batch", StringComparison.OrdinalIgnoreCase))
            {
                var aggregatedTranscript = BuildBatchTranscript(transcriptionResult.Segments, transcriptionResult.Transcript);
                return new SpeechTranscriptionResult(aggregatedTranscript, transcriptionResult.Segments);
            }

            return transcriptionResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Błąd podczas transkrypcji audio ({Mode}) {FilePath}", mode, filePath);
            throw;
        }
        finally
        {
            CleanupTemporaryFiles(temporaryFiles);
        }
    }

    private static void ValidateInputFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Ścieżka do pliku nie może być pusta", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Nie znaleziono pliku źródłowego.", filePath);
        }
    }

    private SpeechConfig CreateSpeechConfig()
    {
        _logger.LogDebug("Tworzenie konfiguracji Azure Speech - Endpoint={Endpoint}, Region={Region}, Language={Language}, MaxSpeakerCount={MaxSpeakerCount}",
            _options.Endpoint ?? "null", _options.Region, _options.Language, _options.MaxSpeakerCount);

        SpeechConfig config;
        if (!string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new InvalidOperationException($"Niepoprawny adres endpointu Azure Speech: {_options.Endpoint}");
            }

            var normalizedEndpoint = NormalizeEndpoint(endpointUri);
            _logger.LogInformation("Korzystanie z niestandardowego endpointu Azure Speech: {Endpoint}", normalizedEndpoint);

            config = SpeechConfig.FromEndpoint(normalizedEndpoint, _options.SubscriptionKey);
        }
        else
        {
            _logger.LogInformation("Korzystanie z regionu Azure Speech: {Region}", _options.Region);
            config = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
        }

        var language = string.IsNullOrWhiteSpace(_options.Language) ? "pl-PL" : _options.Language;
        var maxSpeakers = Math.Clamp(_options.MaxSpeakerCount, 2, 10);
        
        config.SpeechRecognitionLanguage = language;
        config.SetProperty("ConversationTranscription_DiarizationEnabled", "true");
        config.SetProperty("ConversationTranscription_MaxSpeakerCount", maxSpeakers.ToString(CultureInfo.InvariantCulture));
        config.SetProperty(PropertyId.SpeechServiceResponse_OutputFormatOption, "detailed");
        config.SetProperty(PropertyId.SpeechServiceResponse_RequestWordLevelTimestamps, "true");
        
        _logger.LogDebug("Konfiguracja Azure Speech utworzona - Language={Language}, DiarizationEnabled=true, MaxSpeakers={MaxSpeakers}",
            language, maxSpeakers);
        
        return config;
    }

    private static Uri NormalizeEndpoint(Uri endpointUri)
    {
        // Jeśli endpoint jest już w formacie WebSocket, zwróć go bez zmian
        if (endpointUri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) || endpointUri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
        {
            return endpointUri;
        }

        // Konwersja z HTTPS/HTTP na WSS dla WebSocket
        if (endpointUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || endpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(endpointUri)
            {
                Scheme = "wss",
                Port = -1
            };

            var path = builder.Path.Trim('/');
            // Jeśli ścieżka jest pusta lub zawiera tylko domenę, dodaj standardową ścieżkę dla Conversation Transcription
            if (string.IsNullOrWhiteSpace(path) || path.Equals("speech", StringComparison.OrdinalIgnoreCase))
            {
                builder.Path = "speech/recognition/conversation/cognitiveservices/v1";
            }
            // Jeśli ścieżka nie zawiera pełnej ścieżki do Conversation Transcription, dodaj ją
            else if (!path.Contains("conversation", StringComparison.OrdinalIgnoreCase))
            {
                builder.Path = $"{path.TrimEnd('/')}/speech/recognition/conversation/cognitiveservices/v1";
            }

            return builder.Uri;
        }

        throw new InvalidOperationException($"Nieobsługiwany schemat endpointu Azure Speech: {endpointUri}");
    }

    private async Task<SpeechTranscriptionResult> TranscribeWaveFileAsync(string wavPath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Rozpoczynam transkrypcję pliku WAV: {WavPath}", wavPath);
        
        using var audioConfig = AudioConfig.FromWavFileInput(wavPath);
        var speechConfig = CreateSpeechConfig();
        using var transcriber = new ConversationTranscriber(speechConfig, audioConfig);

        var transcriptBuilder = new StringBuilder();
        var segments = new ConcurrentBag<TranscriptionSegmentDto>();
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorSource = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnTranscribed(object? sender, ConversationTranscriptionEventArgs e)
        {
            _logger.LogDebug("Otrzymano wynik transkrypcji: Reason={Reason}, Text={Text}, SpeakerId={SpeakerId}", 
                e.Result.Reason, e.Result.Text, e.Result.SpeakerId);

            if (e.Result.Reason != ResultReason.RecognizedSpeech || string.IsNullOrWhiteSpace(e.Result.Text))
            {
                return;
            }

            transcriptBuilder.AppendLine(e.Result.Text.Trim());

            var offsetTicks = e.Result.OffsetInTicks;
            var start = TimeSpan.FromTicks(offsetTicks);
            var end = start + e.Result.Duration;
            var speaker = string.IsNullOrWhiteSpace(e.Result.SpeakerId) ? "Speaker" : e.Result.SpeakerId.Trim();

            segments.Add(new TranscriptionSegmentDto(
                speaker,
                start,
                end,
                e.Result.Text.Trim()));
        }

        void OnCanceled(object? sender, ConversationTranscriptionCanceledEventArgs e)
        {
            _logger.LogWarning("Otrzymano zdarzenie Canceled z Azure Speech - Reason={Reason}, ErrorCode={ErrorCode}, ErrorDetails={ErrorDetails}",
                e.Reason, e.ErrorCode, e.ErrorDetails);

            if (e.ErrorCode == CancellationErrorCode.NoError || e.Reason == CancellationReason.EndOfStream)
            {
                _logger.LogInformation("Azure Speech zakończyło sesję bez błędu (Reason={Reason}).", e.Reason);
                completionSource.TrySetResult(true);
                return;
            }

            var errorMessage = $"Azure Speech anulowało transkrypcję (kod {e.ErrorCode}): {e.ErrorDetails}";
            if (e.Reason == CancellationReason.Error && e.ErrorCode == CancellationErrorCode.ConnectionFailure)
            {
                errorMessage += " | Sprawdź klucz subskrypcji, region i połączenie sieciowe.";
            }
            else if (e.Reason == CancellationReason.Error && e.ErrorCode == CancellationErrorCode.ServiceError)
            {
                errorMessage += " | Błąd usługi Azure Speech. Sprawdź czy używasz dedykowanego Speech Services resource (nie multi-service Cognitive Services).";
            }

            var ex = new InvalidOperationException(errorMessage);
            errorSource.TrySetException(ex);
        }

        void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            _logger.LogDebug("Sesja Azure Speech rozpoczęta: SessionId={SessionId}", e.SessionId);
        }

        void OnSessionStopped(object? sender, SessionEventArgs e)
        {
            _logger.LogDebug("Sesja Azure Speech zatrzymana: SessionId={SessionId}", e.SessionId);
            completionSource.TrySetResult(true);
        }

        transcriber.Transcribed += OnTranscribed;
        transcriber.Canceled += OnCanceled;
        transcriber.SessionStarted += OnSessionStarted;
        transcriber.SessionStopped += OnSessionStopped;

        try
        {
            _logger.LogDebug("Rozpoczynam StartTranscribingAsync...");
            
            using (cancellationToken.Register(() => 
            {
                _logger.LogWarning("CancellationToken został anulowany");
                errorSource.TrySetCanceled(cancellationToken);
            }))
            {
                await transcriber.StartTranscribingAsync().WaitAsync(cancellationToken);
                _logger.LogDebug("StartTranscribingAsync zakończone pomyślnie, oczekuję na wyniki...");

                var finishedTask = await Task.WhenAny(completionSource.Task, errorSource.Task);

                if (finishedTask == errorSource.Task)
                {
                    _logger.LogError("Transkrypcja zakończona błędem");
                    await errorSource.Task; // throw captured error
                }

                _logger.LogDebug("Zatrzymuję transkrypcję...");
                await transcriber.StopTranscribingAsync().WaitAsync(cancellationToken);
                _logger.LogDebug("Transkrypcja zatrzymana pomyślnie");
            }

            var result = new SpeechTranscriptionResult(
                transcriptBuilder.ToString().Trim(),
                segments
                    .OrderBy(s => s.StartOffset)
                    .ToList());

            _logger.LogInformation("Transkrypcja zakończona: {SegmentCount} segmentów, {TextLength} znaków", 
                result.Segments.Count, result.Transcript.Length);

            return result;
        }
        finally
        {
            transcriber.Transcribed -= OnTranscribed;
            transcriber.Canceled -= OnCanceled;
            transcriber.SessionStarted -= OnSessionStarted;
            transcriber.SessionStopped -= OnSessionStopped;
        }
    }

    internal static string BuildBatchTranscript(IReadOnlyList<TranscriptionSegmentDto> segments, string? fallbackTranscript)
    {
        if (segments.Count == 0)
        {
            return string.IsNullOrWhiteSpace(fallbackTranscript) ? string.Empty : fallbackTranscript.Trim();
        }

        var orderedSegments = segments
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .OrderBy(s => s.StartOffset)
            .ToList();

        if (orderedSegments.Count == 0)
        {
            return string.IsNullOrWhiteSpace(fallbackTranscript) ? string.Empty : fallbackTranscript.Trim();
        }

        var builder = new StringBuilder();
        string? currentSpeaker = null;
        var buffer = new List<string>();

        void FlushBuffer()
        {
            if (buffer.Count == 0 || string.IsNullOrWhiteSpace(currentSpeaker))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(currentSpeaker);
            builder.Append(": ");
            builder.Append(string.Join(" ", buffer));
            buffer.Clear();
        }

        foreach (var segment in orderedSegments)
        {
            var speaker = string.IsNullOrWhiteSpace(segment.SpeakerTag)
                ? "Speaker"
                : segment.SpeakerTag.Trim();

            var text = segment.Text.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            if (!string.Equals(currentSpeaker, speaker, StringComparison.OrdinalIgnoreCase))
            {
                FlushBuffer();
                currentSpeaker = speaker;
            }

            buffer.Add(text);
        }

        FlushBuffer();

        return builder.Length == 0
            ? (string.IsNullOrWhiteSpace(fallbackTranscript) ? string.Empty : fallbackTranscript.Trim())
            : builder.ToString().Trim();
    }

    private string PrepareAudioForRecognition(string filePath, string contentType, ICollection<string> temporaryFiles)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (IsWav(contentType, extension))
        {
            return filePath;
        }

        if (IsMp3(contentType, extension))
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
            using var reader = new Mp3FileReader(filePath);
            using var pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);
            WaveFileWriter.CreateWaveFile(tempFile, pcmStream);
            temporaryFiles.Add(tempFile);
            return tempFile;
        }

        throw new NotSupportedException($"Nieobsługiwany format audio: {contentType ?? extension}");
    }

    private async Task<string> EnsureWaveFormatAsync(string filePath, ICollection<string> temporaryFiles, CancellationToken cancellationToken)
    {
        await using var waveStream = new WaveFileReader(filePath);
        if (waveStream.WaveFormat.SampleRate == TargetSampleRate && waveStream.WaveFormat.Channels == 1 && waveStream.WaveFormat.BitsPerSample == 16)
        {
            return filePath;
        }

        var targetPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        temporaryFiles.Add(targetPath);

        var targetFormat = new WaveFormat(TargetSampleRate, 16, 1);
        using var conversionStream = new MediaFoundationResampler(waveStream, targetFormat)
        {
            ResamplerQuality = 60
        };

        await using var output = new WaveFileWriter(targetPath, conversionStream.WaveFormat);
        var buffer = new byte[TargetSampleRate * 2];
        int bytesRead;
        while ((bytesRead = conversionStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return targetPath;
    }

    private async Task<string> ExtractAudioTrackAsync(string filePath, string contentType, ICollection<string> temporaryFiles, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (IsWav(contentType, extension) || IsMp3(contentType, extension))
        {
            return PrepareAudioForRecognition(filePath, contentType, temporaryFiles);
        }

        var ffmpegExecutable = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (string.IsNullOrWhiteSpace(ffmpegExecutable))
        {
            ffmpegExecutable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        temporaryFiles.Add(tempFile);

        var arguments = $"-y -i \"{filePath}\" -vn -acodec pcm_s16le -ar {TargetSampleRate} -ac 1 \"{tempFile}\"";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegExecutable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Nie udało się uruchomić procesu ffmpeg. Upewnij się, że ffmpeg jest zainstalowany i dostępny w PATH lub ustaw zmienną środowiskową FFMPEG_PATH.");
            }

            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var stderr = await stderrTask;
                throw new InvalidOperationException($"FFmpeg zakończył się kodem {process.ExitCode}. Szczegóły: {stderr}");
            }

            _ = await stdoutTask; // ensure completion
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException("Nie znaleziono programu ffmpeg. Zainstaluj ffmpeg i dodaj go do PATH lub ustaw zmienną FFMPEG_PATH.", ex);
        }

        return tempFile;
    }

    private static bool IsWav(string? contentType, string extension) =>
        string.Equals(contentType, "audio/wav", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contentType, "audio/x-wav", StringComparison.OrdinalIgnoreCase) ||
        extension == ".wav";

    private static bool IsMp3(string? contentType, string extension) =>
        string.Equals(contentType, "audio/mpeg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contentType, "audio/mp3", StringComparison.OrdinalIgnoreCase) ||
        extension == ".mp3";

    private static void CleanupTemporaryFiles(IEnumerable<string> temporaryFiles)
    {
        foreach (var temp in temporaryFiles)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(temp) && File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
            catch
            {
                // Ignorujemy błędy sprzątania
            }
        }
    }
}

