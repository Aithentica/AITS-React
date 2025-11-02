using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace AITS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        AppDbContext db, 
        IConfiguration configuration,
        ILogger<DiagnosticsController> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Testuje połączenie z bazą danych używając prostego zapytania SQL
    /// </summary>
    [HttpGet("test-db-connection")]
    public async Task<IActionResult> TestDbConnection()
    {
        var result = new
        {
            success = false,
            message = "",
            connectionString = "",
            serverInfo = "",
            databaseInfo = "",
            tablesCount = 0,
            error = (string?)null,
            timestamp = DateTime.UtcNow
        };

        try
        {
            var connectionString = _configuration.GetConnectionString("Default");
            if (string.IsNullOrEmpty(connectionString))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Connection string nie jest skonfigurowany",
                    connectionString = "",
                    error = "ConnectionStrings__Default is null or empty"
                });
            }

            // Ukryj hasło w logach
            var safeConnectionString = connectionString;
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = "***";
                    safeConnectionString = builder.ConnectionString;
                }
            }
            catch { }

            // Test 1: Sprawdź czy można otworzyć połączenie
            var canConnect = await _db.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Nie można nawiązać połączenia z bazą danych",
                    connectionString = safeConnectionString,
                    error = "Database.CanConnectAsync() returned false"
                });
            }

            // Test 2: Wykonaj zapytanie SELECT TOP 1 FROM sys.tables (jak sugerował użytkownik)
            var firstTable = await _db.Database
                .SqlQueryRaw<TableInfo>("SELECT TOP 1 name, object_id, create_date FROM sys.tables ORDER BY name")
                .FirstOrDefaultAsync();

            // Test 3: Pobierz liczbę tabel
            var tablesCountResult = await _db.Database
                .SqlQueryRaw<CountResult>("SELECT COUNT(*) as Value FROM sys.tables")
                .FirstOrDefaultAsync();
            var tablesCount = tablesCountResult?.Value ?? 0;

            // Test 4: Pobierz informacje o serwerze
            var serverNameResult = await _db.Database
                .SqlQueryRaw<NameResult>("SELECT @@SERVERNAME as Value")
                .FirstOrDefaultAsync();
            var serverName = serverNameResult?.Value ?? "Unknown";

            var databaseNameResult = await _db.Database
                .SqlQueryRaw<NameResult>("SELECT DB_NAME() as Value")
                .FirstOrDefaultAsync();
            var databaseName = databaseNameResult?.Value ?? "Unknown";

            return Ok(new
            {
                success = true,
                message = "Połączenie z bazą danych działa poprawnie",
                connectionString = safeConnectionString,
                serverInfo = new
                {
                    serverName = serverName ?? "Unknown",
                    databaseName = databaseName ?? "Unknown"
                },
                databaseInfo = new
                {
                    tablesCount = tablesCount,
                    firstTable = firstTable != null ? new
                    {
                        firstTable.name,
                        firstTable.object_id,
                        firstTable.create_date
                    } : null
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (SqlException sqlEx)
        {
            _logger.LogError(sqlEx, "Błąd SQL podczas testowania połączenia z bazą");
            return StatusCode(503, new
            {
                success = false,
                message = "Błąd SQL Server",
                connectionString = "",
                error = $"SQL Error {sqlEx.Number}: {sqlEx.Message}",
                sqlException = new
                {
                    sqlEx.Number,
                    sqlEx.State,
                    sqlEx.Class,
                    sqlEx.Server,
                    sqlEx.Procedure
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas testowania połączenia z bazą");
            return StatusCode(500, new
            {
                success = false,
                message = "Nieoczekiwany błąd podczas testowania połączenia",
                connectionString = "",
                error = ex.Message,
                exceptionType = ex.GetType().Name,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private sealed class TableInfo
    {
        public string name { get; set; } = string.Empty;
        public int object_id { get; set; }
        public DateTime create_date { get; set; }
    }

    private sealed class CountResult
    {
        public int Value { get; set; }
    }

    private sealed class NameResult
    {
        public string Value { get; set; } = string.Empty;
    }
}

