import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import App from './App'

vi.mock('./i18n', () => ({
  loadTranslations: async (culture: string) => {
    if (culture === 'en') {
      return {
        'dashboard.noSessionsToday': 'No sessions today'
      }
    }
    return {
      'login.title': 'Logowanie',
      'login.email': 'E-mail',
      'login.password': 'Hasło',
      'login.submit': 'Zaloguj',
      'dashboard.noSessionsToday': 'Brak sesji na dzisiaj'
    }
  }
}))

afterEach(() => {
  localStorage.clear()
  vi.restoreAllMocks()
})

it('renderuje formularz logowania', async () => {
  // Upewnij się, że localStorage jest czysty i ustaw ścieżkę na /login
  localStorage.clear()
  window.history.pushState({}, '', '/login')
  
  render(<App />)
  // Sprawdź czy tytuł formularza jest widoczny
  expect(await screen.findByText('Logowanie')).toBeInTheDocument()
  expect(screen.getByText('E-mail')).toBeInTheDocument()
})


it('nie pokazuje formularza integracji na dashboardzie', async () => {
  localStorage.setItem('token', 'test-token')
  localStorage.setItem('roles', JSON.stringify(['Terapeuta']))
  window.history.pushState({}, '', '/dashboard')

  const fetchMock = vi.fn((input: RequestInfo) => {
    if (typeof input === 'string' && input === '/api/sessions/today') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => []
      } as Response)
    }
    if (typeof input === 'string' && input.startsWith('/api/sessions?page=1&pageSize=1000')) {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => ({ sessions: [] })
      } as Response)
    }
    return Promise.resolve({
      ok: true,
      status: 200,
      json: async () => ({})
    } as Response)
  })
  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  await screen.findByText(/Sesje dzisiaj/i)
  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))

  expect(screen.queryByText(/Integracja z Google Calendar/i)).not.toBeInTheDocument()
})

it('wyświetla tłumaczenie braku sesji w języku angielskim', async () => {
  localStorage.setItem('token', 'test-token')
  localStorage.setItem('roles', JSON.stringify(['Terapeuta']))
  window.history.pushState({}, '', '/dashboard')

  const fetchMock = vi.fn((input: RequestInfo) => {
    if (typeof input === 'string' && input === '/api/sessions/today') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => []
      } as Response)
    }
    if (typeof input === 'string' && input.startsWith('/api/sessions?page=1&pageSize=1000')) {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => ({ sessions: [] })
      } as Response)
    }
    return Promise.resolve({
      ok: true,
      status: 200,
      json: async () => ({})
    } as Response)
  })
  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  await screen.findByText(/Sesje dzisiaj/i)
  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))

  const cultureSelect = screen.getByDisplayValue('PL')
  fireEvent.change(cultureSelect, { target: { value: 'en' } })

  expect(await screen.findByText('No sessions today')).toBeInTheDocument()
  expect(screen.queryByText('Brak sesji na dzisiaj')).not.toBeInTheDocument()
})


it('sortuje listę sesji malejąco po dacie rozpoczęcia', async () => {
  localStorage.setItem('token', 'test-token')
  localStorage.setItem('roles', JSON.stringify(['Terapeuta']))
  window.history.pushState({}, '', '/sessions')

  const sessions = {
    sessions: [
      {
        id: 1,
        patient: { firstName: 'Adam', lastName: 'Starszy', email: 'adam@test.com' },
        startDateTime: '2025-01-10T10:00:00Z',
        endDateTime: '2025-01-10T11:00:00Z',
        statusId: 1,
        price: 100
      },
      {
        id: 2,
        patient: { firstName: 'Ewa', lastName: 'Nowsza', email: 'ewa@test.com' },
        startDateTime: '2025-05-15T12:00:00Z',
        endDateTime: '2025-05-15T13:00:00Z',
        statusId: 2,
        price: 200
      }
    ]
  }

  const fetchMock = vi.fn((input: RequestInfo) => {
    if (typeof input === 'string' && input.startsWith('/api/sessions')) {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => sessions
      } as unknown as Response)
    }

    if (typeof input === 'string' && input === '/api/activity') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: async () => ({})
      } as unknown as Response)
    }

    return Promise.resolve({
      ok: true,
      status: 200,
      json: async () => ({})
    } as unknown as Response)
  })

  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  const items = await screen.findAllByTestId('session-item')
  expect(items).toHaveLength(2)

  expect(within(items[0]).getByText('Ewa Nowsza')).toBeInTheDocument()
  expect(within(items[1]).getByText('Adam Starszy')).toBeInTheDocument()
})




