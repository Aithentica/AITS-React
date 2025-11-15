# ✅ ROZWIĄZANIE: Azure Speech Services - Diaryzacja (Conversation Transcription)

## 🔴 PROBLEM

```
ErrorCode=ServiceError: WebSocket upgrade failed: Internal service error (404)
Error Details: Failed with HTTP 404 Resource Not Found
```

### Dlaczego testowy kod działa, a nasza aplikacja nie?

| Aspekt | Testowy kod ✅ | Nasza aplikacja ❌ |
|--------|----------------|-------------------|
| **Recognizer** | `SpeechRecognizer` | `ConversationTranscriber` |
| **Diaryzacja** | NIE (brak rozpoznawania mówców) | TAK (rozpoznawanie mówców) |
| **Resource** | Multi-service Cognitive Services | **WYMAGA dedykowanego Speech Services** |
| **Klucz** | Działa | **NIE DZIAŁA** (404 błąd) |

**Wniosek:** `ConversationTranscriber` (diaryzacja) **WYMAGA dedykowanego Speech Services resource**, nie może używać multi-service Cognitive Services!

---

## ✅ ROZWIĄZANIE

### **Opcja 1: Utwórz dedykowany Speech Services resource (ZALECANE)**

#### **Krok 1: Azure Portal - Utwórz Speech Services**

1. Przejdź do: https://portal.azure.com
2. Kliknij: **"Create a resource"**
3. Szukaj: **"Speech Services"** ← WAŻNE: NIE "Cognitive Services"!
4. Wybierz: **"Speech Services"** (ikona z mikrofonem)
5. Kliknij: **"Create"**

#### **Krok 2: Konfiguracja**

```
Subscription: Azure for startups
Resource Group: aits-azuere-ai-services-rg (lub utwórz nowy)
Region: West Europe
Name: aits-speech-diarization
Pricing Tier: Standard S0 (dla diaryzacji w produkcji)
             lub Free F0 (do testów, może mieć ograniczenia)
```

6. Kliknij: **"Review + Create"** → **"Create"**
7. Poczekaj na utworzenie (1-2 minuty)

#### **Krok 3: Pobierz klucz**

1. Po utworzeniu przejdź do resource
2. W menu po lewej kliknij: **"Keys and Endpoint"**
3. Skopiuj:
   - **KEY 1**: `[TWÓJ_NOWY_KLUCZ]`
   - **Location/Region**: `westeurope`

#### **Krok 4: Zaktualizuj appsettings.Development.json**

```json
"AzureSpeech": {
  "SubscriptionKey": "[WKLEJ_TUTAJ_NOWY_KLUCZ_Z_KROKU_3]",
  "Region": "westeurope",
  "Language": "pl-PL",
  "MaxSpeakerCount": 3
}
```

**WAŻNE:** 
- ❌ **NIE DODAWAJ** pola `Endpoint`
- ✅ Używaj tylko `Region` i `SubscriptionKey`

#### **Krok 5: Przebuduj i uruchom**

```powershell
# W katalogu server
dotnet build AITS.Api/AITS.Api.csproj

# Uruchom API
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project AITS.Api/AITS.Api.csproj
```

W logach powinieneś zobaczyć:
```
info: AITS.Api.Services.AzureSpeechService[0]
      Korzystanie z regionu Azure Speech: westeurope

dbug: AITS.Api.Services.AzureSpeechService[0]
      Sesja Azure Speech rozpoczęta: SessionId=xxx

dbug: AITS.Api.Services.AzureSpeechService[0]
      Otrzymano wynik transkrypcji: Reason=RecognizedSpeech, Text=..., SpeakerId=Guest-1
```

---

### **Opcja 2: Wyłącz diaryzację (NIE ZALECANE)**

Jeśli naprawdę nie potrzebujesz rozpoznawania mówców (diaryzacji), możesz zmienić kod na `SpeechRecognizer`:

**W `AzureSpeechService.cs` zmień:**

```csharp
// BYŁO (z diaryzacją):
using var transcriber = new ConversationTranscriber(speechConfig, audioConfig);

// BĘDZIE (bez diaryzacji):
using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
```

**ALE:** Stracisz rozpoznawanie mówców (SpeakerId będzie zawsze pusty).

---

## 🔍 Weryfikacja rozwiązania

### **Test 1: Sprawdź typ resource w Azure Portal**

1. Przejdź do: https://portal.azure.com
2. Znajdź swój nowy Speech Services resource
3. Sprawdź:
   - ✅ **Kind:** powinno być **"SpeechServices"**
   - ❌ **NIE:** "CognitiveServices"

### **Test 2: Przetestuj endpoint przez cURL**

```powershell
$key = "[TWÓJ_NOWY_KLUCZ]"
$region = "westeurope"

# Test autoryzacji
curl -v -X POST "https://$region.stt.speech.microsoft.com/cognitiveservices/v1" `
  -H "Ocp-Apim-Subscription-Key: $key" `
  -H "Content-Type: application/ssml+xml" `
  -d "<speak version='1.0' xml:lang='pl-PL'><voice name='pl-PL-ZofiaNeural'>Test</voice></speak>"
```

**Oczekiwany wynik:**
- ✅ Status: 200 OK
- ❌ Status: 401 → Zły klucz
- ❌ Status: 404 → Zły region lub typ resource

### **Test 3: Uruchom aplikację i testuj diaryzację**

1. Uruchom API i klienta
2. Rozpocznij nagrywanie na żywo
3. Powiedz coś do mikrofonu
4. Sprawdź logi:

```
dbug: AITS.Api.Services.AzureSpeechService[0]
      Otrzymano wynik transkrypcji: Reason=RecognizedSpeech, Text=Dzień dobry, SpeakerId=Guest-1
```

**SpeakerId powinien być:** `Guest-1`, `Guest-2`, itp. (nie pusty!)

---

## 📋 Porównanie: Cognitive Services vs Speech Services

| Cecha | Multi-service Cognitive Services | Dedykowany Speech Services |
|-------|----------------------------------|----------------------------|
| **Endpoint** | `https://westeurope.api.cognitive.microsoft.com/` | Region-based (automatyczny) |
| **SpeechRecognizer** | ✅ Działa | ✅ Działa |
| **ConversationTranscriber** | ❌ **NIE DZIAŁA (404)** | ✅ **DZIAŁA** |
| **Diaryzacja** | ❌ Nie wspierana | ✅ Pełne wsparcie |
| **Pricing** | Multi-service bundle | Dedykowany dla Speech |

---

## 🎯 Podsumowanie

### ✅ **Co zrobić:**
1. Utwórz dedykowany **Speech Services** resource w Azure Portal
2. Skopiuj nowy klucz
3. Zaktualizuj `appsettings.Development.json` (tylko `SubscriptionKey` i `Region`, BEZ `Endpoint`)
4. Przebuduj i uruchom aplikację

### ❌ **Czego NIE robić:**
- ❌ Nie używaj multi-service Cognitive Services dla diaryzacji
- ❌ Nie dodawaj pola `Endpoint` do konfiguracji (używaj tylko `Region`)
- ❌ Nie używaj endpoint `https://westeurope.api.cognitive.microsoft.com/` dla ConversationTranscriber

---

## 📞 Jeśli nadal nie działa:

1. Sprawdź logi API (szukaj `ErrorCode=` i `ErrorDetails=`)
2. Sprawdź typ resource w Azure Portal (powinien być "SpeechServices", nie "CognitiveServices")
3. Sprawdź czy region to `westeurope` (zgodny z Location w Azure Portal)
4. Upewnij się, że klucz jest z **dedykowanego Speech Services**, nie z Cognitive Services

**Z dedykowanym Speech Services resource diaryzacja będzie działać! 🎉**


