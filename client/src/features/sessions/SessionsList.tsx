import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import NavBar from '../../components/NavBar'
import { formatDateTimeWithZone } from './dateTimeUtils'

export default function SessionsList() {
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
                     <span className="px-4 py-2 rounded-lg text-base font-semibold bg-gray-100 text-gray-800">{s.statusId === 1 ? 'Zaplanowana' : s.statusId === 2 ? 'Potwierdzona' : s.statusId === 3 ? 'Zakończona' : 'Anulowana'}</span>
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

