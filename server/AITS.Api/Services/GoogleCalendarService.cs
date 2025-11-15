using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using AITS.Api.Services.Interfaces;
using AITS.Api.Services.Models;

namespace AITS.Api.Services;

public sealed class GoogleCalendarService
{
    private readonly string _calendarId;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(IConfiguration configuration, IGoogleOAuthService googleOAuthService, ILogger<GoogleCalendarService> logger)
    {
        _calendarId = configuration["GoogleCalendar:CalendarId"] ?? "primary";
        _googleOAuthService = googleOAuthService;
        _logger = logger;
    }

    public async Task<GoogleCalendarEventResult?> CreateEventAsync(Session session, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await CreateCalendarServiceAsync(session.TerapeutaId, cancellationToken);
            if (service is null)
            {
                return null;
            }
            var patientName = $"{session.Patient.FirstName} {session.Patient.LastName}";
            
            var calendarEvent = new Event
            {
                Summary = $"Sesja terapeutyczna - {patientName}",
                Description = $"Sesja z pacjentem {patientName}.{Environment.NewLine}{session.Notes}",
                Start = new EventDateTime
                {
                    DateTime = session.StartDateTime,
                    TimeZone = "Europe/Warsaw"
                },
                End = new EventDateTime
                {
                    DateTime = session.EndDateTime,
                    TimeZone = "Europe/Warsaw"
                },
                ConferenceData = new ConferenceData
                {
                    CreateRequest = new CreateConferenceRequest
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                    }
                },
                Attendees = string.IsNullOrWhiteSpace(session.Patient.Email)
                    ? null
                    : new List<EventAttendee>
                    {
                        new() { Email = session.Patient.Email, DisplayName = patientName }
                    }
            };

            var request = service.Events.Insert(calendarEvent, _calendarId);
            request.ConferenceDataVersion = 1;
            var createdEvent = await request.ExecuteAsync(cancellationToken);

            var meetLink = createdEvent.ConferenceData?.EntryPoints?.FirstOrDefault()?.Uri;
            return new GoogleCalendarEventResult(createdEvent.Id, meetLink);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetMeetLinkAsync(string terapeutaId, string eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await CreateCalendarServiceAsync(terapeutaId, cancellationToken);
            if (service is null)
            {
                return null;
            }

            var calendarEvent = await service.Events.Get(_calendarId, eventId).ExecuteAsync(cancellationToken);
            return calendarEvent.ConferenceData?.EntryPoints?.FirstOrDefault()?.Uri;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(session.GoogleCalendarEventId)) return false;
        
        try
        {
            var service = await CreateCalendarServiceAsync(session.TerapeutaId, cancellationToken);
            if (service is null)
            {
                return false;
            }

            var existingEvent = await service.Events.Get(_calendarId, session.GoogleCalendarEventId).ExecuteAsync(cancellationToken);
            var patientName = $"{session.Patient.FirstName} {session.Patient.LastName}";
            
            existingEvent.Summary = $"Sesja terapeutyczna - {patientName}";
            existingEvent.Description = $"Sesja z pacjentem {patientName}.{Environment.NewLine}{session.Notes}";
            existingEvent.Start = new EventDateTime
            {
                DateTime = session.StartDateTime,
                TimeZone = "Europe/Warsaw"
            };
            existingEvent.End = new EventDateTime
            {
                DateTime = session.EndDateTime,
                TimeZone = "Europe/Warsaw"
            };
            
            await service.Events.Update(existingEvent, _calendarId, session.GoogleCalendarEventId).ExecuteAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(string terapeutaId, string? eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId)) return false;
        
        try
        {
            var service = await CreateCalendarServiceAsync(terapeutaId, cancellationToken);
            if (service is null)
            {
                return false;
            }

            await service.Events.Delete(_calendarId, eventId).ExecuteAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<CalendarService?> CreateCalendarServiceAsync(string terapeutaId, CancellationToken cancellationToken)
    {
        var tokenResult = await _googleOAuthService.EnsureValidAccessTokenAsync(terapeutaId, cancellationToken);
        if (!tokenResult.Success || tokenResult.Token is null)
        {
            _logger.LogWarning("Nie udało się pobrać tokenu Google dla terapeuty {TerapeutaId}: {Error}", terapeutaId, tokenResult.Error);
            return null;
        }

        var credential = GoogleCredential.FromAccessToken(tokenResult.Token.AccessToken)
            .CreateScoped(CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AI Therapy Support"
        });
    }
}




