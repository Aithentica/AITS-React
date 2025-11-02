using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace AITS.Api.Services;

public sealed class GoogleCalendarService
{
    private readonly string _credentialsPath;
    private readonly string _calendarId;
    private CalendarService? _calendarService;

    public GoogleCalendarService(IConfiguration configuration)
    {
        _credentialsPath = configuration["GoogleCalendar:CredentialsPath"] ?? throw new InvalidOperationException("GoogleCalendar:CredentialsPath not configured");
        _calendarId = configuration["GoogleCalendar:CalendarId"] ?? "primary";
    }

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        if (_calendarService != null) return _calendarService;

        var credential = await GoogleCredential.FromFileAsync(_credentialsPath, CancellationToken.None);
        if (credential.IsCreateScopedRequired)
            credential = credential.CreateScoped(CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents);

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AITS"
        });

        return _calendarService;
    }

    public async Task<string?> CreateEventAsync(Session session)
    {
        try
        {
            var service = await GetCalendarServiceAsync();
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
                Attendees = new List<EventAttendee>
                {
                    new() { Email = session.Patient.Email, DisplayName = patientName }
                }
            };

            var request = service.Events.Insert(calendarEvent, _calendarId);
            request.ConferenceDataVersion = 1;
            var createdEvent = await request.ExecuteAsync();
            
            return createdEvent.Id;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetMeetLinkAsync(string eventId)
    {
        try
        {
            var service = await GetCalendarServiceAsync();
            var calendarEvent = await service.Events.Get(_calendarId, eventId).ExecuteAsync();
            return calendarEvent.ConferenceData?.EntryPoints?.FirstOrDefault()?.Uri;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(Session session)
    {
        if (string.IsNullOrEmpty(session.GoogleCalendarEventId)) return false;
        
        try
        {
            var service = await GetCalendarServiceAsync();
            var existingEvent = await service.Events.Get(_calendarId, session.GoogleCalendarEventId).ExecuteAsync();
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
            
            await service.Events.Update(existingEvent, _calendarId, session.GoogleCalendarEventId).ExecuteAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(string? eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return false;
        
        try
        {
            var service = await GetCalendarServiceAsync();
            await service.Events.Delete(_calendarId, eventId).ExecuteAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}




