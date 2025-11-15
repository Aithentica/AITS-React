# Skrypt do generowania bezpiecznego klucza JWT
# Uruchom: .\scripts\generate-jwt-key.ps1

Write-Host "=== Generator Bezpiecznego Klucza JWT ===" -ForegroundColor Cyan
Write-Host ""

$projectPath = "server\AITS.Api"

if (-not (Test-Path $projectPath)) {
    Write-Host "BLAD: Nie znaleziono projektu w $projectPath" -ForegroundColor Red
    exit 1
}

# Generuj losowy klucz o długości 64 znaków (256 bitów)
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

Write-Host "Wygenerowany bezpieczny klucz JWT:" -ForegroundColor Green
Write-Host $jwtKey -ForegroundColor Yellow
Write-Host ""
Write-Host "Dlugosc klucza: $($jwtKey.Length) znakow" -ForegroundColor Gray
Write-Host ""

# Przejdź do katalogu projektu i ustaw klucz
Set-Location $projectPath

Write-Host "Inicjalizacja User Secrets (jeśli potrzebne)..." -ForegroundColor Yellow
dotnet user-secrets init 2>$null

Write-Host "Ustawianie klucza JWT w User Secrets..." -ForegroundColor Yellow
dotnet user-secrets set "Jwt:Key" $jwtKey

Write-Host ""
Write-Host "=== Sukces! Klucz JWT zostal ustawiony ===" -ForegroundColor Green
Write-Host ""
Write-Host "Aby zobaczyc wszystkie sekrety:" -ForegroundColor Cyan
Write-Host "  dotnet user-secrets list" -ForegroundColor Gray

Set-Location ..\..


