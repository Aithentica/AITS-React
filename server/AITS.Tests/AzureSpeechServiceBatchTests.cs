using System;
using System.Collections.Generic;
using AITS.Api.Services;
using AITS.Api.Services.Models;
using Xunit;

namespace AITS.Tests;

public class AzureSpeechServiceBatchTests
{
    [Fact]
    public void BuildBatchTranscript_GroupsContiguousSegmentsBySpeaker()
    {
        var segments = new List<TranscriptionSegmentDto>
        {
            new("Speaker1", TimeSpan.Zero, TimeSpan.FromSeconds(3), "Cześć"),
            new("Speaker1", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), "jak się masz"),
            new("Speaker2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8), "W porządku"),
            new("Speaker1", TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(11), "Świetnie to słyszeć"),
            new("Speaker2", TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(15), "Do zobaczenia jutro")
        };

        var transcript = AzureSpeechService.BuildBatchTranscript(segments, "fallback");

        const string expected = "Speaker1: Cześć jak się masz\nSpeaker2: W porządku\nSpeaker1: Świetnie to słyszeć\nSpeaker2: Do zobaczenia jutro";
        // Normalizuj końce linii do \n dla porównania niezależnego od platformy
        var normalizedTranscript = transcript.Replace("\r\n", "\n").Replace("\r", "\n");
        Assert.Equal(expected, normalizedTranscript);
    }

    [Fact]
    public void BuildBatchTranscript_ReturnsFallback_WhenNoSegments()
    {
        var transcript = AzureSpeechService.BuildBatchTranscript(Array.Empty<TranscriptionSegmentDto>(), "fallback tekst");

        Assert.Equal("fallback tekst", transcript);
    }
}

