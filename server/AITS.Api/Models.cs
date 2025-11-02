using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Pacjent = "Pacjent";
    public const string Terapeuta = "Terapeuta";
    public const string TerapeutaFreeAccess = "TerapeutaFreeAccess";
}

public sealed class ApplicationUser : IdentityUser
{
}

public sealed class Translation
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Culture { get; set; } = "pl"; // np. pl, en
    public string Value { get; set; } = string.Empty;
}

public sealed class EnumType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // np. TherapyType
}

public sealed class EnumValue
{
    public int Id { get; set; }
    public int EnumTypeId { get; set; }
    public string Code { get; set; } = string.Empty; // np. CBT
    public EnumType EnumType { get; set; } = null!;
}

public sealed class EnumValueTranslation
{
    public int Id { get; set; }
    public int EnumValueId { get; set; }
    public string Culture { get; set; } = "pl";
    public string Name { get; set; } = string.Empty;
    public EnumValue EnumValue { get; set; } = null!;
}

public enum TherapyType
{
    CBT = 1,
    DBT = 2,
    ACT = 3
}

public enum SessionStatus
{
    Scheduled = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public sealed class Patient
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    
    // Dane demograficzne
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; } // M, F, Other
    public string? Pesel { get; set; }
    
    // Dane adresowe
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "Polska";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}

public sealed class Session
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string TerapeutaId { get; set; } = string.Empty;
    public ApplicationUser Terapeuta { get; set; } = null!;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int StatusId { get; set; }
    public decimal Price { get; set; }
    public string? GoogleCalendarEventId { get; set; }
    public string? GoogleMeetLink { get; set; }
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public decimal Amount { get; set; }
    public int StatusId { get; set; }
    public string? TpayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<EnumType> EnumTypes => Set<EnumType>();
    public DbSet<EnumValue> EnumValues => Set<EnumValue>();
    public DbSet<EnumValueTranslation> EnumValueTranslations => Set<EnumValueTranslation>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Translation>().HasIndex(x => new { x.Key, x.Culture }).IsUnique();

        modelBuilder.Entity<EnumType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<EnumValue>().HasIndex(x => new { x.EnumTypeId, x.Code }).IsUnique();
        modelBuilder.Entity<EnumValueTranslation>().HasIndex(x => new { x.EnumValueId, x.Culture }).IsUnique();

        // Seed enum: TherapyType with PL/EN
        modelBuilder.Entity<EnumType>().HasData(new EnumType { Id = 1, Name = nameof(TherapyType) });
        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue { Id = 1, EnumTypeId = 1, Code = nameof(TherapyType.CBT) },
            new EnumValue { Id = 2, EnumTypeId = 1, Code = nameof(TherapyType.DBT) },
            new EnumValue { Id = 3, EnumTypeId = 1, Code = nameof(TherapyType.ACT) }
        );
        modelBuilder.Entity<EnumValueTranslation>().HasData(
            new EnumValueTranslation { Id = 1, EnumValueId = 1, Culture = "pl", Name = "Terapia poznawczo-behawioralna" },
            new EnumValueTranslation { Id = 2, EnumValueId = 1, Culture = "en", Name = "Cognitive Behavioral Therapy" },
            new EnumValueTranslation { Id = 3, EnumValueId = 2, Culture = "pl", Name = "Dialektyczna terapia behawioralna" },
            new EnumValueTranslation { Id = 4, EnumValueId = 2, Culture = "en", Name = "Dialectical Behavior Therapy" },
            new EnumValueTranslation { Id = 5, EnumValueId = 3, Culture = "pl", Name = "Terapia akceptacji i zaangażowania" },
            new EnumValueTranslation { Id = 6, EnumValueId = 3, Culture = "en", Name = "Acceptance and Commitment Therapy" }
        );

        // Minimal translations for login form (PL baseline with EN)
        modelBuilder.Entity<Translation>().HasData(
            new Translation { Id = 1, Key = "login.title", Culture = "pl", Value = "Logowanie" },
            new Translation { Id = 2, Key = "login.title", Culture = "en", Value = "Sign in" },
            new Translation { Id = 3, Key = "login.email", Culture = "pl", Value = "E-mail" },
            new Translation { Id = 4, Key = "login.email", Culture = "en", Value = "Email" },
            new Translation { Id = 5, Key = "login.password", Culture = "pl", Value = "Hasło" },
            new Translation { Id = 6, Key = "login.password", Culture = "en", Value = "Password" },
            new Translation { Id = 7, Key = "login.submit", Culture = "pl", Value = "Zaloguj" },
            new Translation { Id = 8, Key = "login.submit", Culture = "en", Value = "Log in" },
            new Translation { Id = 9, Key = "login.language", Culture = "pl", Value = "Język" },
            new Translation { Id = 10, Key = "login.language", Culture = "en", Value = "Language" },
            
            // Dashboard translations (PL baseline with EN)
            new Translation { Id = 11, Key = "dashboard.title", Culture = "pl", Value = "Kokpit" },
            new Translation { Id = 12, Key = "dashboard.title", Culture = "en", Value = "Dashboard" },
            new Translation { Id = 13, Key = "dashboard.todaySessions", Culture = "pl", Value = "Dzisiejsze sesje" },
            new Translation { Id = 14, Key = "dashboard.todaySessions", Culture = "en", Value = "Today's sessions" },
            new Translation { Id = 15, Key = "dashboard.allSessions", Culture = "pl", Value = "Wszystkie sesje" },
            new Translation { Id = 16, Key = "dashboard.allSessions", Culture = "en", Value = "All sessions" },
            new Translation { Id = 17, Key = "dashboard.statistics", Culture = "pl", Value = "Statystyki" },
            new Translation { Id = 18, Key = "dashboard.statistics", Culture = "en", Value = "Statistics" },
            new Translation { Id = 19, Key = "dashboard.sessionsToday", Culture = "pl", Value = "Sesje dzisiaj" },
            new Translation { Id = 20, Key = "dashboard.sessionsToday", Culture = "en", Value = "Sessions today" },
            new Translation { Id = 21, Key = "dashboard.sessionsScheduled", Culture = "pl", Value = "Zaplanowane" },
            new Translation { Id = 22, Key = "dashboard.sessionsScheduled", Culture = "en", Value = "Scheduled" },
            new Translation { Id = 23, Key = "dashboard.sessionsCompleted", Culture = "pl", Value = "Zakończone w tym miesiącu" },
            new Translation { Id = 24, Key = "dashboard.sessionsCompleted", Culture = "en", Value = "Completed this month" },
            
            // Sessions translations
            new Translation { Id = 25, Key = "sessions.title", Culture = "pl", Value = "Sesje" },
            new Translation { Id = 26, Key = "sessions.title", Culture = "en", Value = "Sessions" },
            new Translation { Id = 27, Key = "sessions.new", Culture = "pl", Value = "Nowa sesja" },
            new Translation { Id = 28, Key = "sessions.new", Culture = "en", Value = "New session" },
            new Translation { Id = 29, Key = "sessions.edit", Culture = "pl", Value = "Edytuj sesję" },
            new Translation { Id = 30, Key = "sessions.edit", Culture = "en", Value = "Edit session" },
            new Translation { Id = 31, Key = "sessions.details", Culture = "pl", Value = "Szczegóły sesji" },
            new Translation { Id = 32, Key = "sessions.details", Culture = "en", Value = "Session details" },
            new Translation { Id = 33, Key = "sessions.patient", Culture = "pl", Value = "Pacjent" },
            new Translation { Id = 34, Key = "sessions.patient", Culture = "en", Value = "Patient" },
            new Translation { Id = 35, Key = "sessions.startTime", Culture = "pl", Value = "Data rozpoczęcia" },
            new Translation { Id = 36, Key = "sessions.startTime", Culture = "en", Value = "Start time" },
            new Translation { Id = 37, Key = "sessions.endTime", Culture = "pl", Value = "Data zakończenia" },
            new Translation { Id = 38, Key = "sessions.endTime", Culture = "en", Value = "End time" },
            new Translation { Id = 39, Key = "sessions.price", Culture = "pl", Value = "Cena" },
            new Translation { Id = 40, Key = "sessions.price", Culture = "en", Value = "Price" },
            new Translation { Id = 41, Key = "sessions.status", Culture = "pl", Value = "Status" },
            new Translation { Id = 42, Key = "sessions.status", Culture = "en", Value = "Status" },
            new Translation { Id = 43, Key = "sessions.confirm", Culture = "pl", Value = "Potwierdź" },
            new Translation { Id = 44, Key = "sessions.confirm", Culture = "en", Value = "Confirm" },
            new Translation { Id = 45, Key = "sessions.cancel", Culture = "pl", Value = "Anuluj" },
            new Translation { Id = 46, Key = "sessions.cancel", Culture = "en", Value = "Cancel" },
            new Translation { Id = 47, Key = "sessions.sendNotification", Culture = "pl", Value = "Wyślij powiadomienie" },
            new Translation { Id = 48, Key = "sessions.sendNotification", Culture = "en", Value = "Send notification" },
            new Translation { Id = 49, Key = "sessions.googleMeet", Culture = "pl", Value = "Link Google Meet" },
            new Translation { Id = 50, Key = "sessions.googleMeet", Culture = "en", Value = "Google Meet link" },
            new Translation { Id = 51, Key = "sessions.notes", Culture = "pl", Value = "Notatki" },
            new Translation { Id = 52, Key = "sessions.notes", Culture = "en", Value = "Notes" },
            new Translation { Id = 53, Key = "sessions.save", Culture = "pl", Value = "Zapisz" },
            new Translation { Id = 54, Key = "sessions.save", Culture = "en", Value = "Save" },
            new Translation { Id = 55, Key = "sessions.delete", Culture = "pl", Value = "Usuń" },
            new Translation { Id = 56, Key = "sessions.delete", Culture = "en", Value = "Delete" },
            
            // Patients translations
            new Translation { Id = 57, Key = "patients.title", Culture = "pl", Value = "Pacjenci" },
            new Translation { Id = 58, Key = "patients.title", Culture = "en", Value = "Patients" },
            new Translation { Id = 59, Key = "patients.new", Culture = "pl", Value = "Nowy pacjent" },
            new Translation { Id = 60, Key = "patients.new", Culture = "en", Value = "New patient" },
            new Translation { Id = 61, Key = "patients.edit", Culture = "pl", Value = "Edytuj pacjenta" },
            new Translation { Id = 62, Key = "patients.edit", Culture = "en", Value = "Edit patient" },
            new Translation { Id = 63, Key = "patients.firstName", Culture = "pl", Value = "Imię" },
            new Translation { Id = 64, Key = "patients.firstName", Culture = "en", Value = "First name" },
            new Translation { Id = 65, Key = "patients.lastName", Culture = "pl", Value = "Nazwisko" },
            new Translation { Id = 66, Key = "patients.lastName", Culture = "en", Value = "Last name" },
            new Translation { Id = 67, Key = "patients.email", Culture = "pl", Value = "E-mail" },
            new Translation { Id = 68, Key = "patients.email", Culture = "en", Value = "Email" },
            new Translation { Id = 69, Key = "patients.phone", Culture = "pl", Value = "Telefon" },
            new Translation { Id = 70, Key = "patients.phone", Culture = "en", Value = "Phone" },
            new Translation { Id = 71, Key = "patients.notes", Culture = "pl", Value = "Notatki" },
            new Translation { Id = 72, Key = "patients.notes", Culture = "en", Value = "Notes" },
            new Translation { Id = 87, Key = "patients.dateOfBirth", Culture = "pl", Value = "Data urodzenia" },
            new Translation { Id = 88, Key = "patients.dateOfBirth", Culture = "en", Value = "Date of birth" },
            new Translation { Id = 89, Key = "patients.gender", Culture = "pl", Value = "Płeć" },
            new Translation { Id = 90, Key = "patients.gender", Culture = "en", Value = "Gender" },
            new Translation { Id = 91, Key = "patients.pesel", Culture = "pl", Value = "PESEL" },
            new Translation { Id = 92, Key = "patients.pesel", Culture = "en", Value = "PESEL" },
            new Translation { Id = 93, Key = "patients.street", Culture = "pl", Value = "Ulica" },
            new Translation { Id = 94, Key = "patients.street", Culture = "en", Value = "Street" },
            new Translation { Id = 95, Key = "patients.streetNumber", Culture = "pl", Value = "Numer" },
            new Translation { Id = 96, Key = "patients.streetNumber", Culture = "en", Value = "Number" },
            new Translation { Id = 97, Key = "patients.apartmentNumber", Culture = "pl", Value = "Nr lokalu" },
            new Translation { Id = 98, Key = "patients.apartmentNumber", Culture = "en", Value = "Apartment" },
            new Translation { Id = 99, Key = "patients.city", Culture = "pl", Value = "Miasto" },
            new Translation { Id = 100, Key = "patients.city", Culture = "en", Value = "City" },
            new Translation { Id = 101, Key = "patients.postalCode", Culture = "pl", Value = "Kod pocztowy" },
            new Translation { Id = 102, Key = "patients.postalCode", Culture = "en", Value = "Postal code" },
            new Translation { Id = 103, Key = "patients.country", Culture = "pl", Value = "Kraj" },
            new Translation { Id = 104, Key = "patients.country", Culture = "en", Value = "Country" },
            
            // Payments translations
            new Translation { Id = 73, Key = "payments.title", Culture = "pl", Value = "Płatności" },
            new Translation { Id = 74, Key = "payments.title", Culture = "en", Value = "Payments" },
            new Translation { Id = 75, Key = "payments.create", Culture = "pl", Value = "Utwórz płatność" },
            new Translation { Id = 76, Key = "payments.create", Culture = "en", Value = "Create payment" },
            new Translation { Id = 77, Key = "payments.amount", Culture = "pl", Value = "Kwota" },
            new Translation { Id = 78, Key = "payments.amount", Culture = "en", Value = "Amount" },
            new Translation { Id = 79, Key = "payments.pay", Culture = "pl", Value = "Zapłać" },
            new Translation { Id = 80, Key = "payments.pay", Culture = "en", Value = "Pay" },
            
            // SMS translations
            new Translation { Id = 81, Key = "sms.session.confirmed", Culture = "pl", Value = "Sesja potwierdzona na {date} o {time}. Link do spotkania: {link}" },
            new Translation { Id = 82, Key = "sms.session.confirmed", Culture = "en", Value = "Session confirmed on {date} at {time}. Meeting link: {link}" },
            new Translation { Id = 83, Key = "sms.session.changed", Culture = "pl", Value = "Sesja została zmieniona. Nowa data: {date} o {time}. Link: {link}" },
            new Translation { Id = 84, Key = "sms.session.changed", Culture = "en", Value = "Session has been changed. New date: {date} at {time}. Link: {link}" },
            new Translation { Id = 85, Key = "sms.session.cancelled", Culture = "pl", Value = "Sesja na {date} została anulowana." },
            new Translation { Id = 86, Key = "sms.session.cancelled", Culture = "en", Value = "Session on {date} has been cancelled." }
        );

        // Konfiguracja Patient
        modelBuilder.Entity<Patient>().ToTable("Patient").HasKey(x => x.Id);
        modelBuilder.Entity<Patient>().Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<Patient>().Property(x => x.LastName).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<Patient>().Property(x => x.Email).IsRequired().HasMaxLength(256);
        modelBuilder.Entity<Patient>().Property(x => x.Phone).HasMaxLength(20);
        modelBuilder.Entity<Patient>().Property(x => x.Gender).HasMaxLength(10);
        modelBuilder.Entity<Patient>().Property(x => x.Pesel).HasMaxLength(11);
        modelBuilder.Entity<Patient>().Property(x => x.Street).HasMaxLength(200);
        modelBuilder.Entity<Patient>().Property(x => x.StreetNumber).HasMaxLength(20);
        modelBuilder.Entity<Patient>().Property(x => x.ApartmentNumber).HasMaxLength(20);
        modelBuilder.Entity<Patient>().Property(x => x.City).HasMaxLength(100);
        modelBuilder.Entity<Patient>().Property(x => x.PostalCode).HasMaxLength(10);
        modelBuilder.Entity<Patient>().Property(x => x.Country).HasMaxLength(100);
        modelBuilder.Entity<Patient>().HasIndex(x => x.Email);
        modelBuilder.Entity<Patient>().HasIndex(x => x.Pesel);
        modelBuilder.Entity<Patient>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Konfiguracja Session
        modelBuilder.Entity<Session>().ToTable("Session").HasKey(x => x.Id);
        modelBuilder.Entity<Session>().Property(x => x.StartDateTime).IsRequired();
        modelBuilder.Entity<Session>().Property(x => x.EndDateTime).IsRequired();
        modelBuilder.Entity<Session>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Session>().HasIndex(x => x.StartDateTime);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Terapeuta)
            .WithMany()
            .HasForeignKey(x => x.TerapeutaId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Payment)
            .WithOne(x => x.Session)
            .HasForeignKey<Payment>(x => x.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Konfiguracja Payment
        modelBuilder.Entity<Payment>().ToTable("Payment").HasKey(x => x.Id);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        modelBuilder.Entity<Payment>().HasIndex(x => x.TpayTransactionId);

        // Seed enum: SessionStatus with PL/EN
        modelBuilder.Entity<EnumType>().HasData(new EnumType { Id = 2, Name = nameof(SessionStatus) });
        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue { Id = 4, EnumTypeId = 2, Code = nameof(SessionStatus.Scheduled) },
            new EnumValue { Id = 5, EnumTypeId = 2, Code = nameof(SessionStatus.Confirmed) },
            new EnumValue { Id = 6, EnumTypeId = 2, Code = nameof(SessionStatus.Completed) },
            new EnumValue { Id = 7, EnumTypeId = 2, Code = nameof(SessionStatus.Cancelled) }
        );
        modelBuilder.Entity<EnumValueTranslation>().HasData(
            new EnumValueTranslation { Id = 7, EnumValueId = 4, Culture = "pl", Name = "Zaplanowana" },
            new EnumValueTranslation { Id = 8, EnumValueId = 4, Culture = "en", Name = "Scheduled" },
            new EnumValueTranslation { Id = 9, EnumValueId = 5, Culture = "pl", Name = "Potwierdzona" },
            new EnumValueTranslation { Id = 10, EnumValueId = 5, Culture = "en", Name = "Confirmed" },
            new EnumValueTranslation { Id = 11, EnumValueId = 6, Culture = "pl", Name = "Zakończona" },
            new EnumValueTranslation { Id = 12, EnumValueId = 6, Culture = "en", Name = "Completed" },
            new EnumValueTranslation { Id = 13, EnumValueId = 7, Culture = "pl", Name = "Anulowana" },
            new EnumValueTranslation { Id = 14, EnumValueId = 7, Culture = "en", Name = "Cancelled" }
        );

        // Seed enum: PaymentStatus with PL/EN
        modelBuilder.Entity<EnumType>().HasData(new EnumType { Id = 3, Name = nameof(PaymentStatus) });
        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue { Id = 8, EnumTypeId = 3, Code = nameof(PaymentStatus.Pending) },
            new EnumValue { Id = 9, EnumTypeId = 3, Code = nameof(PaymentStatus.Completed) },
            new EnumValue { Id = 10, EnumTypeId = 3, Code = nameof(PaymentStatus.Failed) }
        );
        modelBuilder.Entity<EnumValueTranslation>().HasData(
            new EnumValueTranslation { Id = 15, EnumValueId = 8, Culture = "pl", Name = "Oczekująca" },
            new EnumValueTranslation { Id = 16, EnumValueId = 8, Culture = "en", Name = "Pending" },
            new EnumValueTranslation { Id = 17, EnumValueId = 9, Culture = "pl", Name = "Zrealizowana" },
            new EnumValueTranslation { Id = 18, EnumValueId = 9, Culture = "en", Name = "Completed" },
            new EnumValueTranslation { Id = 19, EnumValueId = 10, Culture = "pl", Name = "Nieudana" },
            new EnumValueTranslation { Id = 20, EnumValueId = 10, Culture = "en", Name = "Failed" }
        );
    }
}

