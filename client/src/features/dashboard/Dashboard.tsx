import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import NavBar from '../../components/NavBar'
import { formatTimeWithZone, formatDateTimeWithZone } from '../sessions/dateTimeUtils'
import type { Session } from '../../types/sessions'
import PatientMetricsDashboardChart from './PatientMetricsDashboardChart'
import PatientTasksList from './PatientTasksList'
import PatientDiariesList from './PatientDiariesList'

const Roles = {
  Administrator: 'Administrator',
  Terapeuta: 'Terapeuta',
  Pacjent: 'Pacjent'
} as const

export default function Dashboard() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [roles, setRoles] = useState<string[]>([])
  const [rolesLoaded, setRolesLoaded] = useState(false)
  const [todaySessions, setTodaySessions] = useState<Session[]>([])
  const [patientSessions, setPatientSessions] = useState<Session[]>([])
  const [loading, setLoading] = useState(true)
  const [stats, setStats] = useState({ today: 0, scheduled: 0, completed: 0 })
  const [error, setError] = useState<string | null>(null)
  const [processingPayments, setProcessingPayments] = useState<Record<number, boolean>>({})

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      // Użyj window.location.href zamiast navigate() aby uniknąć pętli przekierowań
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
      console.warn('Brak ról w localStorage')
      setRoles([])
    }
    setRolesLoaded(true)
  }, [])

  useEffect(() => {
    loadTranslations(culture).then(setT).catch(err => console.error('Error loading translations:', err))
  }, [culture])

  useEffect(() => {
    if (!rolesLoaded) return
    
    // Jeśli nie ma żadnych ról, nie próbuj załadować danych - pokaż błąd
    if (roles.length === 0) {
      console.error('Brak ról w localStorage. Użytkownik może nie być poprawnie zalogowany.')
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
    // Jeśli użytkownik ma inną rolę (terapeuta), załaduj dane terapeuty
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
          // Użyj window.location.href zamiast navigate() aby uniknąć pętli przekierowań
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
          
          // Sortuj sesje po dacie (najnowsze pierwsze)
          const sortedSessions = [...sessionsArray].sort((a: Session, b: Session) => {
            return new Date(b.startDateTime).getTime() - new Date(a.startDateTime).getTime()
          })
          
          setPatientSessions(sortedSessions)
          
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
          setPatientSessions([])
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
          // Użyj window.location.href zamiast navigate() aby uniknąć pętli przekierowań
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
              {t['admin.dashboard.subtitle'] ?? 'Zarządzaj użytkownikami systemu oraz profilami terapeutów. Wybierz moduł, aby wyświetlić szczegóły i wykonać operacje administracyjne.'}
            </p>
          </section>

          <section className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            <button
              onClick={() => navigate('/admin/users')}
              className="text-left bg-white border border-gray-200 rounded-xl p-6 shadow-md hover:shadow-lg transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <h2 className="text-2xl font-semibold text-gray-900 mb-2">{t['admin.dashboard.users'] ?? 'Zarządzanie użytkownikami'}</h2>
              <p className="text-sm text-gray-600">
                {t['admin.dashboard.users.desc'] ?? 'Dodawaj nowych użytkowników, aktualizuj role oraz blokuj dostępy do aplikacji.'}
              </p>
            </button>

            <button
              onClick={() => navigate('/admin/therapists')}
              className="text-left bg-white border border-gray-200 rounded-xl p-6 shadow-md hover:shadow-lg transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <h2 className="text-2xl font-semibold text-gray-900 mb-2">{t['admin.dashboard.therapists'] ?? 'Zarządzanie terapeutami'}</h2>
              <p className="text-sm text-gray-600">
                {t['admin.dashboard.therapists.desc'] ?? 'Przypisuj role terapeuty, nadaj dostęp typu Free Access oraz kontroluj aktywność profili.'}
              </p>
            </button>

            <button
              onClick={() => navigate('/admin/activity')}
              className="text-left bg-white border border-gray-200 rounded-xl p-6 shadow-md hover:shadow-lg transition-all focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <h2 className="text-2xl font-semibold text-gray-900 mb-2">{t['admin.dashboard.activity'] ?? 'Aktywność użytkowników'}</h2>
              <p className="text-sm text-gray-600">
                {t['admin.dashboard.activity.desc'] ?? 'Analizuj, jakie widoki odwiedzają użytkownicy oraz ile czasu na nich spędzają.'}
              </p>
            </button>
          </section>
        </main>
      </div>
    )
  }

  // Dashboard dla pacjenta
  if (isPatient) {
    const token = localStorage.getItem('token')
    if (!token) {
      return null
    }

    return (
      <div className="min-h-screen bg-gray-50">
        <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
        <main className="w-full py-8 px-8">
          <div className="space-y-8">
            <header className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900">{t['dashboard.title'] ?? 'Kokpit'}</h1>
              <p className="text-gray-600 mt-2">
                {t['dashboard.patient.subtitle'] ?? 'Twoje sesje, metryki, zadania i dzienniczki'}
              </p>
            </header>

            {/* Sekcja z listą sesji */}
            <div className="bg-white rounded-lg shadow-lg">
              <div className="p-6 border-b-2 border-gray-200 flex justify-between items-center">
                <h2 className="text-2xl font-bold">{t['dashboard.patient.sessions'] ?? 'Moje sesje'}</h2>
                <button 
                  onClick={() => navigate('/sessions')} 
                  className="text-blue-600 hover:text-blue-800 font-semibold text-base px-4 py-2 rounded-lg hover:bg-blue-50 transition-colors"
                >
                  {t['dashboard.viewAll'] ?? 'Zobacz wszystkie'}
                </button>
              </div>
              {loading ? (
                <div className="p-8 text-center text-gray-500 text-lg">Ładowanie...</div>
              ) : error ? (
                <div className="p-8 text-center text-red-600 text-lg font-semibold">{error}</div>
              ) : patientSessions.length === 0 ? (
                <div className="p-8 text-center">
                  <p className="text-gray-600 text-lg">{t['dashboard.patient.noSessions'] ?? 'Brak zaplanowanych sesji'}</p>
                </div>
              ) : (
                <div className="divide-y-2 divide-gray-100">
                  {patientSessions.slice(0, 5).map(session => (
                    <div 
                      key={session.id} 
                      className="p-6 hover:bg-gray-50 transition-colors" 
                    >
                      <div className="flex justify-between items-start">
                        <div className="flex-1 cursor-pointer" onClick={() => navigate(`/sessions/${session.id}`)}>
                          <p className="font-bold text-xl mb-2 text-gray-800">
                            {formatDateTimeWithZone(session.startDateTime, culture)}
                          </p>
                          <p className="text-gray-600 text-base mb-2">
                            {formatTimeWithZone(session.startDateTime, culture)} - {formatTimeWithZone(session.endDateTime, culture)}
                          </p>
                          {/* Status płatności */}
                          <div className="mt-2">
                            {session.isPaid ? (
                              <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold bg-green-100 text-green-800">
                                ✓ Opłacona
                              </span>
                            ) : (
                              <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold bg-yellow-100 text-yellow-800">
                                ⚠ Nieopłacona
                              </span>
                            )}
                          </div>
                        </div>
                        <div className="text-right ml-4">
                          <span className={`px-4 py-2 rounded-lg text-base font-semibold ${
                            session.statusId === 2 ? 'bg-green-100 text-green-800' :
                            session.statusId === 3 ? 'bg-blue-100 text-blue-800' :
                            session.statusId === 4 ? 'bg-red-100 text-red-800' :
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {getStatusName(session.statusId)}
                          </span>
                          {session.googleMeetLink && (
                            <a 
                              href={session.googleMeetLink} 
                              target="_blank" 
                              rel="noopener noreferrer" 
                              className="block mt-3 text-blue-600 hover:text-blue-800 text-base font-semibold underline"
                              onClick={(e) => e.stopPropagation()}
                            >
                              Google Meet
                            </a>
                          )}
                          {/* Przycisk płatności */}
                          {!session.isPaid && (session.canPay !== false) && (
                            <button
                              onClick={(e) => initiatePayment(session.id, e)}
                              disabled={processingPayments[session.id]}
                              className="mt-3 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              {processingPayments[session.id] ? 'Przetwarzanie...' : `Zapłać ${session.price} PLN`}
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Sekcja z wykresem metryk */}
            <PatientMetricsDashboardChart token={token} />

            {/* Sekcja z zadaniami */}
            <PatientTasksList token={token} />

            {/* Sekcja z dzienniczkami */}
            <PatientDiariesList token={token} />
          </div>
        </main>
      </div>
    )
  }

  function formatTime(dateStr: string) {
    return formatTimeWithZone(dateStr, culture)
  }

  function getStatusName(statusId: number) {
    // TODO: Pobieranie z tłumaczeń enumów
    const statusNames: Record<number, string> = { 1: 'Zaplanowana', 2: 'Potwierdzona', 3: 'Zakończona', 4: 'Anulowana' }
    return statusNames[statusId] || 'Nieznany'
  }

  async function initiatePayment(sessionId: number, e: React.MouseEvent) {
    e.stopPropagation()
    try {
      setProcessingPayments(prev => ({ ...prev, [sessionId]: true }))
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/patient/sessions/${sessionId}/initiate-payment`, {
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
      setProcessingPayments(prev => ({ ...prev, [sessionId]: false }))
    }
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
                <p className="text-gray-600 text-lg mb-6">{t['dashboard.noSessionsToday'] ?? noSessionsFallback}</p>
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

