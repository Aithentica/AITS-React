import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import NavBar from '../../components/NavBar'

export default function PatientsList() {
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

