using System;
using System.Collections.Generic;

namespace AITS.Api.Services.Models;

public sealed record TranscriptionSegmentDto(string SpeakerTag, TimeSpan StartOffset, TimeSpan EndOffset, string Text);

public sealed record SpeechTranscriptionResult(string Transcript, IReadOnlyList<TranscriptionSegmentDto> Segments);

