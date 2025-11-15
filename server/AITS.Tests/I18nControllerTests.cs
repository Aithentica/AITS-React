using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITS.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AITS.Tests;

public sealed class I18nControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnEnglishTranslations_ForSessionTranscriptions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"translations-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var controller = new I18nController(dbContext);

        var result = await controller.Get("en");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var translations = Assert.IsAssignableFrom<IDictionary<string, string>>(okResult.Value);

        Assert.Equal("Session transcriptions", translations["sessions.transcriptions.title"]);
        Assert.Equal("Real-time recording with diarization", translations["sessions.transcriptions.realtime.title"]);
        Assert.Equal("Select audio file", translations["sessions.transcriptions.audioUploadButton"]);
        Assert.Equal("No sessions today", translations["dashboard.noSessionsToday"]);
        Assert.Equal("Integrations", translations["integrations.googleCalendar.nav"]);
        Assert.Equal("Close", translations["common.close"]);
        Assert.Equal("Refresh", translations["common.refresh"]);
    }

    [Fact]
    public async Task Get_ShouldReturnPolishTranslations_ForSessionTranscriptions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"translations-pl-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var controller = new I18nController(dbContext);

        var result = await controller.Get("pl");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var translations = Assert.IsAssignableFrom<IDictionary<string, string>>(okResult.Value);

        Assert.Equal("Transkrypcje sesji", translations["sessions.transcriptions.title"]);
        Assert.Equal("Nagrywanie z diarizacją w czasie rzeczywistym", translations["sessions.transcriptions.realtime.title"]);
        Assert.Equal("Wybierz plik audio", translations["sessions.transcriptions.audioUploadButton"]);
        Assert.Equal("Brak sesji na dzisiaj", translations["dashboard.noSessionsToday"]);
        Assert.Equal("Integracje", translations["integrations.googleCalendar.nav"]);
        Assert.Equal("Zamknij", translations["common.close"]);
        Assert.Equal("Odśwież", translations["common.refresh"]);
    }
}


