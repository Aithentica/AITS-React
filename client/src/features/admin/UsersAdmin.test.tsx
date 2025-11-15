import type { ReactNode } from 'react'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi, afterEach, beforeEach } from 'vitest'
import UsersAdmin from './UsersAdmin'

vi.mock('../../components/NavBar', () => ({
  default: ({ children }: { children?: ReactNode }) => <div data-testid="navbar">{children}</div>
}))

vi.mock('../../i18n', () => ({
  loadTranslations: async () => ({})
}))

describe('UsersAdmin', () => {
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

  it('renderuje listę użytkowników', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ([
        { id: '1', email: 'admin@example.com', roles: ['Administrator'], isLockedOut: false }
      ])
    })

    render(
      <MemoryRouter>
        <UsersAdmin />
      </MemoryRouter>
    )

    expect(await screen.findByText('admin@example.com')).toBeInTheDocument()
  })

  it('pozwala na przełączenie trybu edycji ról', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ([
        { id: '1', email: 'user@example.com', roles: ['Pacjent'], isLockedOut: false }
      ])
    })

    render(
      <MemoryRouter>
        <UsersAdmin />
      </MemoryRouter>
    )

    await screen.findByText('user@example.com')
    fireEvent.click(screen.getByText(/Edytuj role/i))

    const row = screen.getByText('user@example.com').closest('tr')
    expect(row).not.toBeNull()

    await waitFor(() => {
      expect(within(row as HTMLTableRowElement).getByLabelText(/Administrator/)).toBeInTheDocument()
    })
  })
})


