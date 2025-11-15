using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AITS.Api.Configuration;
using AITS.Api.Hubs;
using AITS.Api.Services;
using AITS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

const long MaxUploadSizeBytes = 200L * 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);

// Włącz User Secrets dla środowiska Development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadSizeBytes;
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    // Wyłącz ostrzeżenie o pending changes - pozwala aplikacji działać nawet jeśli są niewielkie różnice
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsTherapist", policy => 
        policy.RequireAssertion(context => 
            context.User.HasClaim("RoleId", "2") || 
            context.User.HasClaim("RoleId", "3") ||
            context.User.IsInRole(Roles.Administrator)));
    
    options.AddPolicy("IsPatient", policy => 
        policy.RequireAssertion(context => 
            context.User.HasClaim("RoleId", "4")));
    
    options.AddPolicy("IsAdministrator", policy => 
        policy.RequireAssertion(context => 
            context.User.HasClaim("RoleId", "1") ||
            context.User.IsInRole(Roles.Administrator)));
    
    options.AddPolicy("IsTherapistOrAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("RoleId", "1") ||
            context.User.HasClaim("RoleId", "2") ||
            context.User.HasClaim("RoleId", "3") ||
            context.User.IsInRole(Roles.Administrator) ||
            context.User.IsInRole(Roles.Terapeuta) ||
            context.User.IsInRole(Roles.TerapeutaFreeAccess)));
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddSignalR();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadSizeBytes;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<SmsConfiguration>()
    .Bind(builder.Configuration.GetSection("SMS"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<EmailConfiguration>()
    .Bind(builder.Configuration.GetSection("Email"))
    .ValidateDataAnnotations();

builder.Services
    .AddOptions<GoogleOAuthOptions>()
    .Bind(builder.Configuration.GetSection("GoogleOAuth"));

builder.Services
    .AddOptions<AzureAIOptions>()
    .Bind(builder.Configuration.GetSection("AzureAI"))
    .ValidateDataAnnotations();

builder.Services
    .AddOptions<AzureSpeechOptions>()
    .Bind(builder.Configuration.GetSection("AzureSpeech"))
    .ValidateDataAnnotations();

builder.Services.AddHttpClient<AITS.Api.Services.PaymentService>();
builder.Services.AddScoped<AITS.Api.Services.GoogleCalendarService>();
builder.Services.AddScoped<ISmsApiClient, SmsApiClient>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAzureSpeechService, AzureSpeechService>();
builder.Services.AddHttpClient<IAzureAIService, AzureAIService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddHttpClient("google-oauth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();

// Background service do automatycznego odświeżania tokenów Google OAuth
builder.Services.AddHostedService<GoogleTokenRefreshBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userRoleService = scope.ServiceProvider.GetRequiredService<IUserRoleService>();
        
        string[] roles = [Roles.Administrator, Roles.Pacjent, Roles.Terapeuta, Roles.TerapeutaFreeAccess];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
        
        // Opcjonalny użytkownik admin dev
        var adminEmail = "admin@aits.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, Roles.Administrator);
            // Synchronizuj role do UserRoleMapping
            await userRoleService.AssignRoleAsync(admin.Id, UserRole.Administrator);
        }
        else
        {
            // Synchronizuj istniejące role dla wszystkich użytkowników
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                await userRoleService.SyncRolesFromIdentityAsync(user.Id);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Błąd podczas migracji/seedowania bazy. Aplikacja kontynuuje, ale niektóre funkcje mogą nie działać.");
        
        // Jeśli to błąd połączenia z SQL Server, wyświetl bardziej szczegółowy komunikat
        Microsoft.Data.SqlClient.SqlException? sqlException = ex as Microsoft.Data.SqlClient.SqlException 
            ?? ex.InnerException as Microsoft.Data.SqlClient.SqlException;
        
        if (sqlException != null)
        {
            var connectionString = builder.Configuration.GetConnectionString("Default");
            var serverInfo = !string.IsNullOrEmpty(connectionString) && connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                ? connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                : "nieznany";
            
            logger.LogError("Szczegóły błędu SQL Server:");
            logger.LogError("  - Connection String Server: {ServerInfo}", serverInfo);
            logger.LogError("  - SQL Error Number: {ErrorNumber}", sqlException.Number);
            logger.LogError("  - SQL Error Message: {ErrorMessage}", sqlException.Message);
            logger.LogError("  - Sprawdź czy SQL Server działa i czy connection string w pliku .env jest poprawny");
            logger.LogError("  - Dla Docker Desktop użyj: Server=host.docker.internal,1433");
            logger.LogError("  - Dla Docker na Linuxie użyj IP hosta zamiast host.docker.internal");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TranscriptionHub>("/hubs/transcriptions");

app.Run();

static string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, string issuer, string audience, SymmetricSecurityKey key)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
        new(ClaimTypes.Email, user.Email ?? user.UserName ?? user.Id)
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(12),
        signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}
