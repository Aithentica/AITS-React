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

const Roles = {
  Administrator: 'Administrator',
  Terapeuta: 'Terapeuta',
  Pacjent: 'Pacjent'
} as const

// Komponent ochrony routingu - sprawdza czy uĹĽytkownik ma odpowiedniÄ… rolÄ™
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
      
      // Sprawdź czy uĹĽytkownik ma jednÄ… z dozwolonych rĂłl
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
    return <div className="min-h-screen flex items-center justify-center">Ĺadowanie...</div>
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
    // Automatyczne przekierowanie może powodować‡ pÄ™tle jeĹ›li token jest nieprawidĹ‚owy
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
      console.log('Zalogowano pomyĹ›lnie. Role:', data.roles)
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
            <label className="block text-sm mb-1">{t['login.password'] ?? 'HasĹ‚o'}</label>
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

function Dashboard() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [roles, setRoles] = useState<string[]>([])
  const [rolesLoaded, setRolesLoaded] = useState(false)
  const [todaySessions, setTodaySessions] = useState<Session[]>([])
  const [loading, setLoading] = useState(true)
  const [stats, setStats] = useState({ today: 0, scheduled: 0, completed: 0 })
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      // UĹĽyj window.location.href zamiast navigate() aby uniknÄ…Ä‡ pÄ™tli przekierowaĹ„
      window.location.href = '/login'
      return
    }
    const storedRoles = localStorage.getItem('roles')
    if (storedRoles) {
      try {
        const parsedRoles = JSON.parse(storedRoles)
        console.log('Wczytane role z localStorage:', parsedRoles)
        setRoles(Array.isArray(parsedRoles) ? parsedRoles : [])
      } catch (e) {
        console.error('Error parsing roles:', e)
        setRoles([])
      }
    } else {
      console.warn('Brak rĂłl w localStorage')
      setRoles([])
    }
    setRolesLoaded(true)
  }, [])

  useEffect(() => {
    loadTranslations(culture).then(setT).catch(err => console.error('Error loading translations:', err))
  }, [culture])

  useEffect(() => {
    if (!rolesLoaded) return
    
    // JeĹ›li nie ma ĹĽadnych rĂłl, nie prĂłbuj zaĹ‚adowaÄ‡ danych - pokaĹĽ bĹ‚Ä…d
    if (roles.length === 0) {
      console.error('Brak rĂłl w localStorage. UĹĽytkownik może nie byÄ‡ poprawnie zalogowany.')
      setError('Brak uprawnień. Zaloguj się ponownie.')
      setLoading(false)
      return
    }
    
    if (roles.includes(Roles.Administrator)) {
      setLoading(false)
      return
    }
    if (roles.includes(Roles.Pacjent)) {
      loadPatientDashboardData()
      return
    }
    // JeĹ›li uĹĽytkownik ma innÄ… rolÄ™ (terapeuta), zaĹ‚aduj dane terapeuty
    loadDashboardData()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [roles, rolesLoaded])

  async function loadPatientDashboardData() {
    try {
      setError(null)
      const token = localStorage.getItem('token')
      if (!token) {
        setError('Brak tokenu autoryzacji')
        setLoading(false)
        return
      }
      
      const headers: HeadersInit = { 
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
      
      const res = await fetch('/api/patient/sessions', { headers }).catch(err => {
        console.error('Error fetching patient sessions:', err)
        return { ok: false, status: 500 } as Response
      })
      
      if (!res.ok) {
        if (res.status === 401 || res.status === 403) {
          setError('Brak uprawnień. Zaloguj się ponownie.')
          localStorage.removeItem('token')
          localStorage.removeItem('roles')
          // UĹĽyj window.location.href zamiast navigate() aby uniknÄ…Ä‡ pÄ™tli przekierowaĹ„
          setTimeout(() => {
            window.location.href = '/login'
          }, 1000)
          setLoading(false)
          return
        }
        const errorText = await res.text().catch(() => '')
        console.error('Error loading patient sessions:', res.status, errorText)
        setError(`Błąd ładowania sesji: ${res.status}`)
        setLoading(false)
        return
      }
      
      if (res.ok) {
        try {
          const allSessions = await res.json()
          const sessionsArray = Array.isArray(allSessions) ? allSessions : []
          
          // Filtruj sesje do dzisiejszych
          const today = new Date()
          today.setHours(0, 0, 0, 0)
          const tomorrow = new Date(today)
          tomorrow.setDate(tomorrow.getDate() + 1)
          
          const todaySessionsFiltered = sessionsArray.filter((session: Session) => {
            const sessionDate = new Date(session.startDateTime)
            return sessionDate >= today && sessionDate < tomorrow
          })
          
          setTodaySessions(todaySessionsFiltered)
          
          // Ustaw statystyki
          const scheduled = sessionsArray.filter((s: Session) => s.statusId === 1).length
          const completed = sessionsArray.filter((s: Session) => {
            const sessionDate = new Date(s.startDateTime)
            const monthStart = new Date(today.getFullYear(), today.getMonth(), 1)
            return s.statusId === 3 && sessionDate >= monthStart
          }).length
          
          setStats({
            today: todaySessionsFiltered.length,
            scheduled,
            completed
          })
        } catch (e) {
          console.error('Error parsing patient sessions:', e)
          setTodaySessions([])
          setStats({ today: 0, scheduled: 0, completed: 0 })
        }
      }
    } catch (err) {
      console.error('Error loading patient dashboard:', err)
      setError('Błąd połączenia z serwerem')
    } finally {
      setLoading(false)
    }
  }

  async function loadDashboardData() {
    try {
      setError(null)
      const token = localStorage.getItem('token')
      if (!token) {
        setError('Brak tokenu autoryzacji')
        return
      }
      
      const headers: HeadersInit = { 
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
      
      const [todayRes, allRes] = await Promise.all([
        fetch('/api/sessions/today', { headers }).catch(err => {
          console.error('Error fetching today sessions:', err)
          return { ok: false, status: 500 } as Response
        }),
        fetch('/api/sessions?page=1&pageSize=1000', { headers }).catch(err => {
          console.error('Error fetching all sessions:', err)
          return { ok: false, status: 500 } as Response
        })
      ])
      
      if (!todayRes.ok && todayRes.status !== 500) {
        if (todayRes.status === 401 || todayRes.status === 403) {
          setError('Brak uprawnień. Zaloguj się ponownie.')
          localStorage.removeItem('token')
          localStorage.removeItem('roles')
          // UĹĽyj window.location.href zamiast navigate() aby uniknÄ…Ä‡ pÄ™tli przekierowaĹ„
          setTimeout(() => {
            window.location.href = '/login'
          }, 1000)
          return
        }
        setError(`Błąd ładowania sesji: ${todayRes.status}`)
      }
      
      if (todayRes.ok) {
        try {
          const today = await todayRes.json()
          console.log('Today sessions loaded:', today)
          setTodaySessions(Array.isArray(today) ? today : [])
          setStats(prev => ({ ...prev, today: Array.isArray(today) ? today.length : 0 }))
        } catch (e) {
          console.error('Error parsing today sessions:', e)
          setTodaySessions([])
        }
      } else {
        console.error('Failed to load today sessions:', todayRes.status, await todayRes.text().catch(() => ''))
      }
      
      if (allRes.ok) {
        try {
          const all = await allRes.json()
          if (all && all.sessions && Array.isArray(all.sessions)) {
            const now = new Date()
            const monthStart = new Date(now.getFullYear(), now.getMonth(), 1)
            const scheduled = all.sessions.filter((s: Session) => s.statusId === 1).length
            const completed = all.sessions.filter((s: Session) => 
              s.statusId === 3 && new Date(s.startDateTime) >= monthStart
            ).length
            setStats(prev => ({ ...prev, scheduled, completed }))
          }
        } catch (e) {
          console.error('Error parsing all sessions:', e)
        }
      }
    } catch (err) {
      console.error('Error loading dashboard:', err)
      setError('Błąd połączenia z serwerem')
    } finally {
      setLoading(false)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  const isAdmin = roles.includes(Roles.Administrator)
  const isPatient = roles.includes(Roles.Pacjent)
  const noSessionsFallback = culture === 'en' ? 'No sessions today' : 'Brak sesji na dzisiaj'

  if (isAdmin) {
    return (
      <div className="min-h-screen bg-gray-50">
        <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
        <main className="w-full py-12 px-8 space-y-10">
          <section className="bg-white p-8 rounded-xl shadow-lg border border-gray-200">
            <h1 className="text-3xl font-bold text-gray-900 mb-4">{t['admin.dashboard.title'] ?? 'Panel administratora'}</h1>
            <p className="text-gray-600 text-base leading-relaxed max-w-3xl">
              {t['admin.dashboard.subtitle'] ?? 'ZarzÄ…dzaj uĹĽytkownikami systemu oraz profilami terapeutĂłw. Wybierz moduĹ‚, aby wyĹ›wietliÄ‡ szczegĂłĹ‚y i wykonaÄ‡ operacje administracyjne.'}
            </p>
          </section>

          <section className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            <button
              onClick={() => navigate('/admin/users')}
              className="text-left bg-white border border-gray-200 rounded-xl p-6 shadow-md hover:shadow-lg transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <h2 className="text-2xl font-semibold text-gray-900 mb-2">{t['admin.dashboard.users'] ?? 'ZarzÄ…dzanie uĹĽytkownikami'}</h2>
              <p className="text-sm text-gray-600">
                {t['admin.dashboard.users.desc'] ?? 'Dodawaj nowych uĹĽytkownikĂłw, aktualizuj role oraz blokuj dostÄ™py do aplikacji.'}
              </p>
            </button>

            <button
              onClick={() => navigate('/admin/therapists')}
              className="text-left bg-white border border-gray-200 rounded-xl p-6 shadow-md hover:shadow-lg transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <h2 className="text-2xl font-semibold text-gray-900 mb-2">{t['admin.dashboard.therapists'] ?? 'ZarzÄ…dzanie terapeutami'}</h2>
              <p className="text-sm text-gray-600">
                {t['admin.dashboard.therapists.desc'] ?? 'Przypisuj role terapeuty, nadaj dostÄ™p typu Free Access oraz kontroluj aktywnoĹ›Ä‡ profili.'}
              </p>
            </button>

            <button
              onClick={() => navigate('/admin/activity')}
              className="text-left bg-white border border-gray-200 rounded-xl p-6 shadow-md hover:shadow-lg transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <h2 className="text-2xl font-semibold text-gray-900 mb-2">{t['admin.dashboard.activity'] ?? 'AktywnoĹ›Ä‡ uĹĽytkownikĂłw'}</h2>
              <p className="text-sm text-gray-600">
                {t['admin.dashboard.activity.desc'] ?? 'Analizuj, jakie widoki odwiedzają… uĹĽytkownicy oraz ile czasu na nich spÄ™dzają….'}
              </p>
            </button>
          </section>
        </main>
      </div>
    )
  }

  // Dashboard dla pacjenta - pusty widok
  if (isPatient) {
    return (
      <div className="min-h-screen bg-gray-50">
        <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
        <main className="w-full py-8 px-8">
        </main>
      </div>
    )
  }

  function formatTime(dateStr: string) {
    return formatTimeWithZone(dateStr, culture)
  }

  function getStatusName(statusId: number) {
    // TODO: Pobieranie z tĹ‚umaczeĹ„ enumĂłw
    const statusNames: Record<number, string> = { 1: 'Zaplanowana', 2: 'Potwierdzona', 3: 'ZakoĹ„czona', 4: 'Anulowana' }
    return statusNames[statusId] || 'Nieznany'
  }

  // Dashboard dla terapeuty
  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="space-y-8">
          {/* Statystyki */}
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-6">
            <div className="bg-white p-8 rounded-lg shadow-lg border-l-4 border-blue-500">
              <h3 className="text-xl font-bold text-gray-700 mb-4">{t['dashboard.sessionsToday'] ?? 'Sesje dzisiaj'}</h3>
              <p className="text-4xl font-bold text-blue-600">{loading ? '...' : stats.today}</p>
            </div>
            <div className="bg-white p-8 rounded-lg shadow-lg border-l-4 border-green-500">
              <h3 className="text-xl font-bold text-gray-700 mb-4">{t['dashboard.sessionsScheduled'] ?? 'Zaplanowane'}</h3>
              <p className="text-4xl font-bold text-green-600">{loading ? '...' : stats.scheduled}</p>
            </div>
            <div className="bg-white p-8 rounded-lg shadow-lg border-l-4 border-purple-500">
              <h3 className="text-xl font-bold text-gray-700 mb-4">{t['dashboard.sessionsCompleted'] ?? 'ZakoĹ„czone w tym miesię…cu'}</h3>
              <p className="text-4xl font-bold text-purple-600">{loading ? '...' : stats.completed}</p>
            </div>
          </div>

          {/* Dzisiejsze sesje */}
          <div className="bg-white rounded-lg shadow-lg">
            <div className="p-6 border-b-2 border-gray-200 flex justify-between items-center">
              <h2 className="text-2xl font-bold">{t['dashboard.todaySessions'] ?? 'Dzisiejsze sesje'}</h2>
              <button onClick={loadDashboardData} className="text-blue-600 hover:text-blue-800 font-semibold text-base px-4 py-2 rounded-lg hover:bg-blue-50 transition-colors">OdĹ›wieĹĽ</button>
            </div>
            {error ? (
              <div className="p-8 text-center text-red-600 text-lg font-semibold">{error}</div>
            ) : loading ? (
              <div className="p-8 text-center text-gray-500 text-lg">Ĺadowanie...</div>
            ) : todaySessions.length === 0 ? (
              <div className="p-8 text-center">
                <p className="text-gray-600 text-lg mb-6">{t['dashboard.noSessionsToday'] ?? noSessionsFallback}</p>
                <button onClick={() => navigate('/sessions/new')} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
                  {t['sessions.new'] ?? 'utwórz nowÄ… sesją™'}
                </button>
              </div>
            ) : (
              <div className="divide-y-2 divide-gray-100">
                {todaySessions.map(session => (
                  <div key={session.id} className="p-6 hover:bg-gray-50 cursor-pointer transition-colors" onClick={() => navigate(`/sessions/${session.id}`)}>
                    <div className="flex justify-between items-start">
                      <div>
                        <p className="font-bold text-xl mb-2 text-gray-800">
                          {session.patient.firstName} {session.patient.lastName}
                        </p>
                        <p className="text-gray-600 text-base mb-2">{session.patient.email}</p>
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
                        {session.googleMeetLink && (
                          <a href={session.googleMeetLink} target="_blank" rel="noopener noreferrer" 
                             className="block mt-3 text-blue-600 hover:text-blue-800 text-base font-semibold underline">
                            Google Meet
                          </a>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}

// Komponent nawigacji wspĂłlny dla wszystkich stron
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
      
      const res = await fetch(`/api/sessions?${params}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      })
      if (res.ok) {
        const data = await res.json()
        const toTime = (value: string | null | undefined) => new Date(value ?? 0).getTime()
        const sortedSessions = [...(data.sessions || [])]
          .sort((a, b) => toTime(b.startDateTime) - toTime(a.startDateTime))
        setSessions(sortedSessions)
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

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="space-y-8">
          <div className="flex justify-between items-center">
            <h1>{t['sessions.title'] ?? 'Sesje'}</h1>
            <button onClick={() => navigate('/sessions/new')} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
              {t['sessions.new'] ?? 'Nowa sesja'}
            </button>
          </div>

          {/* Filtry */}
          <div className="bg-white p-6 rounded-lg shadow-lg grid grid-cols-1 md:grid-cols-4 gap-6">
            <select value={filters.statusId} onChange={e=>setFilters({...filters, statusId: e.target.value})} className="border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200">
              <option value="">Wszystkie statusy</option>
              <option value="1">Zaplanowana</option>
              <option value="2">Potwierdzona</option>
              <option value="3">ZakoĹ„czona</option>
              <option value="4">Anulowana</option>
            </select>
            <input type="date" value={filters.fromDate} onChange={e=>setFilters({...filters, fromDate: e.target.value})} className="border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" placeholder="Od" />
            <input type="date" value={filters.toDate} onChange={e=>setFilters({...filters, toDate: e.target.value})} className="border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" placeholder="Do" />
            <button onClick={loadSessions} className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">Filtruj</button>
          </div>

          {/* Lista */}
          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ĺadowanie...</div> :
           sessions.length === 0 ? <div className="text-center p-8 text-lg text-gray-500">Brak sesji</div> :
           <div className="bg-white rounded-lg shadow-lg divide-y-2 divide-gray-100">
             {sessions.map((s: any) => (
               <div
                 key={s.id}
                 data-testid="session-item"
                 className="p-6 hover:bg-gray-50 cursor-pointer transition-colors"
                 onClick={() => navigate(`/sessions/${s.id}`)}
               >
                 <div className="flex justify-between items-start">
                   <div>
                     <p className="font-bold text-xl mb-2 text-gray-800">{s.patient.firstName} {s.patient.lastName}</p>
                     <p className="text-gray-600 text-base mb-2">{s.patient.email}</p>
                     <p className="text-gray-700 text-base font-medium">{formatDateTime(s.startDateTime)}</p>
                   </div>
                   <div className="text-right">
                     <span className="px-4 py-2 rounded-lg text-base font-semibold bg-gray-100 text-gray-800">{s.statusId === 1 ? 'Zaplanowana' : s.statusId === 2 ? 'Potwierdzona' : s.statusId === 3 ? 'ZakoĹ„czona' : 'Anulowana'}</span>
                     <p className="mt-3 font-bold text-lg">{s.price} PLN</p>
                     {s.payment && (
                       <div className="mt-2">
                         {s.isPaid ? (
                           <span className="px-3 py-1 rounded text-sm font-semibold bg-green-100 text-green-800">
                             Opłacona
                           </span>
                         ) : (
                           <div>
                             <span className="px-3 py-1 rounded text-sm font-semibold bg-yellow-100 text-yellow-800">
                               Nieopłacona
                             </span>
                             {s.paymentDelayDays !== null && s.paymentDelayDays !== undefined && (
                               <p className="text-xs text-red-600 mt-1">
                                 Opóźnienie: {s.paymentDelayDays} {s.paymentDelayDays === 1 ? 'dzień' : 'dni'}
                               </p>
                             )}
                           </div>
                         )}
                       </div>
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

// Lista pacjentĂłw
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
          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ĺadowanie...</div> :
           patients.length === 0 ? <div className="text-center p-8 text-lg text-gray-500">Brak pacjentĂłw</div> :
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
      alert('WypeĹ‚nij wszystkie wymagane pola')
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

  if (loading) return <div className="min-h-screen flex items-center justify-center">Ĺadowanie...</div>

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
              <label>{t['sessions.startTime'] ?? 'Data rozpoczÄ™cia'} *</label>
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
                <span className="text-base">{t['sessions.sendNotification'] ?? 'WyĹ›lij powiadomienie po utworzeniu'}</span>
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
    ? ['Pon', 'Wt', 'Ĺšr', 'Czw', 'Pt', 'Sob', 'Nie']
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
            <div className="text-center p-8 text-lg text-gray-500">Ĺadowanie...</div>
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

// Widok sesji dla pacjentĂłw
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

          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ĺadowanie...</div> :
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
                         OpĹ‚acona: {formatDateTime(s.payment.completedAt)}
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

  if (loading || !session) return <div className="min-h-screen flex items-center justify-center">Ĺadowanie...</div>

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
                      {processingPayment ? 'Przetwarzanie...' : 'ZapĹ‚aÄ‡ przez Tpay'}
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
                  OtwĂłrz Google Meet
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
                {t['integrations.googleCalendar.description'] ?? 'PoĹ‚Ä…cz swoje konto terapeuty z Google Calendar, aby automatycznie tworzyÄ‡ wydarzenia i linki Google Meet dla sesji.'}
              </p>
            </div>
            <GoogleCalendarIntegrationForm returnUrl="/integrations/google-calendar" />
          </div>

          <div className="bg-white rounded-lg shadow-lg p-8 space-y-4">
            <h2 className="text-2xl font-bold text-gray-900">{t['integrations.googleCalendar.helpTitle'] ?? 'NajczÄ™stsze problemy'}</h2>
            <ul className="list-disc space-y-2 pl-6 text-sm text-gray-700">
              <li>{t['integrations.googleCalendar.help.scope'] ?? 'Upewnij się, że podczas autoryzacji zaznaczasz wszystkie wymagane zakresy (kalendarz i Google Meet). Bez nich wydarzenia nie zostaną utworzone.'}</li>
              <li>{t['integrations.googleCalendar.help.calendarAccess'] ?? 'Konto terapeuty musi mieć prawo zapisu do kalendarza skonfigurowanego w aplikacji. Jeśli pojawia się błąd uprawnień, sprawdź ustawienia kalendarza Google.'}</li>
              <li>{t['integrations.googleCalendar.help.refresh'] ?? 'W razie utraty poĹ‚Ä…czenia odĹ‚Ä…cz kalendarz i wykonaj ponowną… autoryzację™. PamiÄ™taj, aby przeprowadzać‡ ją… po każdej zmianie hasĹ‚a w Google.'}</li>
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




