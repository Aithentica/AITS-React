import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import NavBar from '../../components/NavBar'
import GoogleCalendarIntegrationForm from './GoogleCalendarIntegration'

export default function GoogleCalendarIntegrationPage() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }
    loadTranslations(culture).then(setT).catch(err => console.error('Error loading translations:', err))
  }, [culture, navigate])

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="max-w-7xl mx-auto py-8 px-8">
        <div className="space-y-8">
          <div className="bg-white rounded-lg shadow-lg p-8 space-y-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">{t['integrations.googleCalendar.title'] ?? 'Integracja z Google Calendar'}</h1>
              <p className="mt-2 text-base text-gray-600">
                {t['integrations.googleCalendar.description'] ?? 'Połącz swoje konto terapeuty z Google Calendar, aby automatycznie tworzyć wydarzenia i linki Google Meet dla sesji.'}
              </p>
            </div>
            <GoogleCalendarIntegrationForm returnUrl="/integrations/google-calendar" />
          </div>

          <div className="bg-white rounded-lg shadow-lg p-8 space-y-4">
            <h2 className="text-2xl font-bold text-gray-900">{t['integrations.googleCalendar.helpTitle'] ?? 'Najczęstsze problemy'}</h2>
            <ul className="list-disc space-y-2 pl-6 text-sm text-gray-700">
              <li>{t['integrations.googleCalendar.help.scope'] ?? 'Upewnij się, że podczas autoryzacji zaznaczasz wszystkie wymagane zakresy (kalendarz i Google Meet). Bez nich wydarzenia nie zostaną utworzone.'}</li>
              <li>{t['integrations.googleCalendar.help.calendarAccess'] ?? 'Konto terapeuty musi mieć prawo zapisu do kalendarza skonfigurowanego w aplikacji. Jeśli pojawia się błąd uprawnień, sprawdź ustawienia kalendarza Google.'}</li>
              <li>{t['integrations.googleCalendar.help.refresh'] ?? 'W razie utraty połączenia odłącz kalendarz i wykonaj ponowną autoryzację. Pamiętaj, aby przeprowadzać ją po każdej zmianie hasła w Google.'}</li>
            </ul>
          </div>
        </div>
      </main>
    </div>
  )
}

