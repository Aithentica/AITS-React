import type { ReactNode } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi, afterEach, beforeEach } from 'vitest'
import TherapistsAdmin from './TherapistsAdmin'

vi.mock('../../components/NavBar', () => ({
  default: ({ children }: { children?: ReactNode }) => <div data-testid="navbar">{children}</div>
}))

vi.mock('../../i18n', () => ({
  loadTranslations: async () => ({})
}))

describe('TherapistsAdmin', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    localStorage.setItem('token', 'test-token')
    localStorage.setItem('roles', JSON.stringify(['Administrator']))
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    localStorage.clear()
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('wyświetla listę terapeutów', async () => {
    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ([{ userId: '1', email: 'therapist@example.com', roles: ['Terapeuta'], freeAccess: false }])
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ([{ id: '2', email: 'user@example.com', roles: ['Pacjent'], isLockedOut: false }])
      })

    render(
      <MemoryRouter>
        <TherapistsAdmin />
      </MemoryRouter>
    )

    expect(await screen.findByText('therapist@example.com')).toBeInTheDocument()
  })

  it('pozwala wypełnić formularz przypisania', async () => {
    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ([])
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ([{ id: '2', email: 'user2@example.com', roles: ['Pacjent'], isLockedOut: false }])
      })

    render(
      <MemoryRouter>
        <TherapistsAdmin />
      </MemoryRouter>
    )

    const select = await screen.findByLabelText(/Wybierz użytkownika/i)
    fireEvent.change(select, { target: { value: '2' } })
    expect((select as HTMLSelectElement).value).toBe('2')
  })
})


