# Wzorzec Stylowania Formularzy dla Ekranów 14-16" FullHD

## Zasady projektowania formularzy

### 1. Wspólne klasy CSS

Zawsze używaj następujących klas dla formularzy:

**Kontener główny:**
```tsx
<main className="max-w-4xl mx-auto py-8 px-6">
  <h1>Tytuł formularza</h1>
  <div className="bg-white p-8 rounded-lg shadow-lg space-y-8">
```

**Sekcje formularza:**
```tsx
<div className="form-section">
  <h2>Tytuł sekcji</h2>
  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
```

**Pola formularza:**
```tsx
<div className="form-group">
  <label>Nazwa pola</label>
  <input className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
</div>
```

**Przyciski:**
```tsx
<button className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
  Zapisz
</button>
```

### 2. Rozmiary i odstępy

- **Czcionka body**: 15px (automatycznie przez CSS)
- **Czcionka inputów**: 16px (automatycznie przez CSS)
- **Padding inputów**: 12px 16px
- **Minimalna wysokość inputów**: 48px
- **Gap między polami**: 24px (gap-6)
- **Padding kontenera**: 32px (p-8)
- **Padding sekcji**: 32px (space-y-8)

### 3. Kolory i kontrast

- **Tło**: bg-gray-50
- **Karty**: bg-white z shadow-lg
- **Border inputów**: border-2 border-gray-300
- **Focus**: border-blue-500 + ring-2 ring-blue-200
- **Tekst główny**: text-gray-800
- **Tekst pomocniczy**: text-gray-600

### 4. Typografia

- **H1**: 28px, font-bold
- **H2**: 20px, font-semibold
- **Label**: 15px, font-semibold
- **Body**: 15px, line-height 1.6

### 5. Responsive

- Desktop (md+): max-w-4xl, grid md:grid-cols-2
- Mobile: max-w-full, grid grid-cols-1

## Przykład kompletnego formularza

```tsx
function MyForm() {
  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar {...props} />
      <main className="max-w-4xl mx-auto py-8 px-6">
        <h1>Tytuł formularza</h1>
        <div className="bg-white p-8 rounded-lg shadow-lg space-y-8">
          <div className="form-section">
            <h2>Pierwsza sekcja</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="form-group">
                <label>Pole 1</label>
                <input className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>Pole 2</label>
                <input className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
            </div>
          </div>
          
          <div className="flex gap-6 pt-4">
            <button className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
              Zapisz
            </button>
            <button className="bg-gray-600 text-white px-8 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">
              Anuluj
            </button>
          </div>
        </div>
      </main>
    </div>
  )
}
```

## Wzorzec jest już zaimplementowany w:

- ✅ PatientForm (pacjent)
- ✅ SessionForm (sesja)
- ✅ SessionDetails (szczegóły sesji)

**Każdy nowy formularz powinien używać tego samego wzorca.**




