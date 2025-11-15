import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import SessionTypeManagement from './SessionTypeManagement'

type MockSessionType = {
  id: number
  name: string
  description: string | null
  isActive: boolean
  sessionsCount: number
  tips: Array<{ id: number; content: string; displayOrder: number; isActive: boolean }>
  questions: Array<{ id: number; content: string; displayOrder: number; isActive: boolean }>
}

describe('SessionTypeManagement', () => {
  const user = userEvent.setup()

  beforeEach(() => {
    localStorage.setItem('token', 'test-token')
    localStorage.setItem('roles', JSON.stringify(['Administrator']))
  })

  afterEach(() => {
    vi.restoreAllMocks()
    localStorage.clear()
  })

  it('pozwala administratorowi utworzyć typ sesji z podpowiedzią i pytaniem', async () => {
    const sessionTypes: MockSessionType[] = [
      {
        id: 1,
        name: 'CBT Standard',
        description: 'Domyślna terapia',
        isActive: true,
        sessionsCount: 0,
        tips: [],
        questions: []
      }
    ]

    let lastPostBody: any = null
    const fetchMock = vi.spyOn(global, 'fetch')
    fetchMock.mockImplementation(async (input: RequestInfo, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.url
      const method = (init?.method ?? 'GET').toUpperCase()

      if (url.includes('/api/i18n')) {
        return new Response(JSON.stringify({}), {
          status: 200,
          headers: { 'Content-Type': 'application/json' }
        })
      }

      if (url.endsWith('/api/sessiontypes') && method === 'GET') {
        return new Response(JSON.stringify(sessionTypes), {
          status: 200,
          headers: { 'Content-Type': 'application/json' }
        })
      }

      if (url.endsWith('/api/sessiontypes') && method === 'POST') {
        lastPostBody = JSON.parse(init?.body as string)
        const newId = Math.max(...sessionTypes.map(st => st.id)) + 1
        const newSessionType: MockSessionType = {
          id: newId,
          name: lastPostBody.name,
          description: lastPostBody.description,
          isActive: lastPostBody.isActive,
          sessionsCount: 0,
          tips: lastPostBody.tips.map((tip: any, index: number) => ({
            id: 100 + index,
            content: tip.content,
            displayOrder: tip.displayOrder,
            isActive: tip.isActive
          })),
          questions: lastPostBody.questions.map((question: any, index: number) => ({
            id: 200 + index,
            content: question.content,
            displayOrder: question.displayOrder,
            isActive: question.isActive
          }))
        }
        sessionTypes.push(newSessionType)

        return new Response(JSON.stringify(newSessionType), {
          status: 201,
          headers: { 'Content-Type': 'application/json' }
        })
      }

      return new Response(null, { status: 404 })
    })

    render(
      <MemoryRouter>
        <SessionTypeManagement />
      </MemoryRouter>
    )

    await screen.findByText('CBT Standard')

    await user.click(screen.getByRole('button', { name: /Nowy typ sesji/i }))

    const nameInput = screen.getByLabelText(/Nazwa/i)
    await user.clear(nameInput)
    await user.type(nameInput, 'Sesja oddechowa')

    await user.click(screen.getByRole('button', { name: /Dodaj podpowiedź/i }))
    const tipTextarea = await screen.findByLabelText(/Treść podpowiedzi/i)
    await user.type(tipTextarea, 'Pamiętaj o spokojnym oddechu przez nos.')

    await user.click(screen.getByRole('button', { name: /Dodaj pytanie/i }))
    const questionTextarea = await screen.findByLabelText(/Treść pytania/i)
    await user.type(questionTextarea, 'Jak się dzisiaj czujesz?')

    await user.click(screen.getByRole('button', { name: /^Zapisz$/i }))

    await waitFor(() => {
      expect(lastPostBody).not.toBeNull()
    })

    expect(lastPostBody.name).toBe('Sesja oddechowa')
    expect(lastPostBody.tips).toHaveLength(1)
    expect(lastPostBody.questions).toHaveLength(1)
    expect(fetchMock).toHaveBeenCalledWith('/api/sessiontypes', expect.objectContaining({ method: 'POST' }))
  })
})

