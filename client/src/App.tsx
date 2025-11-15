import { useEffect, useState } from 'react'
import React from 'react'
import { BrowserRouter, Routes, Route, Navigate, useNavigate, useParams } from 'react-router-dom'
import './index.css'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js'
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend
)
import { loadTranslations, type Translations } from './i18n'
import GoogleCalendarIntegrationForm from './features/google/GoogleCalendarIntegration'
import {
  ensureFullHour,
  formatDateTimeWithZone,
  formatDateWithZone,
  formatForDateTimeLocalInput,
  formatTimeWithZone
} from './features/sessions/dateTimeUtils'
import { getWeekCalendarLabels } from './features/sessions/weekCalendarLabels'
import UsersAdmin from './features/admin/UsersAdmin'
import TherapistsAdmin from './features/admin/TherapistsAdmin'
import NavBar from './components/NavBar'
import ActivityTracker from './components/ActivityTracker'
import ActivityLogAdmin from './features/admin/ActivityLogAdmin'
import SessionTypeManagement from './features/session-types/SessionTypeManagement'
import { TherapistProfileManagement } from './features/therapist/TherapistProfileManagement'
import { TherapistDocumentsManagement } from './features/therapist/TherapistDocumentsManagement'
import SessionDetails from './features/sessions/SessionDetails'
import PatientForm from './features/patients/PatientForm'
import Dashboard from './features/dashboard/Dashboard'

const Roles = {
  Administrator: 'Administrator',
  Terapeuta: 'Terapeuta',
  Pacjent: 'Pacjent'
} as const

// Komponent ochrony routingu - sprawdza czy użytkownik ma odpowiednią rolę
function ProtectedRoute({ children, allowedRoles }: { children: React.ReactNode, allowedRoles: string[] }) {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const storedRoles = localStorage.getItem('roles')
    if (!storedRoles) {
      navigate('/login')
      return
    }
    try {
      const parsed = JSON.parse(storedRoles)
      const userRoles = Array.isArray(parsed) ? parsed : []
      
      // Sprawdź czy użytkownik ma jedną z dozwolonych ról
      const hasAccess = allowedRoles.some(role => userRoles.includes(role))
      
      if (!hasAccess) {
        // Pacjent próbuje dostać się do niedozwolonej strony - przekieruj do dashboardu
        if (userRoles.includes(Roles.Pacjent)) {
          navigate('/dashboard')
        } else {
          navigate('/login')
        }
      }
    } catch {
      navigate('/login')
    } finally {
      setLoading(false)
    }
  }, [allowedRoles, navigate])

  if (loading) {
    return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>
  }

  const storedRoles = localStorage.getItem('roles')
  if (!storedRoles) {
    return null
  }

  try {
    const parsed = JSON.parse(storedRoles)
    const userRoles = Array.isArray(parsed) ? parsed : []
    const hasAccess = allowedRoles.some(role => userRoles.includes(role))
    
    if (!hasAccess) {
      return null
    }
  } catch {
    return null
  }

  return <>{children}</>
}

function LoginPage() {
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

interface Session {
  id: number
  patient: { firstName: string; lastName: string; email: string }
  startDateTime: string
  endDateTime: string
  statusId: number
  price: number
  googleMeetLink?: string
}

// Interfejsy przeniesione do features/patients/PatientForm.tsx
// Dashboard jest teraz importowany z features/dashboard/Dashboard.tsx

// Usunięto lokalną funkcję Dashboard() - używamy importowanego komponentu z features/dashboard/Dashboard.tsx

// Komponent nawigacji wspólny dla wszystkich stron
// Lista sesji
function SessionsList() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [sessions, setSessions] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState({ statusId: '', patientId: '', fromDate: '', toDate: '' })

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
    loadSessions()
  }, [culture, navigate])

  async function loadSessions() {
    try {
      const token = localStorage.getItem('token')
      const params = new URLSearchParams({ page: '1', pageSize: '100' })
      if (filters.statusId) params.append('statusId', filters.statusId)
      if (filters.fromDate) params.append('fromDate', filters.fromDate)
      if (filters.toDate) params.append('toDate', filters.toDate)
      if (filters.patientId) params.append('patientId', filters.patientId)
      
      const headers: HeadersInit = { 
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
      
      const res = await fetch(`/api/sessions?${params.toString()}`, { headers })
      if (!res.ok) {
        if (res.status === 401 || res.status === 403) {
          navigate('/login')
          return
        }
        console.error('Error loading sessions:', res.status)
        setLoading(false)
        return
      }
      
      const data = await res.json()
      setSessions(Array.isArray(data.sessions) ? data.sessions : [])
    } catch (err) {
      console.error('Error loading sessions:', err)
    } finally {
      setLoading(false)
    }
  }

  function formatTime(dateStr: string) {
    return formatTimeWithZone(dateStr, culture)
  }

  function getStatusName(statusId: number) {
    const statusNames: Record<number, string> = { 1: 'Zaplanowana', 2: 'Potwierdzona', 3: 'Zakończona', 4: 'Anulowana' }
    return statusNames[statusId] || 'Nieznany'
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={() => { localStorage.removeItem('token'); localStorage.removeItem('roles'); navigate('/login') }} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="space-y-6">
          <div className="flex justify-between items-center">
            <h1 className="text-3xl font-bold text-gray-900">{t['sessions.title'] ?? 'Sesje'}</h1>
            <button
              onClick={() => navigate('/sessions/new')}
              className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold"
            >
              {t['sessions.new'] ?? 'Nowa sesja'}
            </button>
          </div>

          {/* Filtry */}
          <div className="bg-white rounded-lg shadow-lg p-6">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">{t['sessions.filter.status'] ?? 'Status'}</label>
                <select
                  value={filters.statusId}
                  onChange={(e) => setFilters({ ...filters, statusId: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
                >
                  <option value="">{t['sessions.filter.all'] ?? 'Wszystkie'}</option>
                  <option value="1">{t['sessions.status.planned'] ?? 'Zaplanowana'}</option>
                  <option value="2">{t['sessions.status.confirmed'] ?? 'Potwierdzona'}</option>
                  <option value="3">{t['sessions.status.completed'] ?? 'Zakończona'}</option>
                  <option value="4">{t['sessions.status.cancelled'] ?? 'Anulowana'}</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">{t['sessions.filter.fromDate'] ?? 'Od daty'}</label>
                <input
                  type="date"
                  value={filters.fromDate}
                  onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">{t['sessions.filter.toDate'] ?? 'Do daty'}</label>
                <input
                  type="date"
                  value={filters.toDate}
                  onChange={(e) => setFilters({ ...filters, toDate: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2"
                />
              </div>
              <div className="flex items-end">
                <button
                  onClick={loadSessions}
                  className="w-full bg-gray-600 text-white px-4 py-2 rounded-lg hover:bg-gray-700 transition-colors"
                >
                  {t['sessions.filter.apply'] ?? 'Filtruj'}
                </button>
              </div>
            </div>
          </div>

          {/* Lista sesji */}
          {loading ? (
            <div className="text-center py-12 text-gray-500">{t['sessions.loading'] ?? 'Ładowanie...'}</div>
          ) : sessions.length === 0 ? (
            <div className="text-center py-12 text-gray-500">{t['sessions.empty'] ?? 'Brak sesji'}</div>
          ) : (
            <div className="bg-white rounded-lg shadow-lg divide-y divide-gray-200">
              {sessions.map((session: any) => (
                <div
                  key={session.id}
                  className="p-6 hover:bg-gray-50 cursor-pointer transition-colors"
                  onClick={() => navigate(`/sessions/${session.id}`)}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-bold text-xl mb-2 text-gray-800">
                        {session.patient?.firstName} {session.patient?.lastName}
                      </p>
                      <p className="text-gray-600 text-base mb-2">{session.patient?.email}</p>
                      <p className="text-gray-700 text-base font-medium">
                        {formatTime(session.startDateTime)} - {formatTime(session.endDateTime)}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className={`px-4 py-2 rounded-lg text-base font-semibold ${
                        session.statusId === 2 ? 'bg-green-100 text-green-800' :
                        session.statusId === 3 ? 'bg-blue-100 text-blue-800' :
                        session.statusId === 4 ? 'bg-red-100 text-red-800' :
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {getStatusName(session.statusId)}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  )
}


// Lista pacjentów
function PatientsList() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [patients, setPatients] = useState<any[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
    loadPatients()
  }, [culture, navigate])

  async function loadPatients() {
    try {
      const token = localStorage.getItem('token')
      const res = await fetch('/api/patients', { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) setPatients(await res.json())
    } catch (err) {
      console.error('Error loading patients:', err)
    } finally {
      setLoading(false)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="space-y-8">
          <div className="flex justify-between items-center">
            <h1>{t['patients.title'] ?? 'Pacjenci'}</h1>
            <button onClick={() => navigate('/patients/new')} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
              {t['patients.new'] ?? 'Nowy pacjent'}
            </button>
          </div>
          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div> :
           patients.length === 0 ? <div className="text-center p-8 text-lg text-gray-500">Brak pacjentów</div> :
           <div className="bg-white rounded-lg shadow-lg divide-y-2 divide-gray-100">
             {patients.map((p: any) => (
               <div key={p.id} className="p-6 hover:bg-gray-50 cursor-pointer transition-colors" onClick={() => navigate(`/patients/${p.id}`)}>
                 <div className="flex justify-between">
                   <div>
                     <p className="font-bold text-xl mb-2 text-gray-800">{p.firstName} {p.lastName}</p>
                     <p className="text-gray-600 text-base mb-1">{p.email}</p>
                     {p.phone && <p className="text-gray-600 text-base">{p.phone}</p>}
                   </div>
                 </div>
               </div>
             ))}
           </div>
          }
        </div>
      </main>
    </div>
  )
}

// Formularz pacjenta z ID z routingu
function PatientFormWithId() {
  const { id } = useParams<{ id: string }>()
  return <PatientForm id={id ? Number(id) : undefined} />
}

// Formularz sesji
function SessionForm() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [patients, setPatients] = useState<any[]>([])
  const [form, setForm] = useState({
    patientId: '', startDateTime: '', durationMinutes: '60', price: '', notes: '', sendNotification: false
  })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
    loadPatients()
    if (id) {
      loadSession()
    } else {
      setLoading(false)
    }
  }, [culture, id, navigate])
  
  async function loadPatients() {
    try {
      const token = localStorage.getItem('token')
      const res = await fetch('/api/patients', { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) setPatients(await res.json())
    } catch (err) {
      console.error('Error loading patients:', err)
    }
  }


  async function loadSession() {
    if (!id) return
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const s = await res.json()
        const start = new Date(s.startDateTime)
        const duration = Math.round((new Date(s.endDateTime).getTime() - start.getTime()) / 60000)
        const newForm = {
          patientId: s.patient.id.toString(),
          startDateTime: ensureFullHour(formatForDateTimeLocalInput(start)),
          durationMinutes: duration.toString(),
          price: s.price.toString(),
          notes: s.notes || '',
          sendNotification: false
        }
        setForm(newForm)
      }
    } catch (err) {
      console.error('Error loading session:', err)
    } finally {
      setLoading(false)
    }
  }

  async function save() {
    if (!form.patientId || !form.startDateTime || !form.durationMinutes || !form.price) {
      alert('Wypełnij wszystkie wymagane pola')
      return
    }
    setSaving(true)
    try {
      const token = localStorage.getItem('token')
      const url = id ? `/api/sessions/${id}` : '/api/sessions'
      const method = id ? 'PUT' : 'POST'
      const normalizedStart = ensureFullHour(form.startDateTime)
      const startDate = new Date(normalizedStart)
      const payload: any = {
        patientId: Number(form.patientId),
        startDateTime: startDate.toISOString(),
        durationMinutes: Number(form.durationMinutes),
        price: Number(form.price),
        notes: form.notes || null
      }
      const res = await fetch(url, {
        method,
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      if (res.ok) {
        const data = await res.json()
        if (form.sendNotification && !id) {
          await fetch(`/api/sessions/${data.id || id}/send-notification?culture=${culture}`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` }
          })
        }
        navigate('/sessions')
      } else {
        const error = await res.text()
        alert(`Błąd: ${error}`)
      }
    } catch (err) {
      console.error('Error saving session:', err)
      alert('Błąd podczas zapisywania')
    } finally {
      setSaving(false)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  if (loading) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-8">
        <h1>{id ? t['sessions.edit'] ?? 'Edytuj sesją™' : t['sessions.new'] ?? 'Nowa sesja'}</h1>
        <div className="bg-white p-8 rounded-lg shadow-lg space-y-6">
          <div className="form-group">
            <label>{t['sessions.patient'] ?? 'Pacjent'} *</label>
            <select value={form.patientId} onChange={e=>setForm({...form, patientId: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" required disabled={!!id}>
              <option value="">-- Wybierz pacjenta --</option>
              {patients.map(p => (
                <option key={p.id} value={p.id}>{p.firstName} {p.lastName} ({p.email})</option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div className="form-group">
              <label>{t['sessions.startTime'] ?? 'Data rozpoczęcia'} *</label>
              <div className="grid grid-cols-2 gap-4">
                <input
                  type="date"
                  value={form.startDateTime.split('T')[0] || ''}
                  onChange={e => {
                    const timePart = form.startDateTime.split('T')[1] || '00:00'
                    setForm({ ...form, startDateTime: `${e.target.value}T${timePart}` })
                  }}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                  required
                />
                <input
                  type="time"
                  value={form.startDateTime.split('T')[1]?.substring(0, 5) || '00:00'}
                  step={3600}
                  onChange={e => {
                    const datePart = form.startDateTime.split('T')[0] || ''
                    const newValue = ensureFullHour(`${datePart}T${e.target.value}`)
                    setForm({ ...form, startDateTime: newValue })
                  }}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                  required
                />
              </div>
            </div>
            <div className="form-group">
              <label>Czas trwania (min) *</label>
              <input type="number" value={form.durationMinutes} onChange={e=>setForm({...form, durationMinutes: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" min="15" step="15" required />
            </div>
          </div>
          <div className="form-group">
            <label>{t['sessions.price'] ?? 'Cena'} (PLN) *</label>
            <input type="number" value={form.price} onChange={e=>setForm({...form, price: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" step="0.01" min="0" required />
          </div>
          <div className="form-group">
            <label>{t['sessions.notes'] ?? 'Notatki'}</label>
            <textarea value={form.notes} onChange={e=>setForm({...form, notes: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" rows={5} />
          </div>
          
          {!id && (
            <div className="form-group">
              <label className="flex items-center gap-3 cursor-pointer">
                <input type="checkbox" checked={form.sendNotification} onChange={e=>setForm({...form, sendNotification: e.target.checked})} className="w-5 h-5" />
                <span className="text-base">{t['sessions.sendNotification'] ?? 'Wyślij powiadomienie po utworzeniu'}</span>
              </label>
            </div>
          )}
          <div className="flex gap-6 pt-4">
            <button onClick={save} disabled={saving} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 disabled:opacity-50 shadow-md transition-all">
              {t['sessions.save'] ?? 'Zapisz'}
            </button>
            <button onClick={() => navigate('/sessions')} className="bg-gray-600 text-white px-8 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all">Anuluj</button>
          </div>
        </div>
      </main>
    </div>
  )
}

// Kalendarz tygodniowy
function WeekCalendar() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [sessions, setSessions] = useState<any[]>([])
  const [currentWeekStart, setCurrentWeekStart] = useState(() => {
    const now = new Date()
    const day = now.getDay()
    const diff = now.getDate() - day + (day === 0 ? -6 : 1)
    const monday = new Date(now.setDate(diff))
    monday.setHours(0, 0, 0, 0)
    return monday
  })
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
    loadSessions()
  }, [culture, navigate, currentWeekStart])

  function getWeekDays() {
    const days = []
    for (let i = 0; i < 7; i++) {
      const date = new Date(currentWeekStart)
      date.setDate(currentWeekStart.getDate() + i)
      days.push(date)
    }
    return days
  }

  function getDaySessions(date: Date) {
    const dayStart = new Date(date)
    dayStart.setHours(0, 0, 0, 0)
    const dayEnd = new Date(date)
    dayEnd.setHours(23, 59, 59, 999)
    return sessions.filter(s => {
      const sessionDate = new Date(s.startDateTime)
      return sessionDate >= dayStart && sessionDate <= dayEnd
    })
  }

  async function loadSessions() {
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const weekEnd = new Date(currentWeekStart)
      weekEnd.setDate(weekEnd.getDate() + 7)
      const params = new URLSearchParams({
        fromDate: currentWeekStart.toISOString(),
        toDate: weekEnd.toISOString(),
        page: '1',
        pageSize: '1000'
      })
      const res = await fetch(`/api/sessions?${params}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const data = await res.json()
        setSessions(data.sessions || [])
      }
    } catch (err) {
      console.error('Error loading sessions:', err)
    } finally {
      setLoading(false)
    }
  }

  function previousWeek() {
    const prev = new Date(currentWeekStart)
    prev.setDate(prev.getDate() - 7)
    setCurrentWeekStart(prev)
  }

  function nextWeek() {
    const next = new Date(currentWeekStart)
    next.setDate(next.getDate() + 7)
    setCurrentWeekStart(next)
  }

  function todayWeek() {
    const now = new Date()
    const day = now.getDay()
    const diff = now.getDate() - day + (day === 0 ? -6 : 1)
    const monday = new Date(now.setDate(diff))
    monday.setHours(0, 0, 0, 0)
    setCurrentWeekStart(monday)
  }

  function formatTime(dateStr: string) {
    return formatTimeWithZone(dateStr, culture)
  }

  function formatDate(date: Date) {
    return formatDateWithZone(date, culture, { day: 'numeric', month: 'short' })
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  const weekDays = getWeekDays()
  const dayNames = culture === 'pl' 
    ? ['Pon', 'Wt', 'Śr', 'Czw', 'Pt', 'Sob', 'Nie']
    : ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
  const labels = getWeekCalendarLabels(culture, t)

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="space-y-8">
          <div className="flex justify-between items-center">
            <h1>{labels.title}</h1>
            <div className="flex gap-4 items-center">
              <button onClick={previousWeek} className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">{labels.previousWeek}</button>
              <button onClick={todayWeek} className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">{labels.today}</button>
              <button onClick={nextWeek} className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">{labels.nextWeek}</button>
              <button onClick={() => navigate('/sessions/new')} className="bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 shadow-md transition-all font-semibold">
                {t['sessions.new'] ?? 'Nowa sesja'}
              </button>
            </div>
          </div>

          {loading ? (
            <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div>
          ) : (
            <div className="bg-white rounded-lg shadow-lg overflow-hidden">
              <div className="grid grid-cols-7 border-b-2 border-gray-200">
                {weekDays.map((date, idx) => (
                  <div key={idx} className="p-6 text-center border-r-2 border-gray-100 last:border-r-0 bg-gray-50">
                    <div className="font-bold text-base text-gray-700 mb-2">{dayNames[idx]}</div>
                    <div className={`text-xl font-bold mt-2 ${date.toDateString() === new Date().toDateString() ? 'text-blue-600' : 'text-gray-800'}`}>
                      {formatDate(date)}
                    </div>
                  </div>
                ))}
              </div>
              <div className="grid grid-cols-7 divide-x-2 divide-gray-100">
                {weekDays.map((date, idx) => (
                  <div key={idx} className="min-h-[600px] p-4 bg-white">
                    {getDaySessions(date).map(session => (
                      <div 
                        key={session.id} 
                        onClick={() => navigate(`/sessions/${session.id}`)}
                        className="mb-3 p-4 bg-blue-50 border-2 border-blue-200 rounded-lg cursor-pointer hover:bg-blue-100 transition-colors shadow-sm"
                      >
                        <div className="text-base font-bold text-blue-800 mb-2">
                          {formatTime(session.startDateTime)}
                        </div>
                        <div className="text-base font-semibold text-blue-700">
                          {session.patient.firstName} {session.patient.lastName}
                        </div>
                      </div>
                    ))}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}

// Widok sesji dla pacjentów
function PatientSessionsList() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [sessions, setSessions] = useState<any[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadSessions()
  }, [culture, navigate])

  async function loadSessions() {
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const res = await fetch('/api/patient/sessions', { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const data = await res.json()
        setSessions(Array.isArray(data) ? data : [])
      }
    } catch (err) {
      console.error('Error loading sessions:', err)
    } finally {
      setLoading(false)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  function formatDateTime(dateStr: string) {
    return formatDateTimeWithZone(dateStr, culture)
  }

  function getPaymentStatusText(payment: any) {
    if (!payment) return 'Nieopłacona'
    if (payment.statusId === 2) return 'Opłacona'
    if (payment.statusId === 3) return 'Nieudana'
    return 'Oczekująca'
  }

  function getPaymentStatusColor(payment: any) {
    if (!payment) return 'bg-red-100 text-red-800'
    if (payment.statusId === 2) return 'bg-green-100 text-green-800'
    if (payment.statusId === 3) return 'bg-red-100 text-red-800'
    return 'bg-yellow-100 text-yellow-800'
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="space-y-8">
          <div className="flex justify-between items-center">
            <h1 className="text-3xl font-bold text-gray-800">Moje sesje</h1>
          </div>

          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div> :
           sessions.length === 0 ? <div className="text-center p-8 text-lg text-gray-500">Brak sesji</div> :
           <div className="bg-white rounded-lg shadow-lg divide-y-2 divide-gray-100">
             {sessions.map((s: any) => (
               <div
                 key={s.id}
                 className="p-6 hover:bg-gray-50 cursor-pointer transition-colors"
                 onClick={() => navigate(`/patient/sessions/${s.id}`)}
               >
                 <div className="flex justify-between items-start">
                   <div>
                     <p className="text-gray-700 text-base font-medium mb-2">{formatDateTime(s.startDateTime)}</p>
                     <p className="text-gray-600 text-sm mb-2">
                       Sesja standardowa
                     </p>
                     <p className="text-gray-800 text-lg font-bold">{s.price} PLN</p>
                   </div>
                   <div className="text-right">
                     <span className={`px-4 py-2 rounded-lg text-base font-semibold ${getPaymentStatusColor(s.payment)}`}>
                       {getPaymentStatusText(s.payment)}
                     </span>
                     {s.isPaid && s.payment?.completedAt && (
                       <p className="mt-2 text-sm text-gray-600">
                         Opłacona: {formatDateTime(s.payment.completedAt)}
                       </p>
                     )}
                   </div>
                 </div>
               </div>
             ))}
           </div>
          }
        </div>
      </main>
    </div>
  )
}

// Szczegóły sesji dla pacjentów
function PatientSessionDetails() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [session, setSession] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [processingPayment, setProcessingPayment] = useState(false)

  useEffect(() => {
    if (!id) return
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
  }, [culture, id, navigate])

  useEffect(() => {
    if (id) loadSession()
  }, [id])

  async function loadSession() {
    if (!id) return
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/patient/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const data = await res.json()
        setSession(data)
      }
    } catch (err) {
      console.error('Error loading session:', err)
    } finally {
      setLoading(false)
    }
  }

  async function initiatePayment() {
    if (!id || !session) return
    try {
      setProcessingPayment(true)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/patient/sessions/${id}/initiate-payment`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      })
      if (res.ok) {
        const data = await res.json()
        if (data.paymentUrl) {
          window.location.href = data.paymentUrl
        } else {
          alert('Nie udało się utworzyć płatności')
        }
      } else {
        const error = await res.json()
        alert(error.error || 'Nie udało się zainicjować płatności')
      }
    } catch (err) {
      console.error('Error initiating payment:', err)
      alert('Błąd podczas inicjacji płatności')
    } finally {
      setProcessingPayment(false)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  if (loading || !session) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  function formatDateTime(dateStr: string) {
    return formatDateTimeWithZone(dateStr, culture)
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-8">
        <div className="space-y-6">
          <div className="flex justify-between items-center">
            <h1 className="text-3xl font-bold text-gray-800">Szczegóły sesji</h1>
            <button onClick={() => navigate('/patient/sessions')} className="text-blue-600 hover:text-blue-800 font-semibold">
              ← Powrót do listy
            </button>
          </div>

          <div className="bg-white rounded-lg shadow-lg p-6 space-y-6">
            <div>
              <h2 className="text-xl font-bold text-gray-800 mb-4">Informacje o sesji</h2>
              <div className="space-y-3">
                <div>
                  <p className="text-sm text-gray-600">Data i godzina</p>
                  <p className="text-lg font-semibold text-gray-800">{formatDateTime(session.startDateTime)}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-600">Cena</p>
                  <p className="text-2xl font-bold text-gray-800">{session.price} PLN</p>
                </div>
              </div>
            </div>

            <div className="border-t pt-6">
              <h2 className="text-xl font-bold text-gray-800 mb-4">Status płatności</h2>
              {session.isPaid ? (
                <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                  <p className="text-green-800 font-semibold mb-2">✓ Sesja opłacona</p>
                  {session.payment?.completedAt && (
                    <p className="text-sm text-green-700">
                      Data płatności: {formatDateTime(session.payment.completedAt)}
                    </p>
                  )}
                  {session.payment?.amount && (
                    <p className="text-sm text-green-700">
                      Kwota: {session.payment.amount} PLN
                    </p>
                  )}
                </div>
              ) : (
                <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                  <p className="text-yellow-800 font-semibold mb-2">⚠ Sesja nieopłacona</p>
                  {session.canPay && (
                    <button
                      onClick={initiatePayment}
                      disabled={processingPayment}
                      className="mt-4 bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {processingPayment ? 'Przetwarzanie...' : 'Zapłać przez Tpay'}
                    </button>
                  )}
                </div>
              )}
            </div>

            {session.googleMeetLink && (
              <div className="border-t pt-6">
                <h2 className="text-xl font-bold text-gray-800 mb-4">Link do spotkania</h2>
                <a
                  href={session.googleMeetLink}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:text-blue-800 font-semibold underline"
                >
                  Otwórz Google Meet
                </a>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}

function GoogleCalendarIntegrationPage() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }
    loadTranslations(culture).then(setT).catch(err => console.error('Error loading translations:', err))
  }, [culture, navigate])

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-8">
        <div className="space-y-8">
          <div className="bg-white rounded-lg shadow-lg p-8 space-y-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">{t['integrations.googleCalendar.title'] ?? 'Integracja z Google Calendar'}</h1>
              <p className="mt-2 text-base text-gray-600">
                {t['integrations.googleCalendar.description'] ?? 'Połącz swoje konto terapeuty z Google Calendar, aby automatycznie tworzyć wydarzenia i linki Google Meet dla sesji.'}
              </p>
            </div>
            <GoogleCalendarIntegrationForm returnUrl="/integrations/google-calendar" />
          </div>

          <div className="bg-white rounded-lg shadow-lg p-8 space-y-4">
            <h2 className="text-2xl font-bold text-gray-900">{t['integrations.googleCalendar.helpTitle'] ?? 'Najczęstsze problemy'}</h2>
            <ul className="list-disc space-y-2 pl-6 text-sm text-gray-700">
              <li>{t['integrations.googleCalendar.help.scope'] ?? 'Upewnij się, że podczas autoryzacji zaznaczasz wszystkie wymagane zakresy (kalendarz i Google Meet). Bez nich wydarzenia nie zostaną utworzone.'}</li>
              <li>{t['integrations.googleCalendar.help.calendarAccess'] ?? 'Konto terapeuty musi mieć prawo zapisu do kalendarza skonfigurowanego w aplikacji. Jeśli pojawia się błąd uprawnień, sprawdź ustawienia kalendarza Google.'}</li>
              <li>{t['integrations.googleCalendar.help.refresh'] ?? 'W razie utraty połączenia odłącz kalendarz i wykonaj ponowną autoryzację. Pamiętaj, aby przeprowadzać ją po każdej zmianie hasła w Google.'}</li>
            </ul>
          </div>
        </div>
      </main>
    </div>
  )
}

function App() {
  return (
    <BrowserRouter>
      <ActivityTracker />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/sessions" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><SessionsList /></ProtectedRoute>} />
        <Route path="/sessions/new" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><SessionForm /></ProtectedRoute>} />
        <Route path="/sessions/:id/edit" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><SessionForm /></ProtectedRoute>} />
        <Route path="/sessions/:id" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><SessionDetails /></ProtectedRoute>} />
        <Route path="/calendar" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><WeekCalendar /></ProtectedRoute>} />
        <Route path="/patients" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><PatientsList /></ProtectedRoute>} />
        <Route path="/patients/new" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><PatientForm /></ProtectedRoute>} />
        <Route path="/patients/:id" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><PatientFormWithId /></ProtectedRoute>} />
        <Route path="/integrations/google-calendar" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess', 'Administrator']}><GoogleCalendarIntegrationPage /></ProtectedRoute>} />
        <Route path="/session-types" element={<ProtectedRoute allowedRoles={['Administrator']}><SessionTypeManagement /></ProtectedRoute>} />
        <Route path="/therapist/profile" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess']}><TherapistProfileManagement /></ProtectedRoute>} />
        <Route path="/therapist/documents" element={<ProtectedRoute allowedRoles={['Terapeuta', 'TerapeutaFreeAccess']}><TherapistDocumentsManagement /></ProtectedRoute>} />
        <Route path="/admin/users" element={<ProtectedRoute allowedRoles={['Administrator']}><UsersAdmin /></ProtectedRoute>} />
        <Route path="/admin/therapists" element={<ProtectedRoute allowedRoles={['Administrator']}><TherapistsAdmin /></ProtectedRoute>} />
        <Route path="/admin/activity" element={<ProtectedRoute allowedRoles={['Administrator']}><ActivityLogAdmin /></ProtectedRoute>} />
        <Route path="/patient/sessions" element={<ProtectedRoute allowedRoles={['Pacjent']}><PatientSessionsList /></ProtectedRoute>} />
        <Route path="/patient/sessions/:id" element={<ProtectedRoute allowedRoles={['Pacjent']}><PatientSessionDetails /></ProtectedRoute>} />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App




