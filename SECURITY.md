# Bezpieczeństwo - Instrukcje Konfiguracji

## ⚠️ WAŻNE - Dane Wrażliwe

Wszystkie wrażliwe dane (API keys, hasła, connection strings) zostały usunięte z repozytorium GitHub.
Musisz skonfigurować je lokalnie lub w środowisku produkcyjnym.

## 🔐 Metody Przechowywania Wrażliwych Danych

### 1. Development - User Secrets (.NET)

Dla środowiska development użyj **User Secrets**:

```powershell
cd server/AITS.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=MSI;Database=AITS-React;Trusted_Connection=true;..."
dotnet user-secrets set "Jwt:Key" "YOUR_SECRET_KEY_MIN_32_CHARS"
dotnet user-secrets set "SMS:ApiToken" "YOUR_SMS_TOKEN"
dotnet user-secrets set "Tpay:ClientSecret" "YOUR_TPAY_SECRET"
dotnet user-secrets set "Email:Username" "your-email@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-password"
dotnet user-secrets set "GoogleOAuth:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "GoogleOAuth:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "AzureAI:ApiKey" "YOUR_AZURE_AI_KEY"
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "YOUR_AZURE_SPEECH_KEY"
```

**Lub** skopiuj przykładowe pliki i wypełnij je danymi:
```powershell
copy server/AITS.Api/appsettings.Example.json server/AITS.Api/appsettings.json
copy server/AITS.Api/appsettings.Development.Example.json server/AITS.Api/appsettings.Development.json
copy server/AITS.Api/google-credentials.example.json server/AITS.Api/google-credentials.json
```

### 2. Production - Azure App Service Configuration

W Azure Portal:
1. Przejdź do **App Service** → **Configuration** → **Application settings**
2. Dodaj wszystkie zmienne środowiskowe:
   - `ConnectionStrings__Default`
   - `Jwt__Key`
   - `SMS__ApiToken`
   - `Tpay__ClientSecret`
   - `Email__Username`
   - `Email__Password`
   - `GoogleOAuth__ClientId`
   - `GoogleOAuth__ClientSecret`
   - `AzureAI__ApiKey`
   - `AzureSpeech__SubscriptionKey`

### 3. Production - Azure Key Vault (Rekomendowane)

Dla większego bezpieczeństwa użyj Azure Key Vault:

1. Utwórz Key Vault w Azure Portal
2. Dodaj wszystkie sekrety do Key Vault
3. W `Program.cs` dodaj integrację z Key Vault:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 4. Docker Compose - Zmienne Środowiskowe

Dla `docker-compose.dev.yml` utwórz plik `.env` (nie commituj go!):

```env
SQL_CONNECTION_STRING=Server=host.docker.internal,1433;Database=AITS-React;User Id=aitsadmin;Password=YOUR_PASSWORD;...
```

## 📋 Pliki do Skonfigurowania

### Lokalnie (Development):
- `server/AITS.Api/appsettings.json` - skopiuj z `appsettings.Example.json`
- `server/AITS.Api/appsettings.Development.json` - skopiuj z `appsettings.Development.Example.json`
- `server/AITS.Api/google-credentials.json` - skopiuj z `google-credentials.example.json`
- `.env` (dla Docker Compose) - utwórz lokalnie

### W Azure (Production):
- Application Settings w App Service
- Lub Azure Key Vault

## 🚨 Co Zrobić Jeśli Sekrety Zostały Ujawnione?

1. **Natychmiast zmień wszystkie hasła i klucze API**
2. **Wygeneruj nowe klucze w Azure Portal**
3. **Wygeneruj nowe Google OAuth credentials**
4. **Zmień hasła do kont email**
5. **Sprawdź logi dostępu w Azure Portal**

## ✅ Pliki Wykluczone z Git

Następujące pliki są w `.gitignore` i nie powinny być commitowane:
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`
- `google-credentials.json`
- `*.env`
- `docker-compose.override.yml`
- `**/Properties/secrets.json`

## 📝 Przykładowe Pliki

W repozytorium znajdziesz przykładowe pliki z sufiksem `.Example.json` lub `.example.json`:
- `appsettings.Example.json`
- `appsettings.Development.Example.json`
- `google-credentials.example.json`

Skopiuj je i wypełnij własnymi danymi.


