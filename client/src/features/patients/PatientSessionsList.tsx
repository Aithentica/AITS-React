import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { formatDateTimeWithZone } from '../sessions/dateTimeUtils'
import NavBar from '../../components/NavBar'

export default function PatientSessionsList() {
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

          {loading ? <div className="text-center p-8 text-lg text-gray-500">Ładowanie...</div> :
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

