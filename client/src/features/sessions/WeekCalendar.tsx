import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import { formatTimeWithZone, formatDateWithZone } from './dateTimeUtils'
import { getWeekCalendarLabels } from './weekCalendarLabels'
import NavBar from '../../components/NavBar'

export default function WeekCalendar() {
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

