import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import { Line } from 'react-chartjs-2'
import SessionTranscriptions, { type SessionTranscriptionDto } from './SessionTranscriptions'
import SessionTypeSidebar from './SessionTypeSidebar'
import { formatDateTimeWithZone } from './dateTimeUtils'
import NavBar from '../../components/NavBar'

export default function SessionDetails() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [session, setSession] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [transcriptions, setTranscriptions] = useState<SessionTranscriptionDto[]>([])
  const [selectedPanel, setSelectedPanel] = useState<'details' | 'recording' | 'metrics' | 'summary'>('details')
  const [sessionTypes, setSessionTypes] = useState<any[]>([])
  const [selectedSessionTypeId, setSelectedSessionTypeId] = useState<number | null>(null)
  const [parameters, setParameters] = useState({
    lek: 0,
    smutek: 0,
    zlosc: 0,
    radosc: 0,
    problem1: 0,
    problem2: 0,
    problem3: 0,
    problem4: 0
  })
  const [chartData, setChartData] = useState<any[]>([])
  const [loadingChart, setLoadingChart] = useState(false)
  const [sessionDetails, setSessionDetails] = useState({
    previousWeekEvents: '',
    previousSessionReflections: '',
    personalWorkDiscussion: '',
    therapeuticIntervention: '',
    agreedPersonalWork: '',
    sessionSummary: ''
  })
  const [savingDetails, setSavingDetails] = useState(false)
  const [savingStatus, setSavingStatus] = useState(false)
  useEffect(() => {
    if (!id) return
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    loadTranslations(culture).then(setT)
    loadSessionTypes()
  }, [culture, id, navigate])

  useEffect(() => {
    if (id) loadSession()
  }, [id])

  async function loadSessionTypes() {
    try {
      const token = localStorage.getItem('token')
      if (!token) return
      
      const res = await fetch('/api/sessiontypes/available', { 
        headers: { 'Authorization': `Bearer ${token}` } 
      })
      
      if (res.ok) {
        const data = await res.json()
        console.log('Loaded session types:', data)
        setSessionTypes(Array.isArray(data) ? data : [])
      } else {
        const errorText = await res.text()
        console.error('Error loading session types - status:', res.status, 'response:', errorText)
      }
    } catch (err) {
      console.error('Error loading session types:', err)
    }
  }

  async function loadSession() {
    if (!id) return
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (res.ok) {
        const data = await res.json()
        setSession(data)
        setTranscriptions(Array.isArray(data.transcriptions) ? data.transcriptions : [])
        // Załaduj parametry sesji jeśli istnieją
        if (data.parameters && Array.isArray(data.parameters)) {
          const params: any = {
            lek: 0,
            smutek: 0,
            zlosc: 0,
            radosc: 0,
            problem1: 0,
            problem2: 0,
            problem3: 0,
            problem4: 0
          }
          data.parameters.forEach((p: any) => {
            const name = p.parameterName.toLowerCase()
            if (name === 'lęk') params.lek = p.value
            else if (name === 'smutek') params.smutek = p.value
            else if (name === 'złość') params.zlosc = p.value
            else if (name === 'radość') params.radosc = p.value
            else if (name === 'problem 1') params.problem1 = p.value
            else if (name === 'problem 2') params.problem2 = p.value
            else if (name === 'problem 3') params.problem3 = p.value
            else if (name === 'problem 4') params.problem4 = p.value
          })
          setParameters(params)
        }
        // Załaduj szczegóły sesji
        setSessionDetails({
          previousWeekEvents: data.previousWeekEvents || '',
          previousSessionReflections: data.previousSessionReflections || '',
          personalWorkDiscussion: data.personalWorkDiscussion || '',
          therapeuticIntervention: data.therapeuticIntervention || '',
          agreedPersonalWork: data.agreedPersonalWork || '',
          sessionSummary: data.sessionSummary || ''
        })
        // Załaduj wykres jeśli sesja ma pacjenta
        if (data.patient?.id) {
          await loadChartData(data.patient.id)
        }
      }
    } catch (err) {
      console.error('Error loading session:', err)
    } finally {
      setLoading(false)
    }
  }

  async function loadChartData(patientId: number) {
    try {
      setLoadingChart(true)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/sessions/patient/${patientId}/parameters-chart`, {
        headers: { 'Authorization': `Bearer ${token}` }
      })
      if (res.ok) {
        const data = await res.json()
        setChartData(data)
      }
    } catch (err) {
      console.error('Error loading chart data:', err)
    } finally {
      setLoadingChart(false)
    }
  }

  async function saveParameters() {
    if (!id) return
    try {
      const token = localStorage.getItem('token')
      const params = [
        { parameterName: 'lęk', value: parameters.lek },
        { parameterName: 'smutek', value: parameters.smutek },
        { parameterName: 'złość', value: parameters.zlosc },
        { parameterName: 'radość', value: parameters.radosc },
        { parameterName: 'problem 1', value: parameters.problem1 },
        { parameterName: 'problem 2', value: parameters.problem2 },
        { parameterName: 'problem 3', value: parameters.problem3 },
        { parameterName: 'problem 4', value: parameters.problem4 }
      ]
      const res = await fetch(`/api/sessions/${id}/parameters`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(params)
      })
      if (res.ok) {
        // Odśwież dane sesji i wykresu
        await loadSession()
        alert('Parametry zostały zapisane pomyślnie!')
      } else {
        const errorText = await res.text().catch(() => '')
        alert(`Błąd podczas zapisywania parametrów: ${errorText}`)
      }
    } catch (err) {
      console.error('Error saving parameters:', err)
      alert('Błąd podczas zapisywania parametrów')
    }
  }

  async function saveSessionDetails() {
    if (!id) return
    try {
      setSavingDetails(true)
      const token = localStorage.getItem('token')
      if (!token) return
      
      // Pobierz aktualne dane sesji, aby zachować inne pola
      const currentRes = await fetch(`/api/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
      if (!currentRes.ok) {
        alert('Nie udało się pobrać danych sesji')
        return
      }
      const currentSession = await currentRes.json()
      
      // Oblicz durationMinutes z startDateTime i endDateTime
      const startDate = new Date(currentSession.startDateTime)
      const endDate = new Date(currentSession.endDateTime)
      const durationMinutes = Math.round((endDate.getTime() - startDate.getTime()) / 60000)
      
      const payload = {
        startDateTime: currentSession.startDateTime,
        durationMinutes: durationMinutes,
        price: currentSession.price,
        notes: currentSession.notes || null,
        previousWeekEvents: sessionDetails.previousWeekEvents || null,
        previousSessionReflections: sessionDetails.previousSessionReflections || null,
        personalWorkDiscussion: sessionDetails.personalWorkDiscussion || null,
        therapeuticIntervention: sessionDetails.therapeuticIntervention || null,
        agreedPersonalWork: sessionDetails.agreedPersonalWork || null,
        sessionSummary: sessionDetails.sessionSummary || null,
        sessionTypeId: currentSession.sessionTypeId || null
      }
      
      const res = await fetch(`/api/sessions/${id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      })
      
      if (res.ok) {
        await loadSession()
        alert('Szczegóły sesji zostały zapisane pomyślnie!')
      } else {
        const errorText = await res.text().catch(() => '')
        alert(`Błąd podczas zapisywania szczegółów sesji: ${errorText}`)
      }
    } catch (err) {
      console.error('Error saving session details:', err)
      alert('Błąd podczas zapisywania szczegółów sesji')
    } finally {
      setSavingDetails(false)
    }
  }

  const loadTranscriptions = useCallback(async () => {
    if (!id) return
    try {
      const token = localStorage.getItem('token')
      if (!token) { navigate('/login'); return }
      const res = await fetch(`/api/sessions/${id}/transcriptions`, {
        headers: { 'Authorization': `Bearer ${token}` }
      })
      if (res.ok) {
        const data = await res.json()
        setTranscriptions(Array.isArray(data) ? data : [])
      }
    } catch (err) {
      console.error('Error loading transcriptions:', err)
    }
  }, [id, navigate])

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

  if (loading || !session) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  const sessionToken = localStorage.getItem('token') ?? ''

  // Renderowanie zawartości panelu
  const renderPanelContent = () => {
    switch (selectedPanel) {
      case 'details':
        return (
          <div className="bg-white border border-gray-200 rounded-lg p-6 shadow-sm space-y-6">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-2xl font-bold text-gray-800">{t['sessions.details'] ?? 'Szczegóły sesji'}</h2>
              <div className="flex gap-3">
                <button onClick={() => id && navigate(`/sessions/${id}/edit`)} className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 shadow-md transition-all font-semibold text-sm">{t['sessions.edit'] ?? 'Edytuj'}</button>
                <button onClick={sendNotification} className="bg-green-600 text-white px-6 py-2 rounded-lg hover:bg-green-700 shadow-md transition-all font-semibold text-sm">{t['sessions.sendNotification'] ?? 'Wyślij powiadomienie'}</button>
              </div>
            </div>
            {session.googleMeetLink && (
              <div className="form-group">
                <label className="text-base font-semibold text-gray-700">{t['sessions.googleMeet'] ?? 'Link Google Meet'}</label>
                <a href={session.googleMeetLink} target="_blank" rel="noopener noreferrer" className="text-blue-600 text-lg font-semibold underline block mt-2">{session.googleMeetLink}</a>
              </div>
            )}
            
            {/* Sekcja szczegółów sesji */}
            <div className="mt-8 space-y-6">
              <h3 className="text-xl font-bold text-gray-800 border-b-2 border-purple-200 pb-2">Szczegóły sesji</h3>
              
              {/* Sekcja 1: Wydarzenia z poprzedniego tygodnia */}
              <div className="bg-purple-50 rounded-lg p-4">
                <label className="block text-base font-semibold text-gray-700 mb-2">
                  Wydarzenia z poprzedniego tygodnia
                </label>
                <textarea
                  value={sessionDetails.previousWeekEvents}
                  onChange={e => setSessionDetails({ ...sessionDetails, previousWeekEvents: e.target.value })}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-purple-500 focus:ring-2 focus:ring-purple-200 px-4 py-2 min-h-[120px] resize-y bg-white"
                  placeholder="Pytanie o wydarzenia z poprzedniego tygodnia..."
                  rows={5}
                />
              </div>

              {/* Sekcja 2: Refleksje po poprzedniej sesji */}
              <div className="bg-purple-50 rounded-lg p-4">
                <label className="block text-base font-semibold text-gray-700 mb-2">
                 Refleksje po poprzedniej sesji
                </label>
                <textarea
                  value={sessionDetails.previousSessionReflections}
                  onChange={e => setSessionDetails({ ...sessionDetails, previousSessionReflections: e.target.value })}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-purple-500 focus:ring-2 focus:ring-purple-200 px-4 py-2 min-h-[120px] resize-y bg-white"
                  placeholder="Omówienie refleksji po poprzedniej sesji..."
                  rows={5}
                />
              </div>

              {/* Sekcja 3: Omówienie pracy własnej */}
              <div className="bg-purple-50 rounded-lg p-4">
                <label className="block text-base font-semibold text-gray-700 mb-2">
                Omówienie pracy własnej
                </label>
                <textarea
                  value={sessionDetails.personalWorkDiscussion}
                  onChange={e => setSessionDetails({ ...sessionDetails, personalWorkDiscussion: e.target.value })}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-purple-500 focus:ring-2 focus:ring-purple-200 px-4 py-2 min-h-[120px] resize-y bg-white"
                  placeholder="Omówienie pracy własnej..."
                  rows={5}
                />
              </div>

              {/* Sekcja 4: Interwencja terapeutyczna */}
              <div className="bg-purple-50 rounded-lg p-4">
                <label className="block text-base font-semibold text-gray-700 mb-2">
                  Interwencja terapeutyczna
                </label>
                <textarea
                  value={sessionDetails.therapeuticIntervention}
                  onChange={e => setSessionDetails({ ...sessionDetails, therapeuticIntervention: e.target.value })}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-purple-500 focus:ring-2 focus:ring-purple-200 px-4 py-2 min-h-[120px] resize-y bg-white"
                  placeholder="Interwencja terapeutyczna..."
                  rows={5}
                />
              </div>

              {/* Sekcja 5: Ustalona praca osobista */}
              <div className="bg-purple-50 rounded-lg p-4">
                <label className="block text-base font-semibold text-gray-700 mb-2">
                  Ustalona praca osobista
                </label>
                <textarea
                  value={sessionDetails.agreedPersonalWork}
                  onChange={e => setSessionDetails({ ...sessionDetails, agreedPersonalWork: e.target.value })}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-purple-500 focus:ring-2 focus:ring-purple-200 px-4 py-2 min-h-[120px] resize-y bg-white"
                  placeholder="Praca osobista wynikająca z pracy na sesji..."
                  rows={5}
                />
              </div>

              {/* Sekcja 6: Podsumowanie sesji */}
              <div className="bg-purple-50 rounded-lg p-4">
                <label className="block text-base font-semibold text-gray-700 mb-2">
                  Podsumowanie sesji
                </label>
                <textarea
                  value={sessionDetails.sessionSummary}
                  onChange={e => setSessionDetails({ ...sessionDetails, sessionSummary: e.target.value })}
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-purple-500 focus:ring-2 focus:ring-purple-200 px-4 py-2 min-h-[120px] resize-y bg-white"
                  placeholder="Podsumowanie - co pacjent bierze dla siebie z tej sesji..."
                  rows={5}
                />
              </div>

              {/* Przycisk zapisu */}
              <div className="flex justify-end">
                <button
                  onClick={saveSessionDetails}
                  disabled={savingDetails}
                  className="bg-purple-600 text-white px-6 py-2 rounded-lg hover:bg-purple-700 shadow-md transition-all font-semibold disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {savingDetails ? 'Zapisywanie...' : 'Zapisz szczegóły sesji'}
                </button>
              </div>
            </div>
          </div>
        )
      case 'recording':
        return (
          <div className="bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
            <h2 className="text-2xl font-bold text-gray-800 mb-4">{t['sessions.recording'] ?? 'Nagrywanie i transkrypcje'}</h2>
            {sessionToken && (
              <SessionTranscriptions
                sessionId={Number(id)}
                token={sessionToken}
                items={transcriptions}
                onRefresh={loadTranscriptions}
                t={t}
              />
            )}
          </div>
        )
      case 'metrics':
        return (
          <div className="bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
            <h2 className="text-2xl font-bold text-gray-800 mb-4">{t['sessions.metrics'] ?? 'Metryki sesji'}</h2>
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-gray-50 p-4 rounded-lg">
                  <label className="text-sm font-semibold text-gray-700 block mb-2">{t['sessions.metrics.duration'] ?? 'Czas trwania'}</label>
                  <p className="text-lg text-gray-800">
                    {session.startDateTime && session.endDateTime 
                      ? `${Math.round((new Date(session.endDateTime).getTime() - new Date(session.startDateTime).getTime()) / 60000)} min`
                      : '-'}
                  </p>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <label className="text-sm font-semibold text-gray-700 block mb-2">{t['sessions.metrics.transcriptions'] ?? 'Liczba transkrypcji'}</label>
                  <p className="text-lg text-gray-800">{transcriptions.length}</p>
                </div>
              </div>

              {/* Formularz parametrów */}
              <div className="mt-6">
                <h3 className="text-lg font-semibold text-gray-800 mb-4">Parametry sesji (0-10)</h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Lęk (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.lek}
                      onChange={e => setParameters({ ...parameters, lek: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Smutek (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.smutek}
                      onChange={e => setParameters({ ...parameters, smutek: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Złość (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.zlosc}
                      onChange={e => setParameters({ ...parameters, zlosc: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Radość (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.radosc}
                      onChange={e => setParameters({ ...parameters, radosc: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Problem 1 (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.problem1}
                      onChange={e => setParameters({ ...parameters, problem1: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Problem 2 (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.problem2}
                      onChange={e => setParameters({ ...parameters, problem2: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Problem 3 (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.problem3}
                      onChange={e => setParameters({ ...parameters, problem3: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-semibold text-gray-700 block mb-2">Problem 4 (0-10)</label>
                    <input
                      type="number"
                      min="0"
                      max="10"
                      value={parameters.problem4}
                      onChange={e => setParameters({ ...parameters, problem4: Number(e.target.value) })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2"
                    />
                  </div>
                </div>
                
                <button
                  onClick={saveParameters}
                  className="bg-green-600 text-white px-6 py-2 rounded-lg hover:bg-green-700 shadow-md transition-all mb-6"
                >
                  Zapisz parametry
                </button>
              </div>

              {/* Wykres parametrów */}
              {session.patient?.id && (
                <div className="mt-6">
                  <h3 className="text-lg font-semibold text-gray-800 mb-4">Wykres parametrów pacjenta</h3>
                  {loadingChart ? (
                    <div className="text-center py-8">Ładowanie wykresu...</div>
                  ) : chartData.length > 0 ? (
                    <div style={{ height: '400px' }}>
                      <Line
                        data={{
                          labels: chartData.map((d: any) => `Sesja ${d.weekNumber || d.WeekNumber}`),
                          datasets: [
                            {
                              label: 'Lęk',
                              data: chartData.map((d: any) => d.lek || d.Lek),
                              borderColor: 'rgb(239, 68, 68)',
                              backgroundColor: 'rgba(239, 68, 68, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Smutek',
                              data: chartData.map((d: any) => d.depresja || d.Depresja),
                              borderColor: 'rgb(59, 130, 246)',
                              backgroundColor: 'rgba(59, 130, 246, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Złość',
                              data: chartData.map((d: any) => d.samopoczucie || d.Samopoczucie),
                              borderColor: 'rgb(245, 158, 11)',
                              backgroundColor: 'rgba(245, 158, 11, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Radość',
                              data: chartData.map((d: any) => d.skalaBecka || d.SkalaBecka),
                              borderColor: 'rgb(34, 197, 94)',
                              backgroundColor: 'rgba(34, 197, 94, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Problem 1',
                              data: chartData.map((d: any) => d.problem1 || d.Problem1),
                              borderColor: 'rgb(168, 85, 247)',
                              backgroundColor: 'rgba(168, 85, 247, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Problem 2',
                              data: chartData.map((d: any) => d.problem2 || d.Problem2),
                              borderColor: 'rgb(236, 72, 153)',
                              backgroundColor: 'rgba(236, 72, 153, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Problem 3',
                              data: chartData.map((d: any) => d.problem3 || d.Problem3),
                              borderColor: 'rgb(14, 165, 233)',
                              backgroundColor: 'rgba(14, 165, 233, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            },
                            {
                              label: 'Problem 4',
                              data: chartData.map((d: any) => d.problem4 || d.Problem4),
                              borderColor: 'rgb(251, 146, 60)',
                              backgroundColor: 'rgba(251, 146, 60, 0.1)',
                              borderWidth: 2,
                              fill: false,
                              tension: 0.35,
                              pointRadius: 4,
                              pointHoverRadius: 6,
                              spanGaps: true
                            }
                          ]
                        }}
                        options={{
                          responsive: true,
                          maintainAspectRatio: false,
                          scales: {
                            y: {
                              beginAtZero: true,
                              max: 10
                            }
                          }
                        }}
                      />
                    </div>
                  ) : (
                    <div className="text-center py-8 text-gray-500">
                      Brak danych do wyświetlenia. Zapisz parametry dla tej sesji, aby zobaczyć wykres.
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        )
      case 'summary':
        return (
          <div className="bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
            <h2 className="text-2xl font-bold text-gray-800 mb-4">{t['sessions.summary'] ?? 'Podsumowanie sesji'}</h2>
            <div className="space-y-4">
              <div className="form-group">
                <label className="text-base font-semibold text-gray-700 block mb-2">{t['sessions.notes'] ?? 'Notatki z sesji'}</label>
                {session.notes ? (
                  <div className="bg-gray-50 p-4 rounded-lg">
                    <p className="text-base text-gray-800 whitespace-pre-wrap">{session.notes}</p>
                  </div>
                ) : (
                  <p className="text-gray-500 italic">{t['sessions.notes.empty'] ?? 'Brak notatek'}</p>
                )}
              </div>
              <div className="form-group">
                <label className="text-base font-semibold text-gray-700 block mb-2">{t['sessions.summary.content'] ?? 'Podsumowanie'}</label>
                <textarea
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 min-h-[300px]"
                  placeholder={t['sessions.summary.placeholder'] ?? 'Wprowadź podsumowanie sesji, kluczowe ustalenia, zadania domowe, itp...'}
                  defaultValue={session.summary || ''}
                />
              </div>
            </div>
          </div>
        )
      default:
        return null
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="flex justify-between items-center mb-6">
          <h1>{t['sessions.details'] ?? 'Szczegóły sesji'}</h1>
          <button onClick={() => navigate('/sessions')} className="bg-gray-600 text-white px-6 py-2 rounded-lg hover:bg-gray-700 shadow-md transition-all font-semibold">Powrót</button>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Lewa kolumna - Sidebar z listą paneli i informacjami */}
          <div className="lg:col-span-2 xl:col-span-2">
            <div className="space-y-4 sticky top-4">
              {/* Informacje o sesji */}
              <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
                <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">Informacje o sesji</h3>
                <div className="space-y-3 text-sm">
                  <div>
                    <p className="text-gray-500 font-medium">Pacjent</p>
                    <p className="text-gray-800 font-semibold">{session.patient.firstName} {session.patient.lastName}</p>
                    {session.patient.email && (
                      <p className="text-gray-600 text-xs mt-1">{session.patient.email}</p>
                    )}
                  </div>
                  <div>
                    <p className="text-gray-500 font-medium">Data</p>
                    <p className="text-gray-800">{formatDateTimeWithZone(session.startDateTime, culture)}</p>
                  </div>
                  <div>
                    <p className="text-gray-500 font-medium">Cena</p>
                    <p className="text-gray-800 font-semibold">{session.price} PLN</p>
                  </div>
                  {session.statusId && (
                    <div>
                      <p className="text-gray-500 font-medium mb-1">Status</p>
                      <select
                        value={session.statusId}
                        onChange={async (e) => {
                          const newStatusId = Number(e.target.value)
                          if (!id) return
                          try {
                            setSavingStatus(true)
                            const token = localStorage.getItem('token')
                            if (!token) return
                            
                            // Pobierz aktualne dane sesji
                            const currentRes = await fetch(`/api/sessions/${id}`, { headers: { 'Authorization': `Bearer ${token}` } })
                            if (!currentRes.ok) {
                              alert('Nie udało się pobrać danych sesji')
                              return
                            }
                            const currentSession = await currentRes.json()
                            
                            // Oblicz durationMinutes
                            const startDate = new Date(currentSession.startDateTime)
                            const endDate = new Date(currentSession.endDateTime)
                            const durationMinutes = Math.round((endDate.getTime() - startDate.getTime()) / 60000)
                            
                            const payload = {
                              startDateTime: currentSession.startDateTime,
                              durationMinutes: durationMinutes,
                              price: currentSession.price,
                              notes: currentSession.notes || null,
                              previousWeekEvents: currentSession.previousWeekEvents || null,
                              previousSessionReflections: currentSession.previousSessionReflections || null,
                              personalWorkDiscussion: currentSession.personalWorkDiscussion || null,
                              therapeuticIntervention: currentSession.therapeuticIntervention || null,
                              agreedPersonalWork: currentSession.agreedPersonalWork || null,
                              sessionSummary: currentSession.sessionSummary || null,
                              sessionTypeId: currentSession.sessionTypeId || null,
                              statusId: newStatusId
                            }
                            
                            const res = await fetch(`/api/sessions/${id}`, {
                              method: 'PUT',
                              headers: {
                                'Authorization': `Bearer ${token}`,
                                'Content-Type': 'application/json'
                              },
                              body: JSON.stringify(payload)
                            })
                            
                            if (res.ok) {
                              await loadSession()
                            } else {
                              const errorText = await res.text().catch(() => '')
                              alert(`Błąd podczas zmiany statusu: ${errorText}`)
                              // Przywróć poprzednią wartość
                              e.target.value = session.statusId.toString()
                            }
                          } catch (err) {
                            console.error('Error updating session status:', err)
                            alert('Błąd podczas zmiany statusu')
                            // Przywróć poprzednią wartość
                            e.target.value = session.statusId.toString()
                          } finally {
                            setSavingStatus(false)
                          }
                        }}
                        disabled={savingStatus}
                        className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-1.5 text-sm bg-white disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        <option value={1}>Zaplanowana</option>
                        <option value={2}>Potwierdzona</option>
                        <option value={3}>Zakończona</option>
                        <option value={4}>Anulowana</option>
                      </select>
                      {savingStatus && (
                        <p className="text-xs text-gray-500 mt-1">Zapisywanie...</p>
                      )}
                    </div>
                  )}
                  <div>
                    <p className="text-gray-500 font-medium">Status płatności</p>
                    {session.payment ? (
                      <div className="mt-1">
                        {session.isPaid ? (
                          <div className="bg-green-50 border border-green-200 rounded p-2">
                            <p className="text-green-800 font-semibold text-xs">✓ Opłacona</p>
                            {session.payment.completedAt && (
                              <p className="text-xs text-green-700 mt-1">
                                {formatDateTimeWithZone(session.payment.completedAt, culture)}
                              </p>
                            )}
                            <p className="text-xs text-green-700">
                              {session.payment.amount} PLN
                            </p>
                          </div>
                        ) : (
                          <div className="bg-yellow-50 border border-yellow-200 rounded p-2">
                            <p className="text-yellow-800 font-semibold text-xs">⚠ Nieopłacona</p>
                            {session.paymentDelayDays !== null && session.paymentDelayDays !== undefined && (
                              <p className="text-xs text-yellow-700 mt-1">
                                Opóźnienie: {session.paymentDelayDays} {session.paymentDelayDays === 1 ? 'dzień' : 'dni'}
                              </p>
                            )}
                            <p className="text-xs text-yellow-700">
                              {session.payment.amount} PLN
                            </p>
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="mt-1 bg-red-50 border border-red-200 rounded p-2">
                        <p className="text-red-800 font-semibold text-xs">Nieopłacona</p>
                        {session.paymentDelayDays !== null && session.paymentDelayDays !== undefined && (
                          <p className="text-xs text-red-700 mt-1">
                            Opóźnienie: {session.paymentDelayDays} {session.paymentDelayDays === 1 ? 'dzień' : 'dni'}
                          </p>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* Menu paneli */}
              <div className="bg-gray-50 border border-gray-200 rounded-lg p-2 space-y-1">
                <div className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                  Panele sesji
                </div>
                <button
                  onClick={() => setSelectedPanel('details')}
                  className={`w-full text-left px-3 py-2.5 rounded-md transition-all text-sm font-medium ${
                    selectedPanel === 'details'
                      ? 'bg-blue-600 text-white shadow-md'
                      : 'text-gray-700 hover:bg-gray-200 hover:text-gray-900'
                  }`}
                >
                  {t['sessions.details'] ?? 'Szczegóły sesji'}
                </button>
                <button
                  onClick={() => setSelectedPanel('recording')}
                  className={`w-full text-left px-3 py-2.5 rounded-md transition-all text-sm font-medium ${
                    selectedPanel === 'recording'
                      ? 'bg-blue-600 text-white shadow-md'
                      : 'text-gray-700 hover:bg-gray-200 hover:text-gray-900'
                  }`}
                >
                  {t['sessions.recording'] ?? 'Nagrywanie i transkrypcje'}
                  {transcriptions.length > 0 && (
                    <span className={`ml-2 text-xs ${selectedPanel === 'recording' ? 'text-blue-100' : 'text-green-600'}`}>
                      ({transcriptions.length})
                    </span>
                  )}
                </button>
                <button
                  onClick={() => setSelectedPanel('metrics')}
                  className={`w-full text-left px-3 py-2.5 rounded-md transition-all text-sm font-medium ${
                    selectedPanel === 'metrics'
                      ? 'bg-blue-600 text-white shadow-md'
                      : 'text-gray-700 hover:bg-gray-200 hover:text-gray-900'
                  }`}
                >
                  {t['sessions.metrics'] ?? 'Metryki sesji'}
                </button>
                <button
                  onClick={() => setSelectedPanel('summary')}
                  className={`w-full text-left px-3 py-2.5 rounded-md transition-all text-sm font-medium ${
                    selectedPanel === 'summary'
                      ? 'bg-blue-600 text-white shadow-md'
                      : 'text-gray-700 hover:bg-gray-200 hover:text-gray-900'
                  }`}
                >
                  {t['sessions.summary'] ?? 'Podsumowanie sesji'}
                  {session.notes && (
                    <span className={`ml-2 text-xs ${selectedPanel === 'summary' ? 'text-blue-100' : 'text-green-600'}`}>
                      â—Ź
                    </span>
                  )}
                </button>
              </div>
            </div>
          </div>

          {/* Środkowa kolumna - Zawartość wybranego panelu */}
          <div className="lg:col-span-7">
            {renderPanelContent()}
          </div>

          {/* Prawa kolumna - Lista typów sesji, Pytania i Podpowiedzi */}
          <div className="lg:col-span-3">
            <div className="space-y-4 sticky top-4">
              {/* Wybór typu sesji */}
              <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
                <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">Typ sesji</h3>
                <select 
                  value={selectedSessionTypeId || ''} 
                  onChange={e => setSelectedSessionTypeId(e.target.value ? Number(e.target.value) : null)} 
                  className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-3 py-2 text-sm"
                >
                  <option value="">-- Wybierz typ sesji --</option>
                  {sessionTypes && sessionTypes.length > 0 ? (
                    sessionTypes.map(st => (
                      <option key={st.id} value={st.id.toString()}>{st.name}</option>
                    ))
                  ) : (
                    <option disabled>Brak dostępnych typów sesji</option>
                  )}
                </select>
                {sessionTypes.length === 0 && (
                  <p className="text-xs text-gray-500 mt-2 italic">
                    {sessionTypes.length === 0 ? 'Brak dostępnych typów sesji. Sprawdź konsolę przeglądarki (F12) aby zobaczyć szczegóły.' : 'Ładowanie typów sesji...'}
                  </p>
                )}
              </div>
              
              <SessionTypeSidebar
                token={sessionToken}
                sessionTypeId={selectedSessionTypeId}
              />
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}
