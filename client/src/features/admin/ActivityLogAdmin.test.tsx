import type { ReactNode } from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest'
import ActivityLogAdmin from './ActivityLogAdmin'

vi.mock('../../components/NavBar', () => ({
  default: ({ children }: { children?: ReactNode }) => <div data-testid="navbar">{children}</div>
}))

vi.mock('../../i18n', () => ({
  loadTranslations: async () => ({})
}))

describe('ActivityLogAdmin', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    localStorage.setItem('token', 'test-token')
    localStorage.setItem('roles', JSON.stringify(['Administrator']))
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    localStorage.clear()
  })

  it('wyświetla logi oraz podsumowanie', async () => {
    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ([
          { id: 'user-a', email: 'user@example.com' }
        ])
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          items: [
            {
              id: 1,
              userId: 'user-a',
              userEmail: 'user@example.com',
              path: '/dashboard',
              startedAtUtc: '2025-01-01T10:00:00Z',
              endedAtUtc: '2025-01-01T10:02:00Z',
              durationSeconds: 120,
              createdAtUtc: '2025-01-01T10:02:00Z'
            }
          ],
          totalCount: 1,
          totalDurationSeconds: 120,
          page: 1,
          pageSize: 100,
          fromUtc: '2025-01-01T00:00:00Z',
          toUtc: '2025-01-02T00:00:00Z'
        })
      })

    render(
      <MemoryRouter>
        <ActivityLogAdmin />
      </MemoryRouter>
    )

    const emailCell = await screen.findByRole('cell', { name: 'user@example.com' })
    expect(emailCell).toBeInTheDocument()
    expect(await screen.findByText('/dashboard')).toBeInTheDocument()
    const durations = await screen.findAllByText(/2 min/i)
    expect(durations.length).toBeGreaterThan(0)
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('wykonuje dodatkowe zapytanie po zmianie filtrowanego użytkownika', async () => {
    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ([
          { id: 'user-a', email: 'user-a@example.com' },
          { id: 'user-b', email: 'user-b@example.com' }
        ])
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          items: [],
          totalCount: 0,
          totalDurationSeconds: 0,
          page: 1,
          pageSize: 100,
          fromUtc: '2025-01-01T00:00:00Z',
          toUtc: '2025-01-02T00:00:00Z'
        })
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          items: [],
          totalCount: 0,
          totalDurationSeconds: 0,
          page: 1,
          pageSize: 100,
          fromUtc: '2025-01-01T00:00:00Z',
          toUtc: '2025-01-02T00:00:00Z'
        })
      })

    render(
      <MemoryRouter>
        <ActivityLogAdmin />
      </MemoryRouter>
    )

    const select = await screen.findByRole('combobox')
    fireEvent.change(select, { target: { value: 'user-b' } })

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(3)
    })

    const callArgs = fetchMock.mock.calls[2]
    expect(callArgs[0]).toContain('userId=user-b')
  })
})
