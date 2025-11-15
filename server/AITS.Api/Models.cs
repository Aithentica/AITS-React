using System;
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

public enum UserRole
{
    Administrator = 1,
    Terapeuta = 2,
    TerapeutaFreeAccess = 3,
    Pacjent = 4
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    PendingVerification = 3,
    Suspended = 4
}

public sealed class ApplicationUser : IdentityUser
{
    // Status użytkownika w systemie
    public int StatusId { get; set; } = (int)UserStatus.Active;
    
    // Relacja 1:1 z Patient (jeśli użytkownik jest pacjentem)
    public Patient? PatientProfile { get; set; }
    
    // Relacja 1:1 z TherapistProfile (jeśli użytkownik jest terapeutą)
    public TherapistProfile? TherapistProfile { get; set; }
    
    // Data utworzenia konta
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Data ostatniej aktywności
    public DateTime? LastActivityAt { get; set; }
    
    // Relacje do UserRoleMapping
    public ICollection<UserRoleMapping> RoleMappings { get; set; } = new List<UserRoleMapping>();
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

public enum SessionTranscriptionSource
{
    ManualText = 11,
    TextFile = 12,
    AudioRecording = 13,
    AudioUpload = 14,
    RealtimeRecording = 15,
    AudioFile = 16,
    VideoFile = 17,
    FinalTranscriptUpload = 18
}

public sealed class Patient
{
    public int Id { get; set; }
    
    // BEZPOŚREDNIA relacja z ApplicationUser (jeśli pacjent ma konto)
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LastSessionSummary { get; set; }
    
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
    public ICollection<PatientInformation> InformationEntries { get; set; } = new List<PatientInformation>();
    public ICollection<PatientTask> Tasks { get; set; } = new List<PatientTask>();
    public ICollection<PatientDiary> Diaries { get; set; } = new List<PatientDiary>();
}

public sealed class PatientInformationType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PatientInformation> InformationEntries { get; set; } = new List<PatientInformation>();
}

public sealed class PatientInformation
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int PatientInformationTypeId { get; set; }
    public PatientInformationType PatientInformationType { get; set; } = null!;
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
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
    public string? PreviousWeekEvents { get; set; }
    public string? PreviousSessionReflections { get; set; }
    public string? PersonalWorkDiscussion { get; set; }
    public string? TherapeuticIntervention { get; set; }
    public string? AgreedPersonalWork { get; set; }
    public string? SessionSummary { get; set; }
    public int? SessionTypeId { get; set; }
    public SessionType? SessionType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SessionTranscription> Transcriptions { get; set; } = new List<SessionTranscription>();
    public ICollection<SessionParameter> Parameters { get; set; } = new List<SessionParameter>();
    public ICollection<PatientTask> Tasks { get; set; } = new List<PatientTask>();
}

public sealed class SessionParameter
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public string ParameterName { get; set; } = string.Empty; // lęk, smutek, złość, radość, problem 1, problem 2, problem 3, problem 4
    public int Value { get; set; } // 0-10
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SessionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false; // Czy jest typem systemowym dostępnym dla wszystkich
    public string? CreatedByUserId { get; set; } // Właściciel (dla typów użytkownika)
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<SessionTypeTip> Tips { get; set; } = new List<SessionTypeTip>();
    public ICollection<SessionTypeQuestion> Questions { get; set; } = new List<SessionTypeQuestion>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}

public sealed class SessionTypeTip
{
    public int Id { get; set; }
    public int SessionTypeId { get; set; }
    public SessionType SessionType { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SessionTypeQuestion
{
    public int Id { get; set; }
    public int SessionTypeId { get; set; }
    public SessionType SessionType { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SessionTranscription
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public SessionTranscriptionSource Source { get; set; }
    public string TranscriptText { get; set; } = string.Empty;
    public string? SourceFileName { get; set; }
    public string? SourceFilePath { get; set; }
    public string? SourceContentType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public ICollection<SessionTranscriptionSegment> Segments { get; set; } = new List<SessionTranscriptionSegment>();
}

public sealed class SessionTranscriptionSegment
{
    public int Id { get; set; }
    public int SessionTranscriptionId { get; set; }
    public SessionTranscription SessionTranscription { get; set; } = null!;
    public string SpeakerTag { get; set; } = string.Empty;
    public TimeSpan StartOffset { get; set; }
    public TimeSpan EndOffset { get; set; }
    public string Content { get; set; } = string.Empty;
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

public sealed class TherapistGoogleToken
{
    public string TerapeutaId { get; set; } = string.Empty;
    public ApplicationUser Terapeuta { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class UserActivityLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TherapistProfile
{
    public string TherapistId { get; set; } = string.Empty;
    public ApplicationUser Therapist { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    // Dane działalności gospodarczej lub firmy/spółki
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; } // NIP
    public string? Regon { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessCity { get; set; }
    public string? BusinessPostalCode { get; set; }
    public string? BusinessCountry { get; set; } = "Polska";
    public bool IsCompany { get; set; } = false; // true = firma/spółka, false = działalność gospodarcza
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<TherapistDocument> Documents { get; set; } = new List<TherapistDocument>();
}

public sealed class TherapistDocument
{
    public int Id { get; set; }
    public string TherapistId { get; set; } = string.Empty;
    public TherapistProfile Therapist { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class PatientTask
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string TherapistId { get; set; } = string.Empty;
    public ApplicationUser Therapist { get; set; } = null!;
    public int? SessionId { get; set; }
    public Session? Session { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class PatientDiary
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateTime EntryDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Mood { get; set; } // np. "dobry", "zły", "neutralny"
    public int? MoodRating { get; set; } // 1-10
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class UserRoleMapping
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int RoleId { get; set; } // UserRole enum value
    
    // Data przypisania roli
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Kto przypisał rolę
    public string? AssignedByUserId { get; set; }
    public ApplicationUser? AssignedBy { get; set; }
    
    // Czy rola jest aktywna
    public bool IsActive { get; set; } = true;
}

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<EnumType> EnumTypes => Set<EnumType>();
    public DbSet<EnumValue> EnumValues => Set<EnumValue>();
    public DbSet<EnumValueTranslation> EnumValueTranslations => Set<EnumValueTranslation>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientInformationType> PatientInformationTypes => Set<PatientInformationType>();
    public DbSet<PatientInformation> PatientInformationEntries => Set<PatientInformation>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionType> SessionTypes => Set<SessionType>();
    public DbSet<SessionTypeTip> SessionTypeTips => Set<SessionTypeTip>();
    public DbSet<SessionTypeQuestion> SessionTypeQuestions => Set<SessionTypeQuestion>();
    public DbSet<SessionTranscription> SessionTranscriptions => Set<SessionTranscription>();
    public DbSet<SessionTranscriptionSegment> SessionTranscriptionSegments => Set<SessionTranscriptionSegment>();
    public DbSet<SessionParameter> SessionParameters => Set<SessionParameter>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<TherapistGoogleToken> TherapistGoogleTokens => Set<TherapistGoogleToken>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<TherapistProfile> TherapistProfiles => Set<TherapistProfile>();
    public DbSet<TherapistDocument> TherapistDocuments => Set<TherapistDocument>();
    public DbSet<UserRoleMapping> UserRoleMappings => Set<UserRoleMapping>();
    public DbSet<PatientTask> PatientTasks => Set<PatientTask>();
    public DbSet<PatientDiary> PatientDiaries => Set<PatientDiary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TherapistGoogleToken>(entity =>
        {
            entity.HasKey(x => x.TerapeutaId);
            entity.HasOne(x => x.Terapeuta)
                .WithOne()
                .HasForeignKey<TherapistGoogleToken>(x => x.TerapeutaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.Property(x => x.Path).HasMaxLength(256);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.StartedAtUtc);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TherapistProfile>(entity =>
        {
            entity.HasKey(x => x.TherapistId);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.CompanyName).HasMaxLength(200);
            entity.Property(x => x.TaxId).HasMaxLength(20);
            entity.Property(x => x.Regon).HasMaxLength(20);
            entity.Property(x => x.BusinessAddress).HasMaxLength(200);
            entity.Property(x => x.BusinessCity).HasMaxLength(100);
            entity.Property(x => x.BusinessPostalCode).HasMaxLength(10);
            entity.Property(x => x.BusinessCountry).HasMaxLength(100);
            entity.HasIndex(x => x.TaxId);
            entity.HasOne(x => x.Therapist)
                .WithOne()
                .HasForeignKey<TherapistProfile>(x => x.TherapistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TherapistDocument>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(500);
            entity.Property(x => x.FileName).IsRequired().HasMaxLength(260);
            entity.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.FileContent).IsRequired();
            entity.HasIndex(x => x.TherapistId);
            entity.HasIndex(x => x.UploadDate);
            entity.HasOne(x => x.Therapist)
                .WithMany(t => t.Documents)
                .HasForeignKey(x => x.TherapistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Translation>().HasIndex(x => new { x.Key, x.Culture }).IsUnique();

        modelBuilder.Entity<EnumType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<EnumValue>().HasIndex(x => new { x.EnumTypeId, x.Code }).IsUnique();
        modelBuilder.Entity<EnumValueTranslation>().HasIndex(x => new { x.EnumValueId, x.Culture }).IsUnique();
        
        modelBuilder.Entity<PatientInformationType>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<PatientInformation>(entity =>
        {
            entity.HasIndex(x => new { x.PatientId, x.PatientInformationTypeId }).IsUnique();
            entity.HasOne(x => x.Patient)
                .WithMany(p => p.InformationEntries)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PatientInformationType)
                .WithMany(t => t.InformationEntries)
                .HasForeignKey(x => x.PatientInformationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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

        var patientInfoSeededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<PatientInformationType>().HasData(
            new PatientInformationType { Id = 1, Code = "INITIAL_CONSULTATION", Name = "Konsultacja wstępna", DisplayOrder = 1, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 2, Code = "DEMOGRAPHIC_INTERVIEW", Name = "Wywiad demograficzny", DisplayOrder = 2, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 3, Code = "DEVELOPMENTAL_INTERVIEW", Name = "Wywiad rozwojowy", DisplayOrder = 3, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 4, Code = "PROBLEM_IDENTIFICATION", Name = "Określenie problemu (Co?)", DisplayOrder = 4, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 5, Code = "PROBLEM_DESCRIPTION", Name = "Opis Problemu (Co?) Szczegółowo", DisplayOrder = 5, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 6, Code = "CONCEPTUALIZATION", Name = "Konceptualizacja", DisplayOrder = 6, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 9, Code = "CONCEPTUALIZATION_LEVEL1", Name = "Poziom 1: \"Jak?\" (mapa procesów)", DisplayOrder = 61, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 10, Code = "CONCEPTUALIZATION_LEVEL2", Name = "Poziom 2: \"Dlaczego?\" (mechanizmy podtrzymujące)", DisplayOrder = 62, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 11, Code = "CONCEPTUALIZATION_SUMMARY", Name = "Podsumowanie: \"Co zmieniamy?\" (cele zmiany)", DisplayOrder = 63, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 7, Code = "STANDARD_SESSION", Name = "Sesja standardowa", DisplayOrder = 7, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 8, Code = "SMART_GOALS", Name = "Cele SMART", DisplayOrder = 8, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 12, Code = "SMART_GOALS_CONNECTIONS", Name = "Powiązania", DisplayOrder = 81, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 13, Code = "SMART_GOALS_DEFINITION", Name = "Definicja SMART", DisplayOrder = 82, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 14, Code = "SMART_GOALS_METRICS", Name = "Metryka i monitoring", DisplayOrder = 83, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 15, Code = "SMART_GOALS_ACTION_PLAN", Name = "Plan działania", DisplayOrder = 84, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 16, Code = "SMART_GOALS_BARRIERS", Name = "Bariery i wsparcie", DisplayOrder = 85, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 17, Code = "SMART_GOALS_REVIEW", Name = "Przegląd i weryfikacja", DisplayOrder = 86, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 18, Code = "SMART_GOALS_PRIORITY", Name = "Priorytet", DisplayOrder = 87, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 19, Code = "DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES", Name = "Wcześne doświadczenia", DisplayOrder = 31, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 20, Code = "DEVELOPMENTAL_INTERVIEW_ADOLESCENCE", Name = "Adolescencja", DisplayOrder = 32, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 21, Code = "DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL", Name = "Dorosłość - Pytania ogólne", DisplayOrder = 33, CreatedAt = patientInfoSeededAt },
            new PatientInformationType { Id = 22, Code = "DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY", Name = "Dorosłość - Pytania w kierunku cech nieprawidłowej osobowości", DisplayOrder = 34, CreatedAt = patientInfoSeededAt }
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
            new Translation { Id = 189, Key = "dashboard.noSessionsToday", Culture = "pl", Value = "Brak sesji na dzisiaj" },
            new Translation { Id = 190, Key = "dashboard.noSessionsToday", Culture = "en", Value = "No sessions today" },
            
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
            new Translation { Id = 71, Key = "patients.lastSessionSummary", Culture = "pl", Value = "Podsumowanie ostatniej sesji" },
            new Translation { Id = 72, Key = "patients.lastSessionSummary", Culture = "en", Value = "Last session summary" },
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
            new Translation { Id = 1001, Key = "patients.informationPanel.title", Culture = "pl", Value = "Kluczowe informacje terapeutyczne" },
            new Translation { Id = 1002, Key = "patients.informationPanel.title", Culture = "en", Value = "Key therapeutic information" },
            new Translation { Id = 1003, Key = "patients.informationPanel.empty", Culture = "pl", Value = "Brak danych. Uzupełnij informacje, aby mieć szybki dostęp podczas sesji." },
            new Translation { Id = 1004, Key = "patients.informationPanel.empty", Culture = "en", Value = "No data yet. Fill in the details to have quick access during sessions." },
            new Translation { Id = 1005, Key = "patients.informationPanel.lastUpdated", Culture = "pl", Value = "Ostatnia aktualizacja" },
            new Translation { Id = 1006, Key = "patients.informationPanel.lastUpdated", Culture = "en", Value = "Last updated" },
            
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
            new Translation { Id = 86, Key = "sms.session.cancelled", Culture = "en", Value = "Session on {date} has been cancelled." },

            // Integrations translations
            new Translation { Id = 1007, Key = "integrations.googleCalendar.nav", Culture = "pl", Value = "Integracje" },
            new Translation { Id = 1008, Key = "integrations.googleCalendar.nav", Culture = "en", Value = "Integrations" }
        );

        // Session transcriptions translations
        modelBuilder.Entity<Translation>().HasData(
            new Translation { Id = 105, Key = "sessions.transcriptions.title", Culture = "pl", Value = "Transkrypcje sesji" },
            new Translation { Id = 106, Key = "sessions.transcriptions.title", Culture = "en", Value = "Session transcriptions" },
            new Translation { Id = 107, Key = "sessions.transcriptions.subtitle", Culture = "pl", Value = "Każda nowa transkrypcja zastępuje poprzednią. Wybierz jedną z metod pozyskania tekstu rozmowy." },
            new Translation { Id = 108, Key = "sessions.transcriptions.subtitle", Culture = "en", Value = "Each new transcript replaces the previous one. Choose one of the methods to capture the conversation." },
            new Translation { Id = 109, Key = "sessions.transcriptions.refresh", Culture = "pl", Value = "Odśwież listę" },
            new Translation { Id = 110, Key = "sessions.transcriptions.refresh", Culture = "en", Value = "Refresh list" },
            new Translation { Id = 111, Key = "sessions.transcriptions.realtime.title", Culture = "pl", Value = "Nagrywanie z diarizacją w czasie rzeczywistym" },
            new Translation { Id = 112, Key = "sessions.transcriptions.realtime.title", Culture = "en", Value = "Real-time recording with diarization" },
            new Translation { Id = 113, Key = "sessions.transcriptions.realtime.statusRecording", Culture = "pl", Value = "Nagrywanie trwa" },
            new Translation { Id = 114, Key = "sessions.transcriptions.realtime.statusRecording", Culture = "en", Value = "Recording in progress" },
            new Translation { Id = 115, Key = "sessions.transcriptions.realtime.status.disconnected", Culture = "pl", Value = "Rozłączono" },
            new Translation { Id = 116, Key = "sessions.transcriptions.realtime.status.disconnected", Culture = "en", Value = "Disconnected" },
            new Translation { Id = 117, Key = "sessions.transcriptions.realtime.status.connecting", Culture = "pl", Value = "Łączenie..." },
            new Translation { Id = 118, Key = "sessions.transcriptions.realtime.status.connecting", Culture = "en", Value = "Connecting..." },
            new Translation { Id = 119, Key = "sessions.transcriptions.realtime.status.recording", Culture = "pl", Value = "Nagrywanie" },
            new Translation { Id = 120, Key = "sessions.transcriptions.realtime.status.recording", Culture = "en", Value = "Recording" },
            new Translation { Id = 121, Key = "sessions.transcriptions.realtime.status.stopping", Culture = "pl", Value = "Zamykanie..." },
            new Translation { Id = 122, Key = "sessions.transcriptions.realtime.status.stopping", Culture = "en", Value = "Stopping..." },
            new Translation { Id = 123, Key = "sessions.transcriptions.realtime.status.error", Culture = "pl", Value = "Błąd połączenia" },
            new Translation { Id = 124, Key = "sessions.transcriptions.realtime.status.error", Culture = "en", Value = "Error" },
            new Translation { Id = 125, Key = "sessions.transcriptions.realtime.description", Culture = "pl", Value = "Połącz się z Azure Speech i uzyskaj transkrypcję z diarizacją dla maksymalnie 3 osób. Wyniki aktualizują się na bieżąco." },
            new Translation { Id = 126, Key = "sessions.transcriptions.realtime.description", Culture = "en", Value = "Connect to Azure Speech and get a diarized transcript for up to 3 speakers. Results update continuously." },
            new Translation { Id = 127, Key = "sessions.transcriptions.realtime.connecting", Culture = "pl", Value = "Łączenie z usługą Azure..." },
            new Translation { Id = 128, Key = "sessions.transcriptions.realtime.connecting", Culture = "en", Value = "Connecting to Azure Speech..." },
            new Translation { Id = 129, Key = "sessions.transcriptions.realtime.stop", Culture = "pl", Value = "Zatrzymaj nagrywanie na żywo" },
            new Translation { Id = 130, Key = "sessions.transcriptions.realtime.stop", Culture = "en", Value = "Stop live recording" },
            new Translation { Id = 131, Key = "sessions.transcriptions.realtime.stopping", Culture = "pl", Value = "Zamykanie sesji..." },
            new Translation { Id = 132, Key = "sessions.transcriptions.realtime.stopping", Culture = "en", Value = "Closing session..." },
            new Translation { Id = 133, Key = "sessions.transcriptions.realtime.retry", Culture = "pl", Value = "Spróbuj ponownie" },
            new Translation { Id = 134, Key = "sessions.transcriptions.realtime.retry", Culture = "en", Value = "Try again" },
            new Translation { Id = 135, Key = "sessions.transcriptions.realtime.start", Culture = "pl", Value = "Rozpocznij nagrywanie na żywo" },
            new Translation { Id = 136, Key = "sessions.transcriptions.realtime.start", Culture = "en", Value = "Start live recording" },
            new Translation { Id = 137, Key = "sessions.transcriptions.realtime.error", Culture = "pl", Value = "Wystąpił błąd podczas nagrywania." },
            new Translation { Id = 138, Key = "sessions.transcriptions.realtime.error", Culture = "en", Value = "An error occurred while recording." },
            new Translation { Id = 139, Key = "sessions.transcriptions.realtime.stopError", Culture = "pl", Value = "Nie udało się zatrzymać nagrywania." },
            new Translation { Id = 140, Key = "sessions.transcriptions.realtime.stopError", Culture = "en", Value = "Failed to stop recording." },
            new Translation { Id = 141, Key = "sessions.transcriptions.success", Culture = "pl", Value = "Transkrypcja została zapisana (poprzednia wersja została zastąpiona)." },
            new Translation { Id = 142, Key = "sessions.transcriptions.success", Culture = "en", Value = "Transcription has been saved (the previous version was replaced)." },
            new Translation { Id = 143, Key = "sessions.transcriptions.error", Culture = "pl", Value = "Nie udało się przetworzyć pliku." },
            new Translation { Id = 144, Key = "sessions.transcriptions.error", Culture = "en", Value = "Could not process the file." },
            new Translation { Id = 145, Key = "sessions.transcriptions.audioUploadTitle", Culture = "pl", Value = "Transkrypcja z pliku audio (WAV/MP3)" },
            new Translation { Id = 146, Key = "sessions.transcriptions.audioUploadTitle", Culture = "en", Value = "Transcription from audio file (WAV/MP3)" },
            new Translation { Id = 147, Key = "sessions.transcriptions.audioUploadHint", Culture = "pl", Value = "Plik zostanie przesłany do Azure Speech i przetworzony z diarizacją." },
            new Translation { Id = 148, Key = "sessions.transcriptions.audioUploadHint", Culture = "en", Value = "The file will be sent to Azure Speech and processed with diarization." },
            new Translation { Id = 149, Key = "sessions.transcriptions.audioUploadButton", Culture = "pl", Value = "Wybierz plik audio" },
            new Translation { Id = 150, Key = "sessions.transcriptions.audioUploadButton", Culture = "en", Value = "Select audio file" },
            new Translation { Id = 151, Key = "sessions.transcriptions.videoUploadTitle", Culture = "pl", Value = "Transkrypcja z pliku wideo (MP4/MOV/MKV/AVI)" },
            new Translation { Id = 152, Key = "sessions.transcriptions.videoUploadTitle", Culture = "en", Value = "Transcription from video file (MP4/MOV/MKV/AVI)" },
            new Translation { Id = 153, Key = "sessions.transcriptions.videoUploadHint", Culture = "pl", Value = "Ścieżka audio zostanie wyodrębniona lokalnie i przetworzona w Azure Speech." },
            new Translation { Id = 154, Key = "sessions.transcriptions.videoUploadHint", Culture = "en", Value = "The audio track will be extracted locally and processed in Azure Speech." },
            new Translation { Id = 155, Key = "sessions.transcriptions.videoUploadButton", Culture = "pl", Value = "Wybierz plik wideo" },
            new Translation { Id = 156, Key = "sessions.transcriptions.videoUploadButton", Culture = "en", Value = "Select video file" },
            new Translation { Id = 157, Key = "sessions.transcriptions.transcriptUploadTitle", Culture = "pl", Value = "Wgraj gotową transkrypcję (TXT/VTT/SRT)" },
            new Translation { Id = 158, Key = "sessions.transcriptions.transcriptUploadTitle", Culture = "en", Value = "Upload final transcript (TXT/VTT/SRT)" },
            new Translation { Id = 159, Key = "sessions.transcriptions.transcriptUploadHint", Culture = "pl", Value = "Plik zostanie zapisany jako finalny transkrypt bez ponownego przetwarzania." },
            new Translation { Id = 160, Key = "sessions.transcriptions.transcriptUploadHint", Culture = "en", Value = "The file will be stored as the final transcript without further processing." },
            new Translation { Id = 161, Key = "sessions.transcriptions.transcriptUploadButton", Culture = "pl", Value = "Wybierz plik transkryptu" },
            new Translation { Id = 162, Key = "sessions.transcriptions.transcriptUploadButton", Culture = "en", Value = "Select transcript file" },
            new Translation { Id = 163, Key = "sessions.transcriptions.currentTitle", Culture = "pl", Value = "Aktualna transkrypcja" },
            new Translation { Id = 164, Key = "sessions.transcriptions.currentTitle", Culture = "en", Value = "Current transcript" },
            new Translation { Id = 165, Key = "sessions.transcriptions.preview", Culture = "pl", Value = "Pokaż podgląd" },
            new Translation { Id = 166, Key = "sessions.transcriptions.preview", Culture = "en", Value = "Show preview" },
            new Translation { Id = 167, Key = "sessions.transcriptions.empty", Culture = "pl", Value = "Brak zapisanej transkrypcji dla tej sesji." },
            new Translation { Id = 168, Key = "sessions.transcriptions.empty", Culture = "en", Value = "No transcript saved for this session." },
            new Translation { Id = 169, Key = "sessions.transcriptions.previewTitle", Culture = "pl", Value = "Podgląd transkrypcji" },
            new Translation { Id = 170, Key = "sessions.transcriptions.previewTitle", Culture = "en", Value = "Transcript preview" },
            new Translation { Id = 171, Key = "sessions.transcriptions.download", Culture = "pl", Value = "Pobierz źródło" },
            new Translation { Id = 172, Key = "sessions.transcriptions.download", Culture = "en", Value = "Download source" },
            new Translation { Id = 173, Key = "sessions.transcriptions.source.manual", Culture = "pl", Value = "Tekst ręczny" },
            new Translation { Id = 174, Key = "sessions.transcriptions.source.manual", Culture = "en", Value = "Manual text" },
            new Translation { Id = 175, Key = "sessions.transcriptions.source.textFile", Culture = "pl", Value = "Plik transkryptu" },
            new Translation { Id = 176, Key = "sessions.transcriptions.source.textFile", Culture = "en", Value = "Transcript file" },
            new Translation { Id = 177, Key = "sessions.transcriptions.source.audioRecording", Culture = "pl", Value = "Nagranie mikrofonem" },
            new Translation { Id = 178, Key = "sessions.transcriptions.source.audioRecording", Culture = "en", Value = "Microphone recording" },
            new Translation { Id = 179, Key = "sessions.transcriptions.source.audioUpload", Culture = "pl", Value = "Plik audio" },
            new Translation { Id = 180, Key = "sessions.transcriptions.source.audioUpload", Culture = "en", Value = "Audio file" },
            new Translation { Id = 181, Key = "sessions.transcriptions.source.video", Culture = "pl", Value = "Plik wideo" },
            new Translation { Id = 182, Key = "sessions.transcriptions.source.video", Culture = "en", Value = "Video file" },
            new Translation { Id = 183, Key = "sessions.transcriptions.source.realtime", Culture = "pl", Value = "Nagrywanie na żywo" },
            new Translation { Id = 184, Key = "sessions.transcriptions.source.realtime", Culture = "en", Value = "Live recording" },
            new Translation { Id = 185, Key = "sessions.transcriptions.source.unknown", Culture = "pl", Value = "Nieznane źródło" },
            new Translation { Id = 186, Key = "sessions.transcriptions.source.unknown", Culture = "en", Value = "Unknown source" },
            new Translation { Id = 187, Key = "common.close", Culture = "pl", Value = "Zamknij" },
            new Translation { Id = 188, Key = "common.close", Culture = "en", Value = "Close" },
            new Translation { Id = 191, Key = "common.refresh", Culture = "pl", Value = "Odśwież" },
            new Translation { Id = 192, Key = "common.refresh", Culture = "en", Value = "Refresh" },
            new Translation { Id = 193, Key = "common.logout", Culture = "pl", Value = "Wyloguj" },
            new Translation { Id = 194, Key = "common.logout", Culture = "en", Value = "Log out" }
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
        modelBuilder.Entity<Patient>().HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        modelBuilder.Entity<Patient>()
            .HasOne(x => x.User)
            .WithOne(u => u.PatientProfile)
            .HasForeignKey<Patient>(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Patient>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Konfiguracja SessionType
        modelBuilder.Entity<SessionType>().ToTable("SessionType").HasKey(x => x.Id);
        modelBuilder.Entity<SessionType>().Property(x => x.Name).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<SessionType>().Property(x => x.Description).HasMaxLength(1000);
        modelBuilder.Entity<SessionType>().Property(x => x.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<SessionType>().Property(x => x.IsSystem).HasDefaultValue(false);
        modelBuilder.Entity<SessionType>().Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<SessionType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<SessionType>().HasIndex(x => x.IsSystem);
        modelBuilder.Entity<SessionType>().HasIndex(x => x.CreatedByUserId);
        modelBuilder.Entity<SessionType>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SessionTypeTip>().ToTable("SessionTypeTip").HasKey(x => x.Id);
        modelBuilder.Entity<SessionTypeTip>().Property(x => x.Content).IsRequired().HasMaxLength(2000);
        modelBuilder.Entity<SessionTypeTip>().Property(x => x.DisplayOrder).HasDefaultValue(0);
        modelBuilder.Entity<SessionTypeTip>().Property(x => x.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<SessionTypeTip>().HasIndex(x => new { x.SessionTypeId, x.DisplayOrder });
        modelBuilder.Entity<SessionTypeTip>()
            .HasOne(x => x.SessionType)
            .WithMany(x => x.Tips)
            .HasForeignKey(x => x.SessionTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessionTypeQuestion>().ToTable("SessionTypeQuestion").HasKey(x => x.Id);
        modelBuilder.Entity<SessionTypeQuestion>().Property(x => x.Content).IsRequired().HasMaxLength(2000);
        modelBuilder.Entity<SessionTypeQuestion>().Property(x => x.DisplayOrder).HasDefaultValue(0);
        modelBuilder.Entity<SessionTypeQuestion>().Property(x => x.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<SessionTypeQuestion>().HasIndex(x => new { x.SessionTypeId, x.DisplayOrder });
        modelBuilder.Entity<SessionTypeQuestion>()
            .HasOne(x => x.SessionType)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.SessionTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konfiguracja Session
        modelBuilder.Entity<Session>().ToTable("Session").HasKey(x => x.Id);
        modelBuilder.Entity<Session>().Property(x => x.StartDateTime).IsRequired();
        modelBuilder.Entity<Session>().Property(x => x.EndDateTime).IsRequired();
        modelBuilder.Entity<Session>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Session>().HasIndex(x => x.StartDateTime);
        
        modelBuilder.Entity<SessionParameter>(entity =>
        {
            entity.ToTable("SessionParameter").HasKey(x => x.Id);
            entity.Property(x => x.ParameterName).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Value).IsRequired();
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => new { x.SessionId, x.ParameterName }).IsUnique();
            entity.HasOne(x => x.Session)
                .WithMany(s => s.Parameters)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
        modelBuilder.Entity<Session>()
            .HasOne(x => x.SessionType)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.SessionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Konfiguracja SessionTranscription
        modelBuilder.Entity<SessionTranscription>().ToTable("SessionTranscription").HasKey(x => x.Id);
        modelBuilder.Entity<SessionTranscription>().Property(x => x.TranscriptText).IsRequired();
        modelBuilder.Entity<SessionTranscription>().Property(x => x.SourceFileName).HasMaxLength(260);
        modelBuilder.Entity<SessionTranscription>().Property(x => x.SourceFilePath).HasMaxLength(500);
        modelBuilder.Entity<SessionTranscription>().Property(x => x.SourceContentType).HasMaxLength(100);
        modelBuilder.Entity<SessionTranscription>().HasIndex(x => x.SessionId);
        modelBuilder.Entity<SessionTranscription>()
            .HasOne(x => x.Session)
            .WithMany(x => x.Transcriptions)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SessionTranscription>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SessionTranscriptionSegment>().ToTable("SessionTranscriptionSegment").HasKey(x => x.Id);
        modelBuilder.Entity<SessionTranscriptionSegment>().Property(x => x.SpeakerTag).IsRequired().HasMaxLength(50);
        modelBuilder.Entity<SessionTranscriptionSegment>().Property(x => x.Content).IsRequired();
        modelBuilder.Entity<SessionTranscriptionSegment>().Property(x => x.StartOffset).HasColumnType("time");
        modelBuilder.Entity<SessionTranscriptionSegment>().Property(x => x.EndOffset).HasColumnType("time");
        modelBuilder.Entity<SessionTranscriptionSegment>().HasIndex(x => x.SessionTranscriptionId);
        modelBuilder.Entity<SessionTranscriptionSegment>()
            .HasOne(x => x.SessionTranscription)
            .WithMany(x => x.Segments)
            .HasForeignKey(x => x.SessionTranscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // Seed enum: SessionTranscriptionSource with PL/EN
        modelBuilder.Entity<EnumType>().HasData(new EnumType { Id = 4, Name = nameof(SessionTranscriptionSource) });
        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue { Id = 11, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.ManualText) },
            new EnumValue { Id = 12, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.TextFile) },
            new EnumValue { Id = 13, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.AudioRecording) },
            new EnumValue { Id = 14, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.AudioUpload) },
            new EnumValue { Id = 15, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.RealtimeRecording) },
            new EnumValue { Id = 16, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.AudioFile) },
            new EnumValue { Id = 17, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.VideoFile) },
            new EnumValue { Id = 18, EnumTypeId = 4, Code = nameof(SessionTranscriptionSource.FinalTranscriptUpload) }
        );
        modelBuilder.Entity<EnumValueTranslation>().HasData(
            new EnumValueTranslation { Id = 21, EnumValueId = 11, Culture = "pl", Name = "Transkrypcja ręczna" },
            new EnumValueTranslation { Id = 22, EnumValueId = 11, Culture = "en", Name = "Manual text" },
            new EnumValueTranslation { Id = 23, EnumValueId = 12, Culture = "pl", Name = "Plik tekstowy" },
            new EnumValueTranslation { Id = 24, EnumValueId = 12, Culture = "en", Name = "Text file" },
            new EnumValueTranslation { Id = 25, EnumValueId = 13, Culture = "pl", Name = "Nagrywanie mikrofonem" },
            new EnumValueTranslation { Id = 26, EnumValueId = 13, Culture = "en", Name = "Microphone recording" },
            new EnumValueTranslation { Id = 27, EnumValueId = 14, Culture = "pl", Name = "Przesłany plik audio" },
            new EnumValueTranslation { Id = 28, EnumValueId = 14, Culture = "en", Name = "Uploaded audio file" },
            new EnumValueTranslation { Id = 29, EnumValueId = 15, Culture = "pl", Name = "Nagrywanie na żywo" },
            new EnumValueTranslation { Id = 30, EnumValueId = 15, Culture = "en", Name = "Realtime recording" },
            new EnumValueTranslation { Id = 31, EnumValueId = 16, Culture = "pl", Name = "Transkrypcja pliku audio" },
            new EnumValueTranslation { Id = 32, EnumValueId = 16, Culture = "en", Name = "Audio file transcription" },
            new EnumValueTranslation { Id = 33, EnumValueId = 17, Culture = "pl", Name = "Transkrypcja pliku wideo" },
            new EnumValueTranslation { Id = 34, EnumValueId = 17, Culture = "en", Name = "Video file transcription" },
            new EnumValueTranslation { Id = 35, EnumValueId = 18, Culture = "pl", Name = "Gotowy transkrypt" },
            new EnumValueTranslation { Id = 36, EnumValueId = 18, Culture = "en", Name = "Uploaded transcript" }
        );

        // Konfiguracja ApplicationUser - dodatkowe pola
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.StatusId).HasDefaultValue((int)UserStatus.Active);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(x => x.StatusId);
        });

        // Konfiguracja UserRoleMapping
        modelBuilder.Entity<UserRoleMapping>(entity =>
        {
            entity.ToTable("UserRoleMapping").HasKey(x => x.Id);
            entity.Property(x => x.RoleId).IsRequired();
            entity.Property(x => x.AssignedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.RoleId);
            entity.HasIndex(x => x.IsActive);
            entity.HasOne(x => x.User)
                .WithMany(u => u.RoleMappings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedBy)
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Konfiguracja PatientTask
        modelBuilder.Entity<PatientTask>().ToTable("PatientTask").HasKey(x => x.Id);
        modelBuilder.Entity<PatientTask>().Property(x => x.Title).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<PatientTask>().Property(x => x.Description).HasMaxLength(2000);
        modelBuilder.Entity<PatientTask>().Property(x => x.DueDate).IsRequired();
        modelBuilder.Entity<PatientTask>().Property(x => x.IsCompleted).HasDefaultValue(false);
        modelBuilder.Entity<PatientTask>().Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<PatientTask>().HasIndex(x => x.PatientId);
        modelBuilder.Entity<PatientTask>().HasIndex(x => x.TherapistId);
        modelBuilder.Entity<PatientTask>().HasIndex(x => x.DueDate);
        modelBuilder.Entity<PatientTask>()
            .HasOne(x => x.Patient)
            .WithMany(p => p.Tasks)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientTask>()
            .HasOne(x => x.Therapist)
            .WithMany()
            .HasForeignKey(x => x.TherapistId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientTask>()
            .HasOne(x => x.Session)
            .WithMany(s => s.Tasks)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Konfiguracja PatientDiary
        modelBuilder.Entity<PatientDiary>().ToTable("PatientDiary").HasKey(x => x.Id);
        modelBuilder.Entity<PatientDiary>().Property(x => x.Title).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<PatientDiary>().Property(x => x.Content).IsRequired();
        modelBuilder.Entity<PatientDiary>().Property(x => x.EntryDate).IsRequired();
        modelBuilder.Entity<PatientDiary>().Property(x => x.Mood).HasMaxLength(50);
        modelBuilder.Entity<PatientDiary>().Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<PatientDiary>().HasIndex(x => x.PatientId);
        modelBuilder.Entity<PatientDiary>().HasIndex(x => x.EntryDate);
        modelBuilder.Entity<PatientDiary>()
            .HasOne(x => x.Patient)
            .WithMany(p => p.Diaries)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed enum: UserRole
        modelBuilder.Entity<EnumType>().HasData(new EnumType { Id = 5, Name = nameof(UserRole) });
        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue { Id = 19, EnumTypeId = 5, Code = nameof(UserRole.Administrator) },
            new EnumValue { Id = 20, EnumTypeId = 5, Code = nameof(UserRole.Terapeuta) },
            new EnumValue { Id = 21, EnumTypeId = 5, Code = nameof(UserRole.TerapeutaFreeAccess) },
            new EnumValue { Id = 22, EnumTypeId = 5, Code = nameof(UserRole.Pacjent) }
        );
        modelBuilder.Entity<EnumValueTranslation>().HasData(
            new EnumValueTranslation { Id = 37, EnumValueId = 19, Culture = "pl", Name = "Administrator" },
            new EnumValueTranslation { Id = 38, EnumValueId = 19, Culture = "en", Name = "Administrator" },
            new EnumValueTranslation { Id = 39, EnumValueId = 20, Culture = "pl", Name = "Terapeuta" },
            new EnumValueTranslation { Id = 40, EnumValueId = 20, Culture = "en", Name = "Therapist" },
            new EnumValueTranslation { Id = 41, EnumValueId = 21, Culture = "pl", Name = "Terapeuta z darmowym dostępem" },
            new EnumValueTranslation { Id = 42, EnumValueId = 21, Culture = "en", Name = "Therapist with free access" },
            new EnumValueTranslation { Id = 43, EnumValueId = 22, Culture = "pl", Name = "Pacjent" },
            new EnumValueTranslation { Id = 44, EnumValueId = 22, Culture = "en", Name = "Patient" }
        );

        // Seed enum: UserStatus
        modelBuilder.Entity<EnumType>().HasData(new EnumType { Id = 6, Name = nameof(UserStatus) });
        modelBuilder.Entity<EnumValue>().HasData(
            new EnumValue { Id = 23, EnumTypeId = 6, Code = nameof(UserStatus.Active) },
            new EnumValue { Id = 24, EnumTypeId = 6, Code = nameof(UserStatus.Inactive) },
            new EnumValue { Id = 25, EnumTypeId = 6, Code = nameof(UserStatus.PendingVerification) },
            new EnumValue { Id = 26, EnumTypeId = 6, Code = nameof(UserStatus.Suspended) }
        );
        modelBuilder.Entity<EnumValueTranslation>().HasData(
            new EnumValueTranslation { Id = 45, EnumValueId = 23, Culture = "pl", Name = "Aktywny" },
            new EnumValueTranslation { Id = 46, EnumValueId = 23, Culture = "en", Name = "Active" },
            new EnumValueTranslation { Id = 47, EnumValueId = 24, Culture = "pl", Name = "Nieaktywny" },
            new EnumValueTranslation { Id = 48, EnumValueId = 24, Culture = "en", Name = "Inactive" },
            new EnumValueTranslation { Id = 49, EnumValueId = 25, Culture = "pl", Name = "Oczekuje na weryfikację" },
            new EnumValueTranslation { Id = 50, EnumValueId = 25, Culture = "en", Name = "Pending verification" },
            new EnumValueTranslation { Id = 51, EnumValueId = 26, Culture = "pl", Name = "Zawieszony" },
            new EnumValueTranslation { Id = 52, EnumValueId = 26, Culture = "en", Name = "Suspended" }
        );
    }
}

