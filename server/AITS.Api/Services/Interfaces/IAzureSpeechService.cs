namespace AITS.Api.Services.Interfaces;

using AITS.Api.Services.Models;

public interface IAzureSpeechService
{
    Task<SpeechTranscriptionResult> TranscribeAudioAsync(string filePath, string contentType, CancellationToken cancellationToken = default);
    Task<SpeechTranscriptionResult> TranscribeAudioBatchAsync(string filePath, string contentType, CancellationToken cancellationToken = default);
    Task<SpeechTranscriptionResult> TranscribeVideoAsync(string filePath, string contentType, CancellationToken cancellationToken = default);
}

