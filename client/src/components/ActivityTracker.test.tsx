import { useEffect } from 'react'
import { MemoryRouter, Route, Routes, useNavigate } from 'react-router-dom'
import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest'
import { act } from 'react'
import ActivityTracker from './ActivityTracker'

const NAVIGATION_DELAY_MS = 600

const NavigateOnce = () => {
  const navigate = useNavigate()

  useEffect(() => {
    const timer = setTimeout(() => {
      navigate('/second')
    }, NAVIGATION_DELAY_MS)
    return () => clearTimeout(timer)
  }, [navigate])

  return <div>first</div>
}

describe('ActivityTracker', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    localStorage.setItem('token', 'test-token')
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockResolvedValue({ ok: true })
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    localStorage.clear()
  })

  it('wysyła log przy zmianie ścieżki', async () => {
    render(
      <MemoryRouter initialEntries={['/first']}>
        <ActivityTracker endpoint="/api/activity" />
        <Routes>
          <Route path="/first" element={<NavigateOnce />} />
          <Route path="/second" element={<div>second</div>} />
        </Routes>
      </MemoryRouter>
    )

    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, NAVIGATION_DELAY_MS + 100))
    })
    await waitFor(() => {
      expect(screen.getByText('second')).toBeInTheDocument()
    })

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled()
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/activity')
    expect(init?.method).toBe('POST')
    expect(init?.headers).toMatchObject({ Authorization: 'Bearer test-token' })
    const payload = JSON.parse(init?.body as string)
    expect(payload.path).toBe('/first')
    expect(typeof payload.startedAtUtc).toBe('string')
    expect(typeof payload.endedAtUtc).toBe('string')
  })

  it('nie wysyła logów gdy brak tokenu', async () => {
    localStorage.clear()
    fetchMock.mockClear()

    render(
      <MemoryRouter initialEntries={['/first']}>
        <ActivityTracker endpoint="/api/activity" />
        <Routes>
          <Route path="/first" element={<NavigateOnce />} />
          <Route path="/second" element={<div>second</div>} />
        </Routes>
      </MemoryRouter>
    )

    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, NAVIGATION_DELAY_MS + 100))
    })
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
