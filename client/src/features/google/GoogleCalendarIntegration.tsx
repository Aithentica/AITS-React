import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

interface GoogleCalendarStatus {
  connected: boolean
  expiresAt?: string
  scope?: string
}

interface MessageState {
  type: 'success' | 'error'
  text: string
}

interface GoogleCalendarIntegrationProps {
  returnUrl?: string
  className?: string
  showInstructions?: boolean
}

async function parseJsonResponse<T>(response: Response): Promise<T> {
  const text = await response.text()
  if (!text) {
    return {} as T
  }
  try {
    return JSON.parse(text) as T
  } catch {
    throw new Error('Niepoprawna odpowiedź serwera')
  }
}

export function GoogleCalendarIntegrationForm({
  returnUrl,
  className,
  showInstructions = true
}: GoogleCalendarIntegrationProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const [status, setStatus] = useState<GoogleCalendarStatus | null>(null)
  const [statusLoading, setStatusLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [callbackUri, setCallbackUri] = useState('')
  const [message, setMessage] = useState<MessageState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const effectiveReturnUrl = useMemo(() => {
    const target = returnUrl && returnUrl.startsWith('http')
      ? returnUrl
      : `${window.location.origin}${returnUrl ?? location.pathname}`
    try {
      return new URL(target).toString()
    } catch {
      return `${window.location.origin}${location.pathname}`
    }
  }, [returnUrl, location.pathname])

  const loadStatus = useCallback(async () => {
    const token = localStorage.getItem('token')
    if (!token) {
      setError('Brak tokenu autoryzacji. Zaloguj się ponownie.')
      setStatus(null)
      setStatusLoading(false)
      return
    }

    setStatusLoading(true)
    setError(null)
    try {
      const response = await fetch('/api/integrations/google-calendar/status', {
        headers: {
          Authorization: `Bearer ${token}`
        }
      })
      if (!response.ok) {
        throw new Error(`Status HTTP ${response.status}`)
      }
      const payload = await parseJsonResponse<GoogleCalendarStatus>(response)
      setStatus(payload)
    } catch (err) {
      console.error('Nie udało się pobrać statusu integracji Google Calendar:', err)
      setError('Nie udało się pobrać statusu integracji. Spróbuj ponownie później.')
      setStatus(null)
    } finally {
      setStatusLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadStatus()
  }, [loadStatus])

  useEffect(() => {
    const params = new URLSearchParams(location.search)
    const integrationState = params.get('googleCalendar')
    if (!integrationState) {
      return
    }

    if (integrationState === 'connected') {
      setMessage({ type: 'success', text: 'Połączenie z Google Calendar zostało poprawnie skonfigurowane.' })
    } else {
      const msg = params.get('message')
      setMessage({ type: 'error', text: msg ?? 'Nie udało się połączyć z Google Calendar.' })
    }

    params.delete('googleCalendar')
    params.delete('message')
    const newQuery = params.toString()
    navigate(`${location.pathname}${newQuery ? `?${newQuery}` : ''}`, { replace: true })

    void loadStatus()
  }, [location.pathname, location.search, navigate, loadStatus])

  async function startIntegration(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const token = localStorage.getItem('token')
    if (!token) {
      setError('Brak tokenu autoryzacji. Zaloguj się ponownie.')
      return
    }

    setActionLoading(true)
    setMessage(null)
    setError(null)
    try {
      const params = new URLSearchParams({ returnUrl: effectiveReturnUrl })
      const trimmedCallback = callbackUri.trim()
      if (trimmedCallback) {
        params.set('callbackUri', trimmedCallback)
      }

      const response = await fetch(`/api/integrations/google-calendar/login?${params.toString()}`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      })

      if (!response.ok) {
        const payload = await parseJsonResponse<{ error?: string }>(response)
        throw new Error(payload.error ?? `Status HTTP ${response.status}`)
      }

      const payload = await parseJsonResponse<{ authorizationUrl?: string }>(response)
      if (!payload.authorizationUrl) {
        throw new Error('Serwer nie zwrócił adresu autoryzacji Google.')
      }
      window.location.assign(payload.authorizationUrl)
    } catch (err) {
      console.error('Błąd inicjowania integracji Google Calendar:', err)
      setMessage({ type: 'error', text: err instanceof Error ? err.message : 'Nie udało się zainicjować połączenia z Google Calendar.' })
    } finally {
      setActionLoading(false)
    }
  }

  async function disconnect() {
    const token = localStorage.getItem('token')
    if (!token) {
      setError('Brak tokenu autoryzacji. Zaloguj się ponownie.')
      return
    }

    setActionLoading(true)
    setMessage(null)
    setError(null)
    try {
      const response = await fetch('/api/integrations/google-calendar/disconnect', {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`
        }
      })
      if (!response.ok) {
        const payload = await parseJsonResponse<{ error?: string }>(response)
        throw new Error(payload.error ?? `Status HTTP ${response.status}`)
      }

      setMessage({ type: 'success', text: 'Połączenie z Google Calendar zostało usunięte.' })
      await loadStatus()
    } catch (err) {
      console.error('Błąd rozłączania integracji Google Calendar:', err)
      setMessage({ type: 'error', text: err instanceof Error ? err.message : 'Nie udało się usunąć połączenia z Google Calendar.' })
    } finally {
      setActionLoading(false)
    }
  }

  return (
    <div className={className}>
      {message && (
        <div
          role="alert"
          className={`mb-4 rounded-md border px-4 py-3 text-sm ${
            message.type === 'success'
              ? 'border-green-200 bg-green-50 text-green-800'
              : 'border-red-200 bg-red-50 text-red-800'
          }`}
        >
          {message.text}
        </div>
      )}

      {error && (
        <div role="alert" className="mb-4 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {error}
        </div>
      )}

      <div className="space-y-4">
        <div className="rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-700">
          {statusLoading ? (
            <span>Sprawdzanie statusu połączenia...</span>
          ) : status?.connected ? (
            <div className="space-y-2">
              <p className="font-semibold text-green-700">Połączenie aktywne</p>
              {status.expiresAt && (
                <p>Token dostępu wygaśnie: {new Date(status.expiresAt).toLocaleString('pl-PL')}</p>
              )}
              {status.scope && (
                <p>Zakres uprawnień: <span className="font-mono break-all">{status.scope}</span></p>
              )}
              {!status.expiresAt && <p>Token będzie automatycznie odświeżany przy każdym użyciu.</p>}
            </div>
          ) : (
            <div>
              <p className="font-semibold text-blue-700">Brak aktywnego połączenia</p>
              <p>Kliknij „Połącz z Google”, aby autoryzować kalendarz terapeuty.</p>
            </div>
          )}
        </div>

        <form className="space-y-4" onSubmit={startIntegration}>
          <div>
            <label htmlFor="callbackUri" className="block text-sm font-medium text-gray-700">
              Własny adres callback (opcjonalnie)
            </label>
            <input
              id="callbackUri"
              type="url"
              placeholder="http://localhost:5106/api/integrations/google-calendar/callback"
              value={callbackUri}
              onChange={e => setCallbackUri(e.target.value)}
              className="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
              disabled={actionLoading}
            />
            <p className="mt-1 text-xs text-gray-500">Pozostaw puste, aby użyć domyślnego adresu z konfiguracji.</p>
          </div>

          <div className="flex flex-wrap gap-3">
            <button
              type="submit"
              className="rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
              disabled={actionLoading}
            >
              {status?.connected ? 'Ponownie autoryzuj Google' : 'Połącz z Google'}
            </button>
            {status?.connected && (
              <button
                type="button"
                onClick={disconnect}
                className="rounded-md bg-red-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-red-700 disabled:cursor-not-allowed disabled:bg-red-300"
                disabled={actionLoading}
              >
                Odłącz kalendarz
              </button>
            )}
            <button
              type="button"
              onClick={() => void loadStatus()}
              className="rounded-md border border-gray-300 px-4 py-2 text-sm font-semibold text-gray-700 shadow-sm transition hover:bg-gray-50 disabled:cursor-not-allowed"
              disabled={statusLoading || actionLoading}
            >
              Odśwież status
            </button>
          </div>
        </form>

        {showInstructions && (
          <div className="rounded-md border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-900">
            <h3 className="mb-2 font-semibold">Jak to działa?</h3>
            <ol className="list-decimal space-y-1 pl-5">
              <li>Zaloguj się na konto terapeuty i upewnij się, że masz dostęp do kalendarza wskazanego w konfiguracji.</li>
              <li>Kliknij „Połącz z Google” i potwierdź zgody na dostęp do kalendarza oraz Google Meet.</li>
              <li>Po zakończeniu autoryzacji wrócisz do panelu terapeuty z informacją o stanie połączenia.</li>
              <li>W każdej chwili możesz odłączyć kalendarz lub ponownie wykonać autoryzację.</li>
            </ol>
          </div>
        )}
      </div>
    </div>
  )
}

export default GoogleCalendarIntegrationForm

