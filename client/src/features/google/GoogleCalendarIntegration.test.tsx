import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import GoogleCalendarIntegrationForm from './GoogleCalendarIntegration'

function buildJsonResponse(data: unknown, init?: { ok?: boolean }) {
  return {
    ok: init?.ok ?? true,
    text: async () => (data === undefined ? '' : JSON.stringify(data))
  } as unknown as Response
}

describe('GoogleCalendarIntegrationForm', () => {
  const originalFetch = globalThis.fetch
  const originalLocation = window.location

  beforeEach(() => {
    localStorage.setItem('token', 'test-token')
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: {
        ...originalLocation,
        assign: vi.fn()
      }
    })
  })

  afterEach(() => {
    localStorage.clear()
    if (originalFetch) {
      globalThis.fetch = originalFetch
    }
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: originalLocation
    })
    vi.restoreAllMocks()
  })

  it('inicjuje proces połączenia z Google Calendar', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)

    fetchMock.mockResolvedValueOnce(buildJsonResponse({ connected: false }))
    fetchMock.mockResolvedValueOnce(buildJsonResponse({ authorizationUrl: 'https://accounts.google.com/o/oauth2/auth' }))

    render(
      <MemoryRouter initialEntries={['/integrations/google-calendar']}>
        <GoogleCalendarIntegrationForm returnUrl="/integrations/google-calendar" showInstructions={false} />
      </MemoryRouter>
    )

    await screen.findByText(/Brak aktywnego połączenia/i)

    const connectButton = screen.getByRole('button', { name: /Połącz z Google/i })
    fireEvent.click(connectButton)

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2)
    })

    expect(fetchMock.mock.calls[1]?.[0].toString()).toContain('/api/integrations/google-calendar/login?')
    expect((fetchMock.mock.calls[1]?.[1] as RequestInit).headers).toMatchObject({ Authorization: 'Bearer test-token' })
    expect(window.location.assign).toHaveBeenCalledWith('https://accounts.google.com/o/oauth2/auth')
  })

  it('rozłącza zintegrowany kalendarz i odświeża status', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)

    fetchMock
      .mockResolvedValueOnce(buildJsonResponse({ connected: true, expiresAt: '2025-01-01T10:00:00Z', scope: 'calendar events' }))
      .mockResolvedValueOnce(buildJsonResponse({ disconnected: true }))
      .mockResolvedValueOnce(buildJsonResponse({ connected: false }))

    render(
      <MemoryRouter initialEntries={['/integrations/google-calendar']}>
        <GoogleCalendarIntegrationForm returnUrl="/integrations/google-calendar" showInstructions={false} />
      </MemoryRouter>
    )

    await screen.findByText(/Połączenie aktywne/i)

    fireEvent.click(screen.getByRole('button', { name: /Odłącz kalendarz/i }))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(3)
      expect(screen.getByRole('alert')).toHaveTextContent(/Połączenie z Google Calendar zostało usunięte/i)
    })
  })
})

