import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import NavBar from '../../components/NavBar'
import { loadTranslations, type Translations } from '../../i18n'
import { ensureFullHour, formatDateTimeWithZone, formatForDateTimeLocalInput } from '../sessions/dateTimeUtils'

type AdminUserSummary = {
  id: string
  email: string
  userName?: string | null
}

type ActivityLogItem = {
  id: number
  userId: string
  userEmail?: string | null
  path: string
  startedAtUtc: string
  endedAtUtc: string
  durationSeconds: number
  createdAtUtc: string
}

type ActivityLogPageDto = {
  items: ActivityLogItem[]
  totalCount: number
  totalDurationSeconds: number
  page: number
  pageSize: number
  fromUtc: string
  toUtc: string
}

const formatDuration = (seconds: number, culture: 'pl' | 'en') => {
  if (seconds <= 0) return culture === 'pl' ? '0 s' : '0 s'
  const hrs = Math.floor(seconds / 3600)
  const mins = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60
  const parts: string[] = []
  if (hrs > 0) parts.push(`${hrs} ${culture === 'pl' ? 'h' : 'h'}`)
  if (mins > 0) parts.push(`${mins} ${culture === 'pl' ? 'min' : 'min'}`)
  if (secs > 0 || parts.length === 0) parts.push(`${secs} ${culture === 'pl' ? 's' : 's'}`)
  return parts.join(' ')
}

const formatDateTime = (value: string, culture: 'pl' | 'en') => {
  return formatDateTimeWithZone(value, culture)
}

const getDefaultFrom = () => {
  const date = new Date()
  date.setMinutes(0, 0, 0)
  date.setDate(date.getDate() - 7)
  return ensureFullHour(formatForDateTimeLocalInput(date))
}

const getDefaultTo = () => {
  const date = new Date()
  date.setMinutes(0, 0, 0)
  return ensureFullHour(formatForDateTimeLocalInput(date))
}

const ActivityLogAdmin = () => {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl' | 'en'>('pl')
  const [translations, setTranslations] = useState<Translations>({})
  const [users, setUsers] = useState<AdminUserSummary[]>([])
  const [selectedUserId, setSelectedUserId] = useState<string>('')
  const [fromFilter, setFromFilter] = useState<string>(getDefaultFrom)
  const [toFilter, setToFilter] = useState<string>(getDefaultTo)
  const [logs, setLogs] = useState<ActivityLogItem[]>([])
  const [summary, setSummary] = useState({ totalCount: 0, totalDurationSeconds: 0 })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadTranslations(culture).then(setTranslations).catch(() => setTranslations({}))
  }, [culture])

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }

    const rolesRaw = localStorage.getItem('roles')
    if (rolesRaw) {
      try {
        const roles = JSON.parse(rolesRaw)
        if (!Array.isArray(roles) || !roles.includes('Administrator')) {
          navigate('/dashboard')
          return
        }
      } catch {
        navigate('/dashboard')
        return
      }
    }

    void loadUsers(token)
  }, [navigate])

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) return
    void loadLogs(token)
  }, [selectedUserId, fromFilter, toFilter])

  const loadUsers = async (token: string) => {
    try {
      const response = await fetch('/api/admin/users', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }
      const payload = await response.json()
      const mapped: AdminUserSummary[] = Array.isArray(payload)
        ? payload.map((item: any) => ({
            id: item.id,
            email: item.email ?? item.userName ?? item.id,
            userName: item.userName ?? null
          }))
        : []
      setUsers(mapped)
    } catch (err) {
      console.error('Nie udało się pobrać listy użytkowników', err)
      setUsers([])
    }
  }

  const loadLogs = async (token: string) => {
    try {
      setLoading(true)
      setError(null)
      const params = new URLSearchParams({ page: '1', pageSize: '100' })
      if (selectedUserId) params.append('userId', selectedUserId)
      const normalizedFrom = ensureFullHour(fromFilter)
      const normalizedTo = ensureFullHour(toFilter)
      if (normalizedFrom) params.append('fromUtc', new Date(normalizedFrom).toISOString())
      if (normalizedTo) params.append('toUtc', new Date(normalizedTo).toISOString())

      const response = await fetch(`/api/admin/activity?${params.toString()}`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }

      const payload: ActivityLogPageDto = await response.json()
      setLogs(Array.isArray(payload.items) ? payload.items : [])
      setSummary({
        totalCount: payload.totalCount ?? 0,
        totalDurationSeconds: payload.totalDurationSeconds ?? 0
      })
    } catch (err) {
      console.error('Błąd pobierania logów aktywności', err)
      setError(culture === 'pl' ? 'Nie udało się pobrać logów aktywności.' : 'Failed to load activity logs.')
      setLogs([])
      setSummary({ totalCount: 0, totalDurationSeconds: 0 })
    } finally {
      setLoading(false)
    }
  }

  const handleRefresh = () => {
    const token = localStorage.getItem('token')
    if (!token) return
    void loadLogs(token)
  }

  const totalDurationLabel = useMemo(
    () => formatDuration(summary.totalDurationSeconds, culture),
    [summary.totalDurationSeconds, culture]
  )

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={() => {
        localStorage.removeItem('token')
        localStorage.removeItem('roles')
        navigate('/login')
      }} navigate={navigate} />

      <main className="w-full py-8 px-8 space-y-8">
        <header className="flex flex-col gap-2">
          <h1 className="text-3xl font-bold text-gray-900">{translations['admin.activity.title'] ?? 'Aktywność użytkowników'}</h1>
          <p className="text-gray-600">
            {translations['admin.activity.subtitle'] ?? 'Analizuj stronice odwiedzane przez użytkowników oraz czas, jaki na nich spędzają.'}
          </p>
        </header>

        <section className="bg-white rounded-lg shadow p-6 space-y-4">
          <h2 className="text-xl font-semibold text-gray-800">{translations['admin.activity.filters'] ?? 'Filtry'}</h2>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <select
              value={selectedUserId}
              onChange={event => setSelectedUserId(event.target.value)}
              className="border-2 border-gray-200 rounded-lg px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
            >
              <option value="">{translations['admin.activity.filters.allUsers'] ?? 'Wszyscy użytkownicy'}</option>
              {users.map(user => (
                <option key={user.id} value={user.id}>
                  {user.email}
                </option>
              ))}
            </select>
            <div className="grid grid-cols-2 gap-2">
              <input
                type="date"
                value={fromFilter.split('T')[0] || ''}
                onChange={e => {
                  const timePart = fromFilter.split('T')[1] || '00:00'
                  setFromFilter(`${e.target.value}T${timePart}`)
                }}
                className="border-2 border-gray-200 rounded-lg px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              />
              <input
                type="time"
                value={fromFilter.split('T')[1]?.substring(0, 5) || '00:00'}
                step={3600}
                onChange={e => {
                  const datePart = fromFilter.split('T')[0] || ''
                  setFromFilter(ensureFullHour(`${datePart}T${e.target.value}`))
                }}
                className="border-2 border-gray-200 rounded-lg px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              />
            </div>
            <div className="grid grid-cols-2 gap-2">
              <input
                type="date"
                value={toFilter.split('T')[0] || ''}
                onChange={e => {
                  const timePart = toFilter.split('T')[1] || '00:00'
                  setToFilter(`${e.target.value}T${timePart}`)
                }}
                className="border-2 border-gray-200 rounded-lg px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              />
              <input
                type="time"
                value={toFilter.split('T')[1]?.substring(0, 5) || '00:00'}
                step={3600}
                onChange={e => {
                  const datePart = toFilter.split('T')[0] || ''
                  setToFilter(ensureFullHour(`${datePart}T${e.target.value}`))
                }}
                className="border-2 border-gray-200 rounded-lg px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              />
            </div>
            <button
              onClick={handleRefresh}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg font-semibold hover:bg-blue-700 shadow transition-colors"
            >
              {translations['common.refresh'] ?? 'Odśwież'}
            </button>
          </div>
        </section>

        <section className="bg-white rounded-lg shadow">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 border-b border-gray-200 px-6 py-4">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">{translations['admin.activity.summary'] ?? 'Podsumowanie'}</h2>
              <p className="text-gray-600 text-sm">
                {translations['admin.activity.summaryInfo'] ?? 'Łącznie wyświetleń w wybranym okresie oraz przybliżony czas spędzony na stronach.'}
              </p>
            </div>
            <div className="flex gap-6">
              <div className="text-center">
                <p className="text-sm text-gray-500">{translations['admin.activity.totalViews'] ?? 'Liczba odwiedzin'}</p>
                <p className="text-2xl font-bold text-gray-900">{summary.totalCount}</p>
              </div>
              <div className="text-center">
                <p className="text-sm text-gray-500">{translations['admin.activity.totalDuration'] ?? 'Łączny czas'}</p>
                <p className="text-2xl font-bold text-gray-900">{totalDurationLabel}</p>
              </div>
            </div>
          </div>

          {error && <div className="px-6 py-4 text-red-600 font-semibold">{error}</div>}

          {loading ? (
            <div className="px-6 py-6 text-gray-500">{translations['common.loading'] ?? 'Ładowanie...'}</div>
          ) : logs.length === 0 ? (
            <div className="px-6 py-6 text-gray-500">{translations['admin.activity.empty'] ?? 'Brak danych dla wybranych filtrów.'}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">{translations['admin.activity.table.user'] ?? 'Użytkownik'}</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">{translations['admin.activity.table.path'] ?? 'Ścieżka'}</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">{translations['admin.activity.table.started'] ?? 'Rozpoczęto'}</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">{translations['admin.activity.table.duration'] ?? 'Czas spędzony'}</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {logs.map(item => (
                    <tr key={item.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{item.userEmail ?? item.userId}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-blue-600 font-medium">{item.path}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">{formatDateTime(item.startedAtUtc, culture)}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">{formatDuration(item.durationSeconds, culture)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </main>
    </div>
  )
}

export default ActivityLogAdmin
