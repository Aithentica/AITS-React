import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { formatDateTimeWithZone } from '../sessions/dateTimeUtils'
import NavBar from '../../components/NavBar'

export default function PatientSessionDetails() {
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

  if (loading || !session) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

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
                      {processingPayment ? 'Przetwarzanie...' : 'Zapłać przez Tpay'}
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
                  Otwórz Google Meet
                </a>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}

