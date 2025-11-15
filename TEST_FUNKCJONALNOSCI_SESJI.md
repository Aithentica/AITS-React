# Test funkcjonalności: Cele sesji i podpowiedzi

## Sprawdzenie czy wszystko działa

### 1. Upewnij się, że migracja została zastosowana
```powershell
cd server/AITS.Api
dotnet ef database update -p AITS.Api -s AITS.Api
```

### 2. Zrestartuj aplikację
Jeśli aplikacja działa w Dockerze:
```powershell
docker compose -f docker-compose.dev.yml restart
```

Lub uruchom ponownie:
```powershell
.\scripts\start-dev.ps1
```

### 3. Sprawdź czy endpoint działa
Otwórz w przeglądarce (po zalogowaniu):
- `http://localhost:7100/api/sessions/types` - powinien zwrócić listę typów sesji z polami `goals`, `tips`, `questions`

### 4. Sprawdź w formularzu sesji
1. Zaloguj się jako terapeuta
2. Przejdź do: **Nowa sesja** (`/sessions/new`)
3. Powinieneś zobaczyć:
   - Pole wyboru "Typ sesji" (po polach daty i czasu)
   - Po wyborze typu sesji powinna pojawić się niebieska ramka z:
     - Nazwą typu sesji
     - Opisem (jeśli istnieje)
     - **Celami sesji** (jeśli istnieją)
     - **Podpowiedziami** (jeśli istnieją)
     - **Pytaniami do rozważenia** (jeśli istnieją)

### 5. Utwórz typ sesji systemowy (jako administrator)
1. Przejdź do `/session-types`
2. Utwórz nowy typ sesji z:
   - Nazwą: np. "Sesja poznawczo-behawioralna"
   - Opisem: np. "Standardowa sesja CBT"
   - **Celami**: np. "Identyfikacja myśli automatycznych\nPraca nad schematami poznawczymi"
   - **Podpowiedziami**: Dodaj kilka podpowiedzi
   - **Pytaniami**: Dodaj kilka pytań
   - Zaznacz **IsSystem = true**

### 6. Sprawdź czy terapeuta widzi typy
1. Zaloguj się jako terapeuta
2. Przejdź do `/sessions/new`
3. W polu "Typ sesji" powinny być widoczne typy systemowe

### 7. Utwórz wersję użytkownika (jako terapeuta)
Możesz użyć API:
```bash
POST /api/sessiontypes/user-version
{
  "baseSessionTypeId": 1,
  "name": "Moja wersja CBT",
  "goals": "Moje własne cele",
  "tips": [...],
  "questions": [...]
}
```

## Rozwiązywanie problemów

### Problem: Nie widzę pola "Typ sesji" w formularzu
**Rozwiązanie:**
- Sprawdź konsolę przeglądarki (F12) - czy są błędy JavaScript?
- Sprawdź czy endpoint `/api/sessions/types` zwraca dane
- Odśwież stronę (Ctrl+F5)

### Problem: Endpoint zwraca błąd 401/403
**Rozwiązanie:**
- Upewnij się, że jesteś zalogowany
- Sprawdź czy token jest ważny
- Wyloguj się i zaloguj ponownie

### Problem: Nie widzę celów i podpowiedzi po wyborze typu
**Rozwiązanie:**
- Sprawdź czy typ sesji ma wypełnione pola `goals`, `tips`, `questions`
- Sprawdź konsolę przeglądarki - czy są błędy?
- Sprawdź czy `selectedSessionType` jest poprawnie ustawione

### Problem: Migracja nie działa
**Rozwiązanie:**
```powershell
# Sprawdź status migracji
dotnet ef migrations list -p server/AITS.Api -s server/AITS.Api

# Jeśli migracja nie została zastosowana, zastosuj ją ręcznie
dotnet ef database update -p server/AITS.Api -s server/AITS.Api
```

## Struktura danych

### Typ sesji (SessionType)
```json
{
  "id": 1,
  "name": "Sesja CBT",
  "description": "Opis",
  "goals": "Cele sesji...",
  "isSystem": true,
  "baseSessionTypeId": null,
  "tips": [
    {
      "id": 1,
      "content": "Podpowiedź 1",
      "displayOrder": 0
    }
  ],
  "questions": [
    {
      "id": 1,
      "content": "Pytanie 1",
      "displayOrder": 0
    }
  ]
}
```


