import { useEffect, useState } from 'react'
import { loadTranslations, type Translations } from '../../i18n'

export default function LoginPage() {
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadTranslations(culture).then(setT)
    // Nie przekierowuj automatycznie - pozwól użytkownikowi się zalogować
    // Automatyczne przekierowanie może powodować pętle jeśli token jest nieprawidłowy
  }, [culture])

  async function login(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Email: email, Password: password })
      })
      if (!res.ok) { 
        setError('Unauthorized')
        return 
      }
      const data = await res.json()
      localStorage.setItem('token', data.token)
      localStorage.setItem('roles', JSON.stringify(data.roles || []))
      console.log('Zalogowano pomyślnie. Role:', data.roles)
      window.location.href = '/dashboard'
    } catch (err) {
      setError('Błąd połączenia z serwerem')
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
      <div className="w-full max-w-md bg-white p-8 rounded shadow">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-semibold">{t['login.title'] ?? 'Logowanie'}</h1>
          <select value={culture} onChange={e=>setCulture(e.target.value as 'pl'|'en')} className="border rounded px-2 py-1">
            <option value="pl">PL</option>
            <option value="en">EN</option>
          </select>
        </div>
        <form className="space-y-4" onSubmit={login}>
          <div>
            <label className="block text-sm mb-1">{t['login.email'] ?? 'E-mail'}</label>
            <input 
              value={email} 
              onChange={e=>setEmail(e.target.value)} 
              type="email" 
              className="w-full border rounded px-3 py-2" 
              autoComplete="email"
              required 
            />
          </div>
          <div>
            <label className="block text-sm mb-1">{t['login.password'] ?? 'Hasło'}</label>
            <input 
              value={password} 
              onChange={e=>setPassword(e.target.value)} 
              type="password" 
              className="w-full border rounded px-3 py-2" 
              autoComplete="current-password"
              required 
            />
          </div>
          {error && <div className="text-red-600 text-sm">{error}</div>}
          <button type="submit" className="w-full bg-blue-600 text-white rounded py-2 hover:bg-blue-700">{t['login.submit'] ?? 'Zaloguj'}</button>
        </form>
      </div>
    </div>
  )
}

