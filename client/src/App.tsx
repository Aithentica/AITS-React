import { useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, Navigate, useNavigate, useParams } from 'react-router-dom'
import './index.css'
import { loadTranslations, type Translations } from './i18n'

function LoginPage() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadTranslations(culture).then(setT)
    const token = localStorage.getItem('token')
    if (token) navigate('/dashboard')
  }, [culture, navigate])

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
            <input value={email} onChange={e=>setEmail(e.target.value)} type="email" className="w-full border rounded px-3 py-2" required />
          </div>
      <div>
            <label className="block text-sm mb-1">{t['login.password'] ?? 'Hasło'}</label>
            <input value={password} onChange={e=>setPassword(e.target.value)} type="password" className="w-full border rounded px-3 py-2" required />
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

function Dashboard() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [roles, setRoles] = useState<string[]>([])
  const [todaySessions, setTodaySessions] = useState<Session[]>([])
  const [loading, setLoading] = useState(true)
  const [stats, setStats] = useState({ today: 0, scheduled: 0, completed: 0 })
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }
    const storedRoles = localStorage.getItem('roles')
    if (storedRoles) {
      try {
        setRoles(JSON.parse(storedRoles))
      } catch (e) {
        console.error('Error parsing roles:', e)
        setRoles([])
      }
    }
    loadTranslations(culture).then(setT).catch(err => console.error('Error loading translations:', err))
    loadDashboardData()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

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
          navigate('/login')
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

  function formatTime(dateStr: string) {
    const d = new Date(dateStr)
    return d.toLocaleTimeString(culture === 'pl' ? 'pl-PL' : 'en-US', { hour: '2-digit', minute: '2-digit' })
  }

  function getStatusName(statusId: number) {
    // TODO: Pobieranie z tłumaczeń enumów
    const statusNames: Record<number, string> = { 1: 'Zaplanowana', 2: 'Potwierdzona', 3: 'Zakończona', 4: 'Anulowana' }
    return statusNames[statusId] || 'Nieznany'
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center gap-4">
              <h1 className="text-xl font-semibold">{t['dashboard.title'] ?? 'Kokpit'}</h1>
              <button onClick={() => navigate('/sessions')} className="text-blue-600 hover:text-blue-800">
                {t['dashboard.allSessions'] ?? 'Wszystkie sesje'}
              </button>
              <button onClick={() => navigate('/patients')} className="text-blue-600 hover:text-blue-800">
                {t['patients.title'] ?? 'Pacjenci'}
              </button>
            </div>
            <div className="flex items-center gap-4">
              <select value={culture} onChange={e=>setCulture(e.target.value as 'pl'|'en')} className="border rounded px-2 py-1">
                <option value="pl">PL</option>
                <option value="en">EN</option>
              </select>
              <button onClick={logout} className="bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700">
                Wyloguj
        </button>
            </div>
          </div>
        </div>
      </nav>
      <main className="max-w-7xl mx-auto py-8 px-6">
        <div className="space-y-8">
          {/* Statystyki */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white p-8 rounded-lg shadow-lg border-l-4 border-blue-500">
              <h3 className="text-xl font-bold text-gray-700 mb-4">{t['dashboard.sessionsToday'] ?? 'Sesje dzisiaj'}</h3>
              <p className="text-4xl font-bold text-blue-600">{loading ? '...' : stats.today}</p>
            </div>
            <div className="bg-white p-8 rounded-lg shadow-lg border-l-4 border-green-500">
              <h3 className="text-xl font-bold text-gray-700 mb-4">{t['dashboard.sessionsScheduled'] ?? 'Zaplanowane'}</h3>
              <p className="text-4xl font-bold text-green-600">{loading ? '...' : stats.scheduled}</p>
            </div>
            <div className="bg-white p-8 rounded-lg shadow-lg border-l-4 border-purple-500">
              <h3 className="text-xl font-bold text-gray-700 mb-4">{t['dashboard.sessionsCompleted'] ?? 'Zakończone w tym miesiącu'}</h3>
              <p className="text-4xl font-bold text-purple-600">{loading ? '...' : stats.completed}</p>
            </div>
          </div>

          {/* Dzisiejsze sesje */}
          <div className="bg-white rounded-lg shadow-lg">
            <div className="p-6 border-b-2 border-gray-200 flex justify-between items-center">
              <h2 className="text-2xl font-bold">{t['dashboard.todaySessions'] ?? 'Dzisiejsze sesje'}</h2>
              <button onClick={loadDashboardData} className="text-blue-600 hover:text-blue-800 font-semibold text-base px-4 py-2 rounded-lg hover:bg-blue-50 transition-colors">Odśwież</button>
            </div>
            {error ? (
              <div className="p-8 text-center text-red-600 text-lg font-semibold">{error}</div>
            ) : loading ? (
              <div className="p-8 text-center text-gray-500 text-lg">Ładowanie...</div>
            ) : todaySessions.length === 0 ? (
              <div className="p-8 text-center">
                <p className="text-gray-600 text-lg mb-6">Brak sesji na dzisiaj</p>
                <button onClick={() => navigate('/sessions/new')} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
                  {t['sessions.new'] ?? 'Utwórz nową sesję'}
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

// Komponent nawigacji wspólny dla wszystkich stron
function NavBar({ culture, setCulture, onLogout, navigate }: { culture: 'pl'|'en', setCulture: (c: 'pl'|'en') => void, onLogout: () => void, navigate: (path: string) => void }) {
  const [t, setT] = useState<Translations>({})
  useEffect(() => { loadTranslations(culture).then(setT) }, [culture])
  
  return (
      <nav className="bg-white shadow-md">
      <div className="max-w-7xl mx-auto px-6 py-4">
        <div className="flex justify-between items-center">
          <div className="flex items-center gap-6">
            <button onClick={() => navigate('/dashboard')} className="text-xl font-bold text-gray-800 hover:text-blue-600 transition-colors">{t['dashboard.title'] ?? 'Kokpit'}</button>
            <button onClick={() => navigate('/sessions')} className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors">{t['sessions.title'] ?? 'Sesje'}</button>
            <button onClick={() => navigate('/calendar')} className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors">Kalendarz</button>
            <button onClick={() => navigate('/patients')} className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors">{t['patients.title'] ?? 'Pacjenci'}</button>
          </div>
          <div className="flex items-center gap-4">
            <select value={culture} onChange={e=>setCulture(e.target.value as 'pl'|'en')} className="border-2 border-gray-300 rounded-lg px-4 py-2 text-base focus:border-blue-500 focus:ring-2 focus:ring-blue-200">
              <option value="pl">PL</option>
              <option value="en">EN</option>
            </select>
            <button onClick={onLogout} className="bg-red-600 text-white px-6 py-2.5 rounded-lg hover:bg-red-700 shadow-md transition-all font-semibold">Wyloguj</button>
          </div>
        </div>
      </div>
    </nav>
  )
}

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
        setSessions(data.sessions || [])
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
    const d = new Date(dateStr)
    return d.toLocaleString(culture === 'pl' ? 'pl-PL' : 'en-US')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-6">
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
              <option value="3">Zakończona</option>
              <option value="4">Anulowana</option>
            </select>
            <input type="date" value={filters.fromDate} onChange={e=>setFilters({...filters, fromDate: e.target.value})} className="border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" placeholder="Od" />
            <input type="date" value={filters.toDate} onChange={e=>setFilters({...filters, toDate: e.target.value})} className="border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" placeholder="Do" />
            <button onClick={loadSessions} className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">Filtruj</button>
          </div>

          {/* Lista */}
          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div> :
           sessions.length === 0 ? <div className="text-center p-8 text-lg text-gray-500">Brak sesji</div> :
           <div className="bg-white rounded-lg shadow-lg divide-y-2 divide-gray-100">
             {sessions.map((s: any) => (
               <div key={s.id} className="p-6 hover:bg-gray-50 cursor-pointer transition-colors" onClick={() => navigate(`/sessions/${s.id}`)}>
                 <div className="flex justify-between items-start">
                   <div>
                     <p className="font-bold text-xl mb-2 text-gray-800">{s.patient.firstName} {s.patient.lastName}</p>
                     <p className="text-gray-600 text-base mb-2">{s.patient.email}</p>
                     <p className="text-gray-700 text-base font-medium">{formatDateTime(s.startDateTime)}</p>
                   </div>
                   <div className="text-right">
                     <span className="px-4 py-2 rounded-lg text-base font-semibold bg-gray-100 text-gray-800">{s.statusId === 1 ? 'Zaplanowana' : s.statusId === 2 ? 'Potwierdzona' : s.statusId === 3 ? 'Zakończona' : 'Anulowana'}</span>
                     <p className="mt-3 font-bold text-lg">{s.price} PLN</p>
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
      <main className="max-w-7xl mx-auto py-8 px-6">
        <div className="space-y-8">
          <div className="flex justify-between items-center">
            <h1>{t['patients.title'] ?? 'Pacjenci'}</h1>
            <button onClick={() => navigate('/patients/new')} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">
              {t['patients.new'] ?? 'Nowy pacjent'}
            </button>
          </div>
          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div> :
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

// Formularz pacjenta
function PatientForm({ id }: { id?: number }) {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [form, setForm] = useState({
    firstName: '', lastName: '', email: '', phone: '', 
    dateOfBirth: '', gender: '', pesel: '',
    street: '', streetNumber: '', apartmentNumber: '', city: '', postalCode: '', country: 'Polska',
    notes: ''
  })
  const [loading, setLoading] = useState(!!id)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
    if (id) loadPatient()
  }, [culture, id, navigate])

  async function loadPatient() {
    try {
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/patients/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const p = await res.json()
        setForm({
          firstName: p.firstName || '', lastName: p.lastName || '', email: p.email || '', phone: p.phone || '',
          dateOfBirth: p.dateOfBirth ? p.dateOfBirth.split('T')[0] : '', gender: p.gender || '', pesel: p.pesel || '',
          street: p.street || '', streetNumber: p.streetNumber || '', apartmentNumber: p.apartmentNumber || '',
          city: p.city || '', postalCode: p.postalCode || '', country: p.country || 'Polska',
          notes: p.notes || ''
        })
      }
    } catch (err) {
      console.error('Error loading patient:', err)
    } finally {
      setLoading(false)
    }
  }

  async function save() {
    setSaving(true)
    try {
      const token = localStorage.getItem('token')
      const url = id ? `/api/patients/${id}` : '/api/patients'
      const method = id ? 'PUT' : 'POST'
      const payload = {
        ...form,
        dateOfBirth: form.dateOfBirth ? new Date(form.dateOfBirth).toISOString() : null
      }
      const res = await fetch(url, {
        method,
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      if (res.ok) navigate('/patients')
      else {
        const error = await res.text()
        alert(`Błąd: ${error}`)
      }
    } catch (err) {
      console.error('Error saving patient:', err)
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

  if (loading) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-4xl mx-auto py-8 px-6">
        <h1>{id ? t['patients.edit'] ?? 'Edytuj pacjenta' : t['patients.new'] ?? 'Nowy pacjent'}</h1>
        <div className="bg-white p-8 rounded-lg shadow-lg space-y-8">
          <div className="form-section">
            <h2>Dane osobowe</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="form-group">
                <label>{t['patients.firstName'] ?? 'Imię'}</label>
                <input value={form.firstName} onChange={e=>setForm({...form, firstName: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" required />
              </div>
              <div className="form-group">
                <label>{t['patients.lastName'] ?? 'Nazwisko'}</label>
                <input value={form.lastName} onChange={e=>setForm({...form, lastName: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" required />
              </div>
              <div className="form-group">
                <label>{t['patients.email'] ?? 'E-mail'}</label>
                <input type="email" value={form.email} onChange={e=>setForm({...form, email: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" required />
              </div>
              <div className="form-group">
                <label>{t['patients.phone'] ?? 'Telefon'}</label>
                <input type="tel" value={form.phone} onChange={e=>setForm({...form, phone: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.dateOfBirth'] ?? 'Data urodzenia'}</label>
                <input type="date" value={form.dateOfBirth} onChange={e=>setForm({...form, dateOfBirth: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.gender'] ?? 'Płeć'}</label>
                <select value={form.gender} onChange={e=>setForm({...form, gender: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200">
                  <option value="">-- Wybierz --</option>
                  <option value="M">Mężczyzna</option>
                  <option value="F">Kobieta</option>
                  <option value="Other">Inna</option>
                </select>
              </div>
              <div className="form-group">
                <label>{t['patients.pesel'] ?? 'PESEL'}</label>
                <input value={form.pesel} onChange={e=>setForm({...form, pesel: e.target.value})} maxLength={11} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
            </div>
          </div>

          <div className="form-section">
            <h2>Dane adresowe</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="md:col-span-2 form-group">
                <label>{t['patients.street'] ?? 'Ulica'}</label>
                <input value={form.street} onChange={e=>setForm({...form, street: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.streetNumber'] ?? 'Numer'}</label>
                <input value={form.streetNumber} onChange={e=>setForm({...form, streetNumber: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.apartmentNumber'] ?? 'Nr lokalu'}</label>
                <input value={form.apartmentNumber} onChange={e=>setForm({...form, apartmentNumber: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.city'] ?? 'Miasto'}</label>
                <input value={form.city} onChange={e=>setForm({...form, city: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.postalCode'] ?? 'Kod pocztowy'}</label>
                <input value={form.postalCode} onChange={e=>setForm({...form, postalCode: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
              <div className="form-group">
                <label>{t['patients.country'] ?? 'Kraj'}</label>
                <input value={form.country} onChange={e=>setForm({...form, country: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" />
              </div>
            </div>
          </div>

          <div className="form-section">
            <label>{t['patients.notes'] ?? 'Notatki'}</label>
            <textarea value={form.notes} onChange={e=>setForm({...form, notes: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" rows={5} />
          </div>

          <div className="flex gap-6 pt-4">
            <button onClick={save} disabled={saving} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 disabled:opacity-50 shadow-md transition-all">
              {t['sessions.save'] ?? 'Zapisz'}
            </button>
            <button onClick={() => navigate('/patients')} className="bg-gray-600 text-white px-8 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all">Anuluj</button>
          </div>
        </div>
      </main>
    </div>
  )
}

// Szczegóły sesji
function SessionDetails() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [session, setSession] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
  }, [culture, id, navigate])

  useEffect(() => {
    if (id) loadSession()
  }, [id])

  async function loadSession() {
    if (!id) return
    try {
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) setSession(await res.json())
    } catch (err) {
      console.error('Error loading session:', err)
    } finally {
      setLoading(false)
    }
  }

  async function sendNotification() {
    if (!id) return
    try {
      const token = localStorage.getItem('token')
      await fetch(`/api/sessions/${id}/send-notification?culture=${culture}`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      })
      alert('Powiadomienie wysłane')
    } catch (err) {
      console.error('Error sending notification:', err)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  if (loading || !session) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-4xl mx-auto py-8 px-6">
        <h1>{t['sessions.details'] ?? 'Szczegóły sesji'}</h1>
        <div className="bg-white p-8 rounded-lg shadow-lg space-y-6">
          <div className="form-group">
            <label className="text-base font-semibold text-gray-700">{t['sessions.patient'] ?? 'Pacjent'}</label>
            <p className="text-lg text-gray-800 mt-2">{session.patient.firstName} {session.patient.lastName}</p>
            <p className="text-base text-gray-600">{session.patient.email}</p>
            {session.patient.phone && <p className="text-base text-gray-600">{session.patient.phone}</p>}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="form-group">
              <label className="text-base font-semibold text-gray-700">{t['sessions.startTime'] ?? 'Data rozpoczęcia'}</label>
              <p className="text-lg text-gray-800 mt-2">{new Date(session.startDateTime).toLocaleString()}</p>
            </div>
            <div className="form-group">
              <label className="text-base font-semibold text-gray-700">{t['sessions.endTime'] ?? 'Data zakończenia'}</label>
              <p className="text-lg text-gray-800 mt-2">{new Date(session.endDateTime).toLocaleString()}</p>
            </div>
            <div className="form-group">
              <label className="text-base font-semibold text-gray-700">{t['sessions.status'] ?? 'Status'}</label>
              <p className="text-lg text-gray-800 mt-2">{session.statusId === 1 ? 'Zaplanowana' : session.statusId === 2 ? 'Potwierdzona' : session.statusId === 3 ? 'Zakończona' : 'Anulowana'}</p>
            </div>
            <div className="form-group">
              <label className="text-base font-semibold text-gray-700">{t['sessions.price'] ?? 'Cena'}</label>
              <p className="text-lg font-bold text-gray-800 mt-2">{session.price} PLN</p>
            </div>
          </div>
          {session.googleMeetLink && (
            <div className="form-group">
              <label className="text-base font-semibold text-gray-700">{t['sessions.googleMeet'] ?? 'Link Google Meet'}</label>
              <a href={session.googleMeetLink} target="_blank" rel="noopener noreferrer" className="text-blue-600 text-lg font-semibold underline block mt-2">{session.googleMeetLink}</a>
            </div>
          )}
          {session.notes && (
            <div className="form-group">
              <label className="text-base font-semibold text-gray-700">{t['sessions.notes'] ?? 'Notatki'}</label>
              <p className="text-base text-gray-800 mt-2 whitespace-pre-wrap">{session.notes}</p>
            </div>
          )}
          <div className="flex gap-6 pt-4">
            <button onClick={() => id && navigate(`/sessions/${id}/edit`)} className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">{t['sessions.edit'] ?? 'Edytuj'}</button>
            <button onClick={sendNotification} className="bg-green-600 text-white px-8 py-3 rounded-lg hover:bg-green-700 shadow-md transition-all font-semibold">{t['sessions.sendNotification'] ?? 'Wyślij powiadomienie'}</button>
            <button onClick={() => navigate('/sessions')} className="bg-gray-600 text-white px-8 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">Powrót</button>
          </div>
        </div>
      </main>
    </div>
  )
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
    if (id) loadSession()
    else setLoading(false)
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
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const s = await res.json()
        const start = new Date(s.startDateTime)
        const duration = Math.round((new Date(s.endDateTime).getTime() - start.getTime()) / 60000)
        setForm({
          patientId: s.patient.id.toString(),
          startDateTime: start.toISOString().slice(0, 16),
          durationMinutes: duration.toString(),
          price: s.price.toString(),
          notes: s.notes || '',
          sendNotification: false
        })
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
      const startDate = new Date(form.startDateTime)
      const payload = {
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

  if (loading) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-4xl mx-auto py-8 px-6">
        <h1>{id ? t['sessions.edit'] ?? 'Edytuj sesję' : t['sessions.new'] ?? 'Nowa sesja'}</h1>
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
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="form-group">
              <label>{t['sessions.startTime'] ?? 'Data rozpoczęcia'} *</label>
              <input type="datetime-local" value={form.startDateTime} onChange={e=>setForm({...form, startDateTime: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200" required />
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
    return new Date(dateStr).toLocaleTimeString(culture === 'pl' ? 'pl-PL' : 'en-US', { hour: '2-digit', minute: '2-digit' })
  }

  function formatDate(date: Date) {
    return date.toLocaleDateString(culture === 'pl' ? 'pl-PL' : 'en-US', { day: 'numeric', month: 'short' })
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

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-6">
        <div className="space-y-8">
          <div className="flex justify-between items-center">
            <h1>Kalendarz tygodniowy</h1>
            <div className="flex gap-4 items-center">
              <button onClick={previousWeek} className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">← Poprzedni</button>
              <button onClick={todayWeek} className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold">Dzisiaj</button>
              <button onClick={nextWeek} className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">Następny →</button>
              <button onClick={() => navigate('/sessions/new')} className="bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 shadow-md transition-all font-semibold">
                {t['sessions.new'] ?? 'Nowa sesja'}
              </button>
            </div>
          </div>

          {loading ? (
            <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div>
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
                  <div key={idx} className="min-h-[500px] p-4 bg-white">
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

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/sessions" element={<SessionsList />} />
        <Route path="/sessions/new" element={<SessionForm />} />
        <Route path="/sessions/:id/edit" element={<SessionForm />} />
        <Route path="/sessions/:id" element={<SessionDetails />} />
        <Route path="/calendar" element={<WeekCalendar />} />
        <Route path="/patients" element={<PatientsList />} />
        <Route path="/patients/new" element={<PatientForm />} />
        <Route path="/patients/:id" element={<PatientFormWithId />} />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
