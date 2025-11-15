# Poprawki Azure Speech Services - Transkrypcja w czasie rzeczywistym

## 🎯 Problem
```
System.Threading.Tasks.TaskCanceledException: A task was canceled.
   at AITS.Api.Services.AzureSpeechService.TranscribeWaveFileAsync(...):line 212
```

Azure Speech Services anulował transkrypcję bez szczegółowych informacji o błędzie.

---

## ✅ Co zostało naprawione

### 1. **Dodano szczegółowe logowanie w `AzureSpeechService.cs`:**

- ✅ Log konfiguracji (endpoint, region, język, MaxSpeakerCount)
- ✅ Log rozpoczęcia transkrypcji
- ✅ Log każdego otrzymanego wyniku (Reason, Text, SpeakerId)
- ✅ **KLUCZOWE:** Szczegółowy log błędów Azure Speech z ErrorCode i ErrorDetails
- ✅ Log startu/stopu sesji Azure Speech
- ✅ Pomocne komunikaty dla typowych błędów (ConnectionFailure, ServiceError)

### 2. **Dodano szczegółowe logowanie w `RealtimeTranscriptionSession.cs`:**

- ✅ Log rozmiaru plików PCM i WAV
- ✅ Log konwersji PCM → WAV
- ✅ Log rozpoczęcia/zakończenia transkrypcji
- ✅ Log liczby segmentów i znaków w wynikach
- ✅ Ostrzeżenia dla pustych plików audio

### 3. **Ulepszona obsługa błędów:**

- ✅ Szczegółowe komunikaty dla ConnectionFailure
- ✅ Szczegółowe komunikaty dla ServiceError (wskazuje na problem z typem resource)
- ✅ Dodano event handler `SessionStarted` do monitorowania połączenia

### 4. **Zaktualizowano konfigurację logowania:**

W `appsettings.Development.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "AITS.Api.Services.AzureSpeechService": "Debug",
    "AITS.Api.Services.Realtime": "Debug",
    "AITS.Api.Hubs.TranscriptionHub": "Debug"
  }
}
```

---

## 📋 Co teraz zrobić - INSTRUKCJE KROK PO KROKU

### **Krok 1: Przebuduj projekt**
```powershell
cd server
dotnet build AITS.Api/AITS.Api.csproj
```
✅ **GOTOWE** - kompilacja zakończona sukcesem

### **Krok 2: Uruchom API z pełnym logowaniem**

W PowerShell w katalogu `server`:
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project AITS.Api/AITS.Api.csproj
```

### **Krok 3: W NOWYM oknie PowerShell uruchom klienta**

W katalogu `client`:
```powershell
npm run dev
```

### **Krok 4: Spróbuj nagrać sesję w czasie rzeczywistym**

1. Zaloguj się do aplikacji
2. Przejdź do sesji
3. Kliknij "Rozpocznij nagrywanie na żywo"
4. Zezwól na dostęp do mikrofonu
5. Powiedz coś do mikrofonu

### **Krok 5: OBSERWUJ LOGI i SKOPIUJ BŁĘDY**

W logach API szukaj następujących linii:

#### ✅ **POPRAWNE LOGI (jeśli wszystko działa):**
```
info: AITS.Api.Services.AzureSpeechService[0]
      Tworzenie konfiguracji Azure Speech - Endpoint=null, Region=westeurope, Language=pl-PL, MaxSpeakerCount=3
      
info: AITS.Api.Services.AzureSpeechService[0]
      Korzystanie z regionu Azure Speech: westeurope
      
dbug: AITS.Api.Services.AzureSpeechService[0]
      Rozpoczynam transkrypcję pliku WAV: C:\Users\...\aits-realtime-xxx.wav
      
dbug: AITS.Api.Services.AzureSpeechService[0]
      Sesja Azure Speech rozpoczęta: SessionId=xxx
      
dbug: AITS.Api.Services.AzureSpeechService[0]
      Otrzymano wynik transkrypcji: Reason=RecognizedSpeech, Text=Dzień dobry, SpeakerId=Guest-1
```

#### ❌ **BŁĘDNE LOGI (które musisz mi pokazać):**
```
fail: AITS.Api.Services.AzureSpeechService[0]
      Azure Speech anulowało transkrypcję - Reason=Error, ErrorCode=???, ErrorDetails=???
```

**SKOPIUJ CAŁĄ SEKCJĘ Z BŁĘDEM I PRZEŚLIJ MI!**

---

## 🔍 Najczęstsze błędy i ich znaczenie

### **Błąd 1: ConnectionFailure**
```
ErrorCode=ConnectionFailure
```
**Przyczyna:** Niepoprawny klucz, region lub problem z siecią.

**Rozwiązanie:**
1. Sprawdź czy klucz w `appsettings.Development.json` jest poprawny
2. Sprawdź czy region to `westeurope` (zgodny z Azure Portal)
3. Sprawdź firewall/proxy

---

### **Błąd 2: ServiceError**
```
ErrorCode=ServiceError, ErrorDetails=Internal service error (404)
```
**Przyczyna:** Używasz multi-service "Cognitive Services" zamiast dedykowanego "Speech Services".

**Rozwiązanie:**
1. W Azure Portal utwórz nowy resource typu **"Speech Services"** (nie "Cognitive Services")
2. Skopiuj nowy klucz do `appsettings.Development.json`
3. Usuń pole `Endpoint` z konfiguracji (jeśli istnieje)

---

### **Błąd 3: AuthenticationFailure**
```
ErrorCode=AuthenticationFailure, ErrorDetails=Unauthorized (401)
```
**Przyczyna:** Niepoprawny klucz subskrypcji.

**Rozwiązanie:**
1. W Azure Portal → Twój Speech Services resource → Keys and Endpoint
2. Skopiuj KEY 1
3. Wklej do `appsettings.Development.json` → `AzureSpeech.SubscriptionKey`

---

### **Błąd 4: InvalidFormat**
```
ErrorCode=BadRequest, ErrorDetails=Invalid audio format
```
**Przyczyna:** Problem z formatem audio (choć nasz kod to obsługuje).

**Rozwiązanie:**
Sprawdź logi czy konwersja PCM → WAV się udała:
```
dbug: AITS.Api.Services.Realtime[0]
      Utworzono plik WAV o rozmiarze XXX bajtów
```

---

## 🎯 Co sprawdzić w Azure Portal

1. **Typ resource:**
   - ✅ POWINIEN BYĆ: "Speech Services"
   - ❌ NIE POWINIEN BYĆ: "Cognitive Services" (multi-service)

2. **Region:**
   - ✅ POWINIEN BYĆ: "West Europe" (zgodny z `westeurope` w konfiguracji)

3. **Pricing Tier:**
   - Free (F0): Może mieć ograniczenia w diaryzacji
   - Standard (S0): Pełna funkcjonalność

4. **Klucz:**
   - Skopiuj KEY 1 z "Keys and Endpoint"

---

## 📞 Następne kroki

1. ✅ **Uruchom aplikację** (Krok 1-3 powyżej)
2. 🔍 **Spróbuj nagrać** (Krok 4)
3. 📋 **SKOPIUJ LOGI** z błędami (Krok 5)
4. 📨 **PRZEŚLIJ MI LOGI** - szczególnie linie zawierające:
   - `Azure Speech anulowało transkrypcję`
   - `ErrorCode=`
   - `ErrorDetails=`
   - `Reason=`

**Z dokładnymi logami błędów będę mógł natychmiast wskazać rozwiązanie!** 🎯


