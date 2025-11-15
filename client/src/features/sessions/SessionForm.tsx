import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import { ensureFullHour, formatForDateTimeLocalInput } from './dateTimeUtils'
import NavBar from '../../components/NavBar'

export default function SessionForm() {
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

  if (loading) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-8">
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

