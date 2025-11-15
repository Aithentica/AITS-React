import { useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

interface MarkdownEditorProps {
  value: string
  onChange: (value: string) => void
  label: string
  id: string
  rows?: number
  required?: boolean
  placeholder?: string
}

export default function MarkdownEditor({
  value,
  onChange,
  label,
  id,
  rows = 6,
  required = false,
  placeholder
}: MarkdownEditorProps) {
  const [showPreview, setShowPreview] = useState(false)

  const insertMarkdown = (before: string, after: string = '') => {
    const textarea = document.getElementById(id) as HTMLTextAreaElement
    if (!textarea) return

    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    const selectedText = value.substring(start, end)
    const newText = value.substring(0, start) + before + selectedText + after + value.substring(end)
    
    onChange(newText)
    
    // Przywróć fokus i pozycję kursora
    setTimeout(() => {
      textarea.focus()
      const newCursorPos = start + before.length + selectedText.length + after.length
      textarea.setSelectionRange(newCursorPos, newCursorPos)
    }, 0)
  }

  const formatButtons = [
    { label: 'B', title: 'Pogrubienie', action: () => insertMarkdown('**', '**') },
    { label: 'I', title: 'Kursywa', action: () => insertMarkdown('*', '*') },
    { label: 'U', title: 'Podkreślenie', action: () => insertMarkdown('<u>', '</u>') },
    { label: 'H1', title: 'Nagłówek 1', action: () => insertMarkdown('# ', '') },
    { label: 'H2', title: 'Nagłówek 2', action: () => insertMarkdown('## ', '') },
    { label: '•', title: 'Lista punktowana', action: () => insertMarkdown('- ', '') },
    { label: '1.', title: 'Lista numerowana', action: () => insertMarkdown('1. ', '') },
    { label: '>', title: 'Cytat', action: () => insertMarkdown('> ', '') },
    { label: '`', title: 'Kod', action: () => insertMarkdown('`', '`') },
    { label: 'Link', title: 'Link', action: () => insertMarkdown('[tekst](', ')') }
  ]

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <label htmlFor={id} className="block text-sm font-semibold text-gray-700">
          {label}
        </label>
        <div className="flex items-center gap-2">
          <span className="text-xs text-gray-500">Markdown</span>
          <button
            type="button"
            onClick={() => setShowPreview(!showPreview)}
            className="text-xs text-blue-600 hover:text-blue-800 underline"
          >
            {showPreview ? 'Edytuj' : 'Podgląd'}
          </button>
        </div>
      </div>
      
      {!showPreview ? (
        <div className="space-y-2">
          <div className="flex flex-wrap gap-1 p-2 bg-gray-100 rounded border border-gray-300">
            {formatButtons.map((btn, idx) => (
              <button
                key={idx}
                type="button"
                onClick={btn.action}
                title={btn.title}
                className="px-2 py-1 text-sm font-semibold text-gray-700 bg-white border border-gray-300 rounded hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-200"
              >
                {btn.label}
              </button>
            ))}
          </div>
          <textarea
            id={id}
            value={value}
            onChange={e => onChange(e.target.value)}
            rows={rows}
            className="w-full rounded border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200 font-mono text-sm"
            required={required}
            placeholder={placeholder || 'Możesz używać Markdown do formatowania tekstu...'}
          />
          <p className="text-xs text-gray-500">
            Wskazówka: Użyj przycisków powyżej lub wpisz składnię Markdown bezpośrednio. 
            <strong>**pogrubienie**</strong>, <em>*kursywa*</em>, <code>`kod`</code>
          </p>
        </div>
      ) : (
        <div className="min-h-[120px] rounded border border-gray-300 bg-white p-4 prose prose-sm max-w-none">
          {value.trim() ? (
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {value}
            </ReactMarkdown>
          ) : (
            <p className="text-gray-400 italic">Podgląd pojawi się tutaj...</p>
          )}
        </div>
      )}
    </div>
  )
}

