Param(
    [string]$SqlServerInstance = 'MSI',
    [string]$DatabaseName = 'AIts-react',
    [string]$JwtKey = 'DEV_KEY_CHANGE_ME_MIN_32_CHARS_1234567890',
    [switch]$NoTests,
    [switch]$SkipEfUpdate,
    [switch]$Detach
)

$ErrorActionPreference = 'Stop'

function Write-Step($msg) { Write-Host "[AITS-DEV] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }

# Przejście do katalogu repo (folder skryptu -> root)
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Step "Ustawiam zmienne środowiskowe (Development, ConnectionStrings, JWT)"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Default = "Server=$SqlServerInstance;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
$env:Jwt__Issuer = 'AITS-Auth'
$env:Jwt__Audience = 'AITS-Clients'
$env:Jwt__Key = $JwtKey

Write-Step "Przywracanie i budowa .NET"
dotnet restore AITS.sln
dotnet build AITS.sln -c Debug --no-restore
if (-not $NoTests) {
    Write-Step "Testy .NET"
    dotnet test AITS.sln -c Debug --no-build --collect:"XPlat Code Coverage"
}

Write-Step "Instalacja zależności klienta"
Push-Location client
if (Test-Path package-lock.json) { npm ci } else { npm i }
if (-not $NoTests) {
    Write-Step "Testy klienta (Vitest)"
    npm test -- --run
}
Pop-Location

if (-not $SkipEfUpdate) {
    try {
        Write-Step "Aktualizacja bazy EF (Server=$SqlServerInstance; Database=$DatabaseName)"
        dotnet ef database update -p server/AITS.Api -s server/AITS.Api
        Write-Ok "Migracje zastosowane"
    }
    catch {
        Write-Warn "Nie udało się zastosować migracji EF: $($_.Exception.Message). Kontynuuję uruchamianie kontenerów."
    }
}

Write-Step "Budowa i uruchomienie kontenerów DEV (API:7100, FRONT:7101)"
$composeCmd = "docker compose -f docker-compose.dev.yml up --build"
if ($Detach) { $composeCmd += " -d" }
iex $composeCmd

Write-Ok "Aplikacja DEV uruchomiona."
Write-Host "API:      http://localhost:7100" -ForegroundColor Magenta
Write-Host "Frontend: http://localhost:7101" -ForegroundColor Magenta

Write-Host "Logowanie testowe: admin@aits.local / Admin123!" -ForegroundColor DarkGray





