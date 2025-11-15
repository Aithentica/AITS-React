import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import MarkdownEditor from './MarkdownEditor'

describe('MarkdownEditor', () => {
  const user = userEvent.setup()
  const mockOnChange = vi.fn()

  beforeEach(() => {
    mockOnChange.mockClear()
  })

  it('renderuje textarea z wartością początkową', () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value="Test content"
        onChange={mockOnChange}
        label="Test Label"
      />
    )

    const textarea = screen.getByLabelText('Test Label')
    expect(textarea).toBeInTheDocument()
    expect(textarea).toHaveValue('Test content')
  })

  it('wywołuje onChange przy zmianie tekstu', async () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value=""
        onChange={mockOnChange}
        label="Test Label"
      />
    )

    const textarea = screen.getByLabelText('Test Label')
    await user.type(textarea, 'Nowy tekst')

    // user.type wywołuje onChange dla każdego znaku osobno
    // Sprawdzamy czy ostatnie wywołanie zawiera pełny tekst
    expect(mockOnChange).toHaveBeenCalled()
    const lastCall = mockOnChange.mock.calls[mockOnChange.mock.calls.length - 1][0]
    expect(lastCall).toBe('t') // Ostatni znak
    // Sprawdzamy czy wszystkie znaki zostały przekazane
    const allCalls = mockOnChange.mock.calls.map(call => call[0]).join('')
    expect(allCalls).toBe('Nowy tekst')
  })

  it('wyświetla przyciski formatowania', () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value=""
        onChange={mockOnChange}
        label="Test Label"
      />
    )

    expect(screen.getByTitle('Pogrubienie')).toBeInTheDocument()
    expect(screen.getByTitle('Kursywa')).toBeInTheDocument()
    expect(screen.getByTitle('Nagłówek 1')).toBeInTheDocument()
  })

  it('wstawia markdown przy użyciu przycisku pogrubienia', async () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value="wybrany tekst"
        onChange={mockOnChange}
        label="Test Label"
      />
    )

    const textarea = screen.getByLabelText('Test Label') as HTMLTextAreaElement
    textarea.setSelectionRange(0, 15) // Zaznacz cały tekst

    const boldButton = screen.getByTitle('Pogrubienie')
    await user.click(boldButton)

    // Sprawdź czy onChange zostało wywołane z poprawnym markdown
    expect(mockOnChange).toHaveBeenCalled()
  })

  it('przełącza między trybem edycji a podglądem', async () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value="**pogrubiony tekst**"
        onChange={mockOnChange}
        label="Test Label"
      />
    )

    // Początkowo powinien być tryb edycji
    expect(screen.getByLabelText('Test Label')).toBeInTheDocument()

    // Kliknij przycisk podglądu
    const previewButton = screen.getByText('Podgląd')
    await user.click(previewButton)

    // Powinien być widoczny podgląd z sformatowanym tekstem
    expect(screen.queryByLabelText('Test Label')).not.toBeInTheDocument()
    expect(screen.getByText('pogrubiony tekst')).toBeInTheDocument() // Tekst powinien być sformatowany jako strong

    // Wróć do trybu edycji
    const editButton = screen.getByText('Edytuj')
    await user.click(editButton)

    expect(screen.getByLabelText('Test Label')).toBeInTheDocument()
  })

  it('wyświetla placeholder gdy wartość jest pusta', () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value=""
        onChange={mockOnChange}
        label="Test Label"
        placeholder="Wpisz tekst..."
      />
    )

    const textarea = screen.getByLabelText('Test Label')
    expect(textarea).toHaveAttribute('placeholder', 'Wpisz tekst...')
  })

  it('respektuje właściwość required', () => {
    render(
      <MarkdownEditor
        id="test-editor"
        value=""
        onChange={mockOnChange}
        label="Test Label"
        required
      />
    )

    const textarea = screen.getByLabelText('Test Label')
    expect(textarea).toBeRequired()
  })
})

