# Instrukcje - AITS React

## Zarządzanie kontenerami Docker

### 🔧 Budowanie i uruchamianie kontenerów

#### 1. Przebudowanie i uruchomienie wszystkich kontenerów

```powershell
docker compose -f docker-compose.dev.yml up --build -d
```

Ten polecenie:
- Przebudowuje obrazy Docker dla API i Client
- Uruchamia kontenery w trybie detached (`-d`)
- Automatycznie uruchamia migracje bazy danych

#### 2. Restartowanie kontenerów (bez przebudowy)

```powershell
docker compose -f docker-compose.dev.yml restart
```

Przydatne gdy:
- Wystąpiły błędy podczas działania
- Chcesz przeładować konfigurację
- Aplikacja przestała odpowiadać

#### 3. Restartowanie pojedynczego kontenera

**Restart tylko API:**
```powershell
docker compose -f docker-compose.dev.yml restart api
```

**Restart tylko Client:**
```powershell
docker compose -f docker-compose.dev.yml restart client
```

#### 4. Przebudowanie i restart tylko API

```powershell
docker compose -f docker-compose.dev.yml up --build -d api
```

Przydatne gdy:
- Wprowadziłeś zmiany w kodzie backend (.NET)
- Zmieniłeś konfigurację w `appsettings.json`
- Chcesz zobaczyć zmiany w API bez przebudowy frontendu

#### 5. Przebudowanie i restart tylko Client

```powershell
docker compose -f docker-compose.dev.yml up --build -d client
```

Przydatne gdy:
- Wprowadziłeś zmiany w kodzie frontend (React)
- Zmieniłeś style CSS
- Chcesz zobaczyć zmiany w interfejsie bez przebudowy API

### 📊 Sprawdzanie statusu kontenerów

```powershell
docker compose -f docker-compose.dev.yml ps
```

Wyświetla:
- Nazwę kontenera
- Status (Up/Down)
- Porty (7100 dla API, 7101 dla Client)
- Czas uruchomienia

### 📝 Przeglądanie logów

**Logi wszystkich kontenerów:**
```powershell
docker compose -f docker-compose.dev.yml logs
```

**Logi tylko API:**
```powershell
docker compose -f docker-compose.dev.yml logs api
```

**Logi tylko Client:**
```powershell
docker compose -f docker-compose.dev.yml logs client
```

**Ostatnie 50 linii logów API:**
```powershell
docker compose -f docker-compose.dev.yml logs api --tail 50
```

**Śledzenie logów w czasie rzeczywistym:**
```powershell
docker compose -f docker-compose.dev.yml logs -f
```

### 🛑 Zatrzymywanie kontenerów

**Zatrzymanie wszystkich kontenerów:**
```powershell
docker compose -f docker-compose.dev.yml stop
```

**Zatrzymanie pojedynczego kontenera:**
```powershell
docker compose -f docker-compose.dev.yml stop api
docker compose -f docker-compose.dev.yml stop client
```

### 🗑️ Usuwanie kontenerów

**Zatrzymanie i usunięcie kontenerów (bez usuwania obrazów):**
```powershell
docker compose -f docker-compose.dev.yml down
```

**Zatrzymanie i usunięcie kontenerów + wolumenów:**
```powershell
docker compose -f docker-compose.dev.yml down -v
```

### 🔍 Diagnostyka problemów

#### Problem: Błąd migracji EF Core - "PendingModelChangesWarning"

**Objawy:**
- W logach API widzisz: `PendingModelChangesWarning` lub `has pending changes`
- Aplikacja uruchamia się, ale migracje nie są aplikowane

**Rozwiązanie:**
1. Ostrzeżenie zostało wyłączone w `Program.cs` - aplikacja powinna działać normalnie
2. Jeśli nadal występuje problem, utwórz nową migrację:
```powershell
cd server/AITS.Api
dotnet ef migrations add FixPendingChanges
dotnet ef database update
```

3. Następnie przebuduj kontener API:
```powershell
docker compose -f docker-compose.dev.yml up --build -d api
```

#### Problem: API nie łączy się z bazą danych

**Objawy:**
- W logach API widzisz: `Could not open a connection to SQL Server`
- `The server was not found or was not accessible`
- `A network-related or instance-specific error occurred`

**Diagnostyka:**

1. **Sprawdź logi API:**
```powershell
docker compose -f docker-compose.dev.yml logs api --tail 50
```

2. **Sprawdź połączenie z kontenera do SQL Server:**
```powershell
docker exec aits-react-api ping -c 3 10.5.240.54
```

3. **Zweryfikuj dostępność SQL Server z hosta:**
```powershell
Test-NetConnection -ComputerName 10.5.240.54 -Port 1433
```

4. **Sprawdź, czy SQL Server działa lokalnie:**
```powershell
# Jeśli SQL Server działa na lokalnym hoście jako MSI
sqlcmd -S MSI -E -Q "SELECT @@VERSION"
```

**Rozwiązanie:**

**Opcja A: SQL Server na lokalnym hoście (MSI)**
1. Sprawdź, czy SQL Server działa:
```powershell
Get-Service MSSQLSERVER
```

2. Jeśli działa, sprawdź swoje IP:
```powershell
ipconfig | Select-String "IPv4"
```

3. Zaktualizuj `docker-compose.dev.yml` z prawidłowym IP:
```yaml
environment:
  ConnectionStrings__Default: "Server=TU_WPISZ_IP,1433;Database=AITS-React;User Id=aitsadmin;Password=Aithentica12345!;TrustServerCertificate=True;"
```

**Opcja B: Użyj host.docker.internal (Docker Desktop)**
```yaml
environment:
  ConnectionStrings__Default: "Server=host.docker.internal,1433;Database=AITS-React;User Id=aitsadmin;Password=Aithentica12345!;TrustServerCertificate=True;"
```

**Opcja C: Sprawdź konfigurację SQL Server**
1. **Włącz SQL Server Authentication:**
   - Otwórz SQL Server Management Studio (SSMS)
   - Kliknij prawym na serwer → Properties → Security
   - Ustaw: `SQL Server and Windows Authentication mode`

2. **Włącz TCP/IP:**
   - Otwórz SQL Server Configuration Manager
   - SQL Server Network Configuration → Protocols for MSSQLSERVER
   - Włącz TCP/IP
   - Restart SQL Server

3. **Sprawdź port SQL Server (domyślnie 1433):**
```powershell
Get-NetTCPConnection -LocalPort 1433 -ErrorAction SilentlyContinue
```

4. **Sprawdź firewall:**
```powershell
# Zezwól na połączenia przez port 1433
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
```

**Opcja D: Użyj LocalDB (tylko Windows)**
```yaml
environment:
  ConnectionStrings__Default: "Server=host.docker.internal\\MSSQLLocalDB;Database=AITS-React;Trusted_Connection=true;TrustServerCertificate=True;"
```

**Aktualny connection string w `docker-compose.dev.yml`:**
```yaml
environment:
  ConnectionStrings__Default: "Server=192.168.50.228,1433;Database=AITS-React;User Id=aitsadmin;Password=Aithentica12345!;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

**Uwaga:** Jeśli zmienisz IP lub connection string, zrestartuj kontener:
```powershell
docker compose -f docker-compose.dev.yml restart api
```

#### Problem: Frontend nie łączy się z API

1. Sprawdź, czy API działa:
```powershell
Invoke-WebRequest -Uri http://localhost:7100/swagger
```

2. Sprawdź konfigurację proxy w `client/nginx.conf`:
```nginx
location /api/ {
    proxy_pass http://api:8080/api/;
}
```

3. Zrestartuj oba kontenery:
```powershell
docker compose -f docker-compose.dev.yml restart
```

#### Problem: Kontener nie uruchamia się po zmianach w kodzie

1. Przebuduj kontener (nie tylko restart):
```powershell
docker compose -f docker-compose.dev.yml up --build -d
```

2. Wyczyść cache Dockera (ostrożnie - usuwa wszystkie obrazy):
```powershell
docker system prune -a
```

### 🌐 Dostęp do aplikacji

- **Frontend**: http://localhost:7101
- **API Swagger**: http://localhost:7100/swagger
- **API Base URL**: http://localhost:7100/api

### ⚙️ Konfiguracja

#### Porty
- API: `7100` → `8080` (w kontenerze)
- Client: `7101` → `80` (w kontenerze)

#### Pliki konfiguracyjne
- `docker-compose.dev.yml` - konfiguracja środowiska deweloperskiego
- `server/AITS.Api/appsettings.json` - konfiguracja API
- `server/AITS.Api/appsettings.Development.json` - konfiguracja DEV
- `client/nginx.conf` - konfiguracja serwera Nginx

### 📦 Skrypt PowerShell do uruchomienia

Możesz również użyć gotowego skryptu:

```powershell
.\scripts\start-dev.ps1
```

Skrypt automatycznie:
- Przebudowuje obrazy
- Uruchamia kontenery
- Pokazuje logi

### 🔄 Typowy workflow deweloperski

1. **Po zmianach w kodzie:**
   ```powershell
   docker compose -f docker-compose.dev.yml up --build -d
   ```

2. **Sprawdzenie logów:**
   ```powershell
   docker compose -f docker-compose.dev.yml logs --tail 50
   ```

3. **Testowanie w przeglądarce:**
   - Otwórz http://localhost:7101
   - Sprawdź http://localhost:7100/swagger

4. **W razie problemów - restart:**
   ```powershell
   docker compose -f docker-compose.dev.yml restart
   ```

### 🔐 Autoryzacja JWT - Jak używać API

Większość endpointów API wymaga autoryzacji JWT. Oto jak uzyskać i użyć tokena:

#### 1. Logowanie i uzyskanie tokena

**Endpoint:** `POST /api/auth/login`

**Przykład cURL:**
```bash
curl -X 'POST' \
  'http://localhost:7100/api/auth/login' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "email": "admin@aits.local",
  "password": "Admin123!"
}'
```

**Odpowiedź:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "roles": ["Administrator"]
}
```

#### 2. Użycie tokena w żądaniach

**Przykład - wywołanie `/api/Weather`:**
```bash
curl -X 'GET' \
  'http://localhost:7100/api/Weather' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

#### 3. Dostępne konta testowe

Po uruchomieniu aplikacji dostępne są następujące konta (jeśli zostały utworzone przez seeding):

| Email | Hasło | Rola |
|-------|-------|------|
| `admin@aits.local` | `Admin123!` | Administrator |
| `pacjent@aits.local` | `Pacjent123!` | Pacjent |
| `terapeuta@aits.local` | `Terapeuta123!` | Terapeuta |
| `terapeuta.free@aits.local` | `TerapeutaFree123!` | TerapeutaFreeAccess |

#### 4. Utworzenie kont testowych

**Endpoint:** `POST /api/auth/seed-users`

```bash
curl -X 'POST' \
  'http://localhost:7100/api/auth/seed-users' \
  -H 'accept: */*'
```

**Endpoint:** `POST /api/auth/seed-admin` (tylko admin)

```bash
curl -X 'POST' \
  'http://localhost:7100/api/auth/seed-admin' \
  -H 'accept: */*'
```

#### 5. Użycie w Swagger

1. Otwórz http://localhost:7100/swagger
2. Kliknij przycisk **"Authorize"** (🔒) w prawym górnym rogu
3. Wklej token JWT (uzyskany z `/api/auth/login`)
4. Kliknij **"Authorize"**
5. Teraz możesz testować endpointy wymagające autoryzacji

#### 6. Wymagania ról dla endpointów

- **`/api/Weather`**: Administrator, Terapeuta
- **`/api/patients`**: Terapeuta, Administrator
- **`/api/sessions`**: Terapeuta, TerapeutaFreeAccess, Administrator
- **`/api/auth/login`**: Publiczny (bez autoryzacji)
- **`/api/i18n/{culture}`**: Publiczny (bez autoryzacji)

### ⚠️ Ważne uwagi

- **Migracje EF Core**: Automatycznie uruchamiają się przy starcie API
- **Cache przeglądarki**: W razie problemów z wyświetlaniem zmian użyj `Ctrl+Shift+R` (hard refresh)
- **Logi SMS**: Sprawdź logi API, aby zobaczyć szczegóły odpowiedzi z SMSAPI
- **Baza danych**: Wymaga lokalnego SQL Server dostępnego pod adresem `192.168.50.228`
- **Token JWT**: Ważny przez 12 godzin

### 📚 Dodatkowe zasoby

- Dokumentacja Docker Compose: https://docs.docker.com/compose/
- ASP.NET Core Docker: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/
- React + Docker: https://mherman.org/blog/dockerizing-a-react-app/

