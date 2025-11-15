import { useEffect, useState } from 'react'
import { loadTranslations, type Translations } from '../i18n'

export type NavBarProps = {
  culture: 'pl' | 'en'
  setCulture: (culture: 'pl' | 'en') => void
  onLogout: () => void
  navigate: (path: string) => void
}

const NavBar = ({ culture, setCulture, onLogout, navigate }: NavBarProps) => {
  const [translations, setTranslations] = useState<Translations>({})
  const [roles, setRoles] = useState<string[]>([])

  useEffect(() => {
    loadTranslations(culture).then(setTranslations).catch(() => setTranslations({}))
  }, [culture])

  useEffect(() => {
    const storedRoles = localStorage.getItem('roles')
    if (!storedRoles) {
      setRoles([])
      return
    }
    try {
      const parsed = JSON.parse(storedRoles)
      if (Array.isArray(parsed)) setRoles(parsed)
      else setRoles([])
    } catch {
      setRoles([])
    }
  }, [])

  const isAdmin = roles.includes('Administrator')
  const isTherapist = roles.includes('Terapeuta') || roles.includes('TerapeutaFreeAccess')
  const isPatient = roles.includes('Pacjent')

  return (
    <nav className="bg-white shadow-md">
      <div className="w-full px-8 py-4">
        <div className="flex justify-between items-center">
          <div className="flex items-center gap-6">
            <button
              onClick={() => navigate('/dashboard')}
              className="text-xl font-bold text-gray-800 hover:text-blue-600 transition-colors"
            >
              {translations['dashboard.title'] ?? 'Kokpit'}
            </button>
            {isAdmin ? (
              <>
                <button
                  onClick={() => navigate('/admin/users')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['admin.nav.users'] ?? 'Użytkownicy'}
                </button>
                <button
                  onClick={() => navigate('/admin/therapists')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['admin.nav.therapists'] ?? 'Terapeuci'}
                </button>
                <button
                  onClick={() => navigate('/admin/activity')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['admin.nav.activity'] ?? 'Aktywność'}
                </button>
                <button
                  onClick={() => navigate('/session-types')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['sessionTypes.nav'] ?? 'Typy sesji'}
                </button>
              </>
            ) : isPatient ? (
              <>
                <button
                  onClick={() => navigate('/patient/sessions')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['patient.sessions.title'] ?? 'Moje sesje'}
                </button>
              </>
            ) : (
              <>
                <button
                  onClick={() => navigate('/sessions')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['sessions.title'] ?? 'Sesje'}
                </button>
                <button
                  onClick={() => navigate('/calendar')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['calendar.weekly'] ?? 'Kalendarz'}
                </button>
                <button
                  onClick={() => navigate('/integrations/google-calendar')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['integrations.googleCalendar.nav'] ?? 'Integracje'}
                </button>
                <button
                  onClick={() => navigate('/patients')}
                  className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  {translations['patients.title'] ?? 'Pacjenci'}
                </button>
                {isTherapist && (
                  <>
                    <button
                      onClick={() => navigate('/therapist/profile')}
                      className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                    >
                      {translations['therapist.profile'] ?? 'Mój profil'}
                    </button>
                    <button
                      onClick={() => navigate('/therapist/documents')}
                      className="text-base font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                    >
                      {translations['therapist.documents'] ?? 'Dokumenty'}
                    </button>
                  </>
                )}
              </>
            )}
          </div>
          <div className="flex items-center gap-4">
            <select
              value={culture}
              onChange={e => setCulture(e.target.value as 'pl' | 'en')}
              className="border-2 border-gray-300 rounded-lg px-4 py-2 text-base focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
            >
              <option value="pl">PL</option>
              <option value="en">EN</option>
            </select>
            <button
              onClick={onLogout}
              className="bg-red-600 text-white px-6 py-2.5 rounded-lg hover:bg-red-700 shadow-md transition-all font-semibold"
            >
              {translations['common.logout'] ?? 'Wyloguj'}
            </button>
          </div>
        </div>
      </div>
    </nav>
  )
}

export default NavBar

