import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import NavBar from '../../components/NavBar'
import MarkdownEditor from '../../components/MarkdownEditor'
import { loadTranslations, type Translations } from '../../i18n'

type SessionTypeTipDto = {
  id: number
  content: string
  displayOrder: number
  isActive: boolean
}

type SessionTypeQuestionDto = {
  id: number
  content: string
  displayOrder: number
  isActive: boolean
}

type SessionTypeDto = {
  id: number
  name: string
  description: string | null
  isActive: boolean
  sessionsCount: number
  tips: SessionTypeTipDto[]
  questions: SessionTypeQuestionDto[]
}

type EditableTip = {
  id?: number
  content: string
  displayOrder: number
  isActive: boolean
}

type EditableQuestion = {
  id?: number
  content: string
  displayOrder: number
  isActive: boolean
}

type EditableSessionType = {
  id?: number
  name: string
  description: string
  isActive: boolean
  tips: EditableTip[]
  questions: EditableQuestion[]
}

const emptyForm: EditableSessionType = {
  name: '',
  description: '',
  isActive: true,
  tips: [],
  questions: []
}

function cloneForm(form: EditableSessionType): EditableSessionType {
  return {
    id: form.id,
    name: form.name,
    description: form.description,
    isActive: form.isActive,
    tips: form.tips.map(t => ({ ...t })),
    questions: form.questions.map(q => ({ ...q }))
  }
}

function mapToEditable(dto: SessionTypeDto): EditableSessionType {
  return {
    id: dto.id,
    name: dto.name,
    description: dto.description ?? '',
    isActive: dto.isActive,
    tips: dto.tips
      .slice()
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map(tip => ({
        id: tip.id,
        content: tip.content,
        displayOrder: tip.displayOrder,
        isActive: tip.isActive
      })),
    questions: dto.questions
      .slice()
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map(question => ({
        id: question.id,
        content: question.content,
        displayOrder: question.displayOrder,
        isActive: question.isActive
      }))
  }
}

function nextOrder<T extends { displayOrder: number }>(items: T[]): number {
  if (items.length === 0) return 1
  return Math.max(...items.map(i => i.displayOrder)) + 1
}

function validateForm(form: EditableSessionType, t: Translations): string[] {
  const errors: string[] = []
  if (!form.name.trim()) {
    errors.push(t['sessionTypes.validation.nameRequired'] ?? 'Nazwa jest wymagana')
  }

  const tipOrders = new Set<number>()
  form.tips.forEach((tip, index) => {
    if (!tip.content.trim()) {
      errors.push((t['sessionTypes.validation.tipContent'] ?? 'Treść podpowiedzi jest wymagana') + ` (#${index + 1})`)
    }
    if (tipOrders.has(tip.displayOrder)) {
      errors.push((t['sessionTypes.validation.tipOrder'] ?? 'Podpowiedzi muszą mieć unikalną kolejność') + ` (#${index + 1})`)
    }
    tipOrders.add(tip.displayOrder)
  })

  const questionOrders = new Set<number>()
  form.questions.forEach((question, index) => {
    if (!question.content.trim()) {
      errors.push((t['sessionTypes.validation.questionContent'] ?? 'Treść pytania jest wymagana') + ` (#${index + 1})`)
    }
    if (questionOrders.has(question.displayOrder)) {
      errors.push((t['sessionTypes.validation.questionOrder'] ?? 'Pytania muszą mieć unikalną kolejność') + ` (#${index + 1})`)
    }
    questionOrders.add(question.displayOrder)
  })

  return errors
}

const SessionTypeManagement = () => {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl' | 'en'>('pl')
  const [translations, setTranslations] = useState<Translations>({})
  const [sessionTypes, setSessionTypes] = useState<SessionTypeDto[]>([])
  const [form, setForm] = useState<EditableSessionType>(cloneForm(emptyForm))
  const [editingId, setEditingId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [validationErrors, setValidationErrors] = useState<string[]>([])
  const [token, setToken] = useState<string | null>(null)

  useEffect(() => {
    loadTranslations(culture).then(setTranslations).catch(() => setTranslations({}))
  }, [culture])

  useEffect(() => {
    const storedToken = localStorage.getItem('token')
    if (!storedToken) {
      navigate('/login')
      return
    }

    const storedRoles = localStorage.getItem('roles')
    let parsedRoles: unknown = []
    if (storedRoles) {
      try {
        parsedRoles = JSON.parse(storedRoles)
      } catch {
        parsedRoles = []
      }
    }

    if (!Array.isArray(parsedRoles) || !parsedRoles.includes('Administrator')) {
      navigate('/dashboard')
      return
    }

    setToken(storedToken)
  }, [navigate])

  const loadSessionTypes = async (authToken: string) => {
    try {
      setLoading(true)
      setError(null)
      const res = await fetch('/api/sessiontypes', {
        headers: {
          Authorization: `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      })

      if (!res.ok) {
        const text = await res.text().catch(() => '')
        throw new Error(text || `HTTP ${res.status}`)
      }

      const data = (await res.json()) as SessionTypeDto[]
      setSessionTypes(data)

      if (editingId !== null) {
        const updated = data.find(st => st.id === editingId)
        if (updated) {
          setForm(cloneForm(mapToEditable(updated)))
        }
      }
    } catch (err) {
      console.error('Error loading session types', err)
      setError(translations['sessionTypes.loadError'] ?? 'Nie udało się załadować typów sesji')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (!token) return
    loadSessionTypes(token).catch(() => {})
  }, [token])

  const isEditing = editingId !== null

  const pageTitle = translations['sessionTypes.title'] ?? 'Typy sesji'

  const tipsHeader = translations['sessionTypes.tips'] ?? 'Podpowiedzi'
  const questionsHeader = translations['sessionTypes.questions'] ?? 'Pytania'

  const sortedSessionTypes = useMemo(
    () => sessionTypes.slice().sort((a, b) => a.name.localeCompare(b.name)),
    [sessionTypes]
  )

  const resetForm = () => {
    setEditingId(null)
    setForm(cloneForm(emptyForm))
    setValidationErrors([])
  }

  const handleSelect = (sessionType: SessionTypeDto) => {
    setEditingId(sessionType.id)
    setForm(cloneForm(mapToEditable(sessionType)))
    setValidationErrors([])
  }

  const handleAddTip = () => {
    setForm(prev => ({
      ...prev,
      tips: [...prev.tips, { content: '', displayOrder: nextOrder(prev.tips), isActive: true }]
    }))
  }

  const handleAddQuestion = () => {
    setForm(prev => ({
      ...prev,
      questions: [...prev.questions, { content: '', displayOrder: nextOrder(prev.questions), isActive: true }]
    }))
  }

  const handleTipChange = (index: number, patch: Partial<EditableTip>) => {
    setForm(prev => ({
      ...prev,
      tips: prev.tips.map((tip, idx) => (idx === index ? { ...tip, ...patch } : tip))
    }))
  }

  const handleQuestionChange = (index: number, patch: Partial<EditableQuestion>) => {
    setForm(prev => ({
      ...prev,
      questions: prev.questions.map((q, idx) => (idx === index ? { ...q, ...patch } : q))
    }))
  }

  const handleRemoveTip = (index: number) => {
    setForm(prev => ({
      ...prev,
      tips: prev.tips.filter((_, idx) => idx !== index)
    }))
  }

  const handleRemoveQuestion = (index: number) => {
    setForm(prev => ({
      ...prev,
      questions: prev.questions.filter((_, idx) => idx !== index)
    }))
  }

  const buildPayload = (source: EditableSessionType) => ({
    name: source.name.trim(),
    description: source.description.trim() ? source.description.trim() : null,
    isActive: source.isActive,
    tips: source.tips.map(tip => ({
      id: tip.id ?? null,
      content: tip.content.trim(),
      displayOrder: tip.displayOrder,
      isActive: tip.isActive
    })),
    questions: source.questions.map(question => ({
      id: question.id ?? null,
      content: question.content.trim(),
      displayOrder: question.displayOrder,
      isActive: question.isActive
    }))
  })

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!token) return

    const errors = validateForm(form, translations)
    if (errors.length > 0) {
      setValidationErrors(errors)
      return
    }

    setValidationErrors([])
    setSaving(true)
    setMessage(null)
    setError(null)

    try {
      const payload = buildPayload(form)
      const url = isEditing ? `/api/sessiontypes/${editingId}` : '/api/sessiontypes'
      const method = isEditing ? 'PUT' : 'POST'
      const res = await fetch(url, {
        method,
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      })

      if (!res.ok) {
        const text = await res.text().catch(() => '')
        throw new Error(text || `HTTP ${res.status}`)
      }

      await loadSessionTypes(token)
      setMessage(translations['sessionTypes.saveSuccess'] ?? 'Zapisano typ sesji')
      if (!isEditing) {
        resetForm()
      }
    } catch (err) {
      console.error('Failed to save session type', err)
      setError(translations['sessionTypes.saveError'] ?? 'Nie udało się zapisać typu sesji')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (sessionType: SessionTypeDto) => {
    if (!token) return

    const confirmMessage = translations['sessionTypes.deleteConfirm'] ?? 'Czy na pewno chcesz usunąć ten typ sesji?'
    if (!window.confirm(confirmMessage)) return

    try {
      const res = await fetch(`/api/sessiontypes/${sessionType.id}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`
        }
      })

      if (res.status === 400) {
        const text = await res.text().catch(() => '')
        throw new Error(text || 'Bad request')
      }

      if (!res.ok && res.status !== 204) {
        const text = await res.text().catch(() => '')
        throw new Error(text || `HTTP ${res.status}`)
      }

      await loadSessionTypes(token)
      if (editingId === sessionType.id) {
        resetForm()
      }
      setMessage(translations['sessionTypes.deleteSuccess'] ?? 'Typ sesji został usunięty')
    } catch (err) {
      console.error('Failed to delete session type', err)
      setError(
        translations['sessionTypes.deleteError'] ?? 'Nie udało się usunąć typu sesji. Upewnij się, że nie jest powiązany z sesjami.'
      )
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={() => {
        localStorage.removeItem('token')
        localStorage.removeItem('roles')
        navigate('/login')
      }} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <header className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">{pageTitle}</h1>
          <p className="text-gray-600 mt-2">
            {translations['sessionTypes.subtitle'] ?? 'Zarządzaj listą typów sesji, podpowiedziami oraz pytaniami dla terapeutów.'}
          </p>
        </header>

        {(message || error || validationErrors.length > 0) && (
          <div className="mb-6 space-y-2">
            {message && (
              <div className="rounded border border-green-200 bg-green-50 px-4 py-3 text-green-800">{message}</div>
            )}
            {error && (
              <div className="rounded border border-red-200 bg-red-50 px-4 py-3 text-red-800">{error}</div>
            )}
            {validationErrors.length > 0 && (
              <ul className="rounded border border-yellow-200 bg-yellow-50 px-4 py-3 text-yellow-800 list-disc list-inside space-y-1">
                {validationErrors.map((errMsg, idx) => (
                  <li key={idx}>{errMsg}</li>
                ))}
              </ul>
            )}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <section className="bg-white rounded-lg shadow-lg border border-gray-200">
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <h2 className="text-xl font-semibold text-gray-800">{translations['sessionTypes.list'] ?? 'Lista typów sesji'}</h2>
              <button
                onClick={resetForm}
                className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 transition-colors"
              >
                {translations['sessionTypes.create'] ?? 'Nowy typ sesji'}
              </button>
            </div>
            <div className="max-h-[600px] overflow-y-auto">
              {loading ? (
                <div className="p-6 text-center text-gray-500">{translations['common.loading'] ?? 'Ładowanie...'}</div>
              ) : sortedSessionTypes.length === 0 ? (
                <div className="p-6 text-center text-gray-500">
                  {translations['sessionTypes.empty'] ?? 'Brak skonfigurowanych typów sesji'}
                </div>
              ) : (
                <ul className="divide-y divide-gray-100">
                  {sortedSessionTypes.map(sessionType => (
                    <li key={sessionType.id} className="px-6 py-4">
                      <div className="flex items-start justify-between gap-4">
                        <div>
                          <button
                            onClick={() => handleSelect(sessionType)}
                            className="text-left text-lg font-semibold text-blue-700 hover:underline"
                          >
                            {sessionType.name}
                          </button>
                          <div className="mt-1 text-sm text-gray-600">
                            <span className={`inline-flex items-center rounded px-2 py-1 text-xs font-semibold ${
                              sessionType.isActive
                                ? 'bg-green-100 text-green-800'
                                : 'bg-gray-200 text-gray-700'
                            }`}>
                              {sessionType.isActive
                                ? translations['common.active'] ?? 'Aktywny'
                                : translations['common.inactive'] ?? 'Nieaktywny'}
                            </span>
                            <span className="ml-3">
                              {translations['sessionTypes.sessionsCount'] ?? 'Powiązane sesje'}: {sessionType.sessionsCount}
                            </span>
                          </div>
                          <div className="mt-2 text-sm text-gray-500 space-x-4">
                            <span>{tipsHeader}: {sessionType.tips.length}</span>
                            <span>{questionsHeader}: {sessionType.questions.length}</span>
                          </div>
                        </div>
                        <div className="flex flex-col gap-2">
                          <button
                            onClick={() => handleSelect(sessionType)}
                            className="rounded border border-blue-200 px-3 py-1 text-blue-700 hover:bg-blue-50"
                          >
                            {translations['common.edit'] ?? 'Edytuj'}
                          </button>
                          <button
                            onClick={() => handleDelete(sessionType)}
                            className="rounded border border-red-200 px-3 py-1 text-red-700 hover:bg-red-50"
                          >
                            {translations['common.delete'] ?? 'Usuń'}
                          </button>
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </section>

          <section className="bg-white rounded-lg shadow-lg border border-gray-200">
            <div className="border-b border-gray-200 px-6 py-4">
              <h2 className="text-xl font-semibold text-gray-800">
                {isEditing
                  ? translations['sessionTypes.editTitle'] ?? 'Edytuj typ sesji'
                  : translations['sessionTypes.createTitle'] ?? 'Dodaj nowy typ sesji'}
              </h2>
              {isEditing && (
                <p className="text-sm text-gray-500 mt-1">
                  {translations['sessionTypes.editInfo'] ?? 'Wprowadź zmiany i zapisz, aby zaktualizować typ.'}
                </p>
              )}
            </div>
            <form onSubmit={handleSubmit} className="px-6 py-6 space-y-6">
              <div className="space-y-2">
                <label htmlFor="session-type-name" className="block text-sm font-semibold text-gray-700">
                  {translations['sessionTypes.name'] ?? 'Nazwa'}
                </label>
                <input
                  id="session-type-name"
                  type="text"
                  value={form.name}
                  onChange={e => setForm(prev => ({ ...prev, name: e.target.value }))}
                  className="w-full rounded border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
                  required
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="session-type-description" className="block text-sm font-semibold text-gray-700">
                  {translations['sessionTypes.description'] ?? 'Opis'}
                </label>
                <textarea
                  id="session-type-description"
                  value={form.description}
                  onChange={e => setForm(prev => ({ ...prev, description: e.target.value }))}
                  rows={4}
                  className="w-full rounded border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
                />
              </div>

              <div className="flex items-center gap-3">
                <input
                  id="session-type-active"
                  type="checkbox"
                  checked={form.isActive}
                  onChange={e => setForm(prev => ({ ...prev, isActive: e.target.checked }))}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <label htmlFor="session-type-active" className="text-sm font-semibold text-gray-700">
                  {translations['sessionTypes.isActive'] ?? 'Aktywny'}
                </label>
              </div>

              <fieldset className="space-y-4">
                <legend className="text-lg font-semibold text-gray-800">{tipsHeader}</legend>
                <p className="text-sm text-gray-500">
                  {translations['sessionTypes.tipsDescription'] ?? 'Podpowiedzi pojawią się jako krótkie wskazówki dla terapeutów podczas pracy z danym typem sesji.'}
                </p>
                {form.tips.map((tip, index) => (
                  <div key={tip.id ?? `new-tip-${index}`} className="rounded border border-gray-200 p-4 space-y-3 bg-gray-50">
                    <MarkdownEditor
                      id={`tip-content-${index}`}
                      value={tip.content}
                      onChange={content => handleTipChange(index, { content })}
                      label={translations['sessionTypes.tipContent'] ?? 'Treść podpowiedzi'}
                      rows={4}
                      required
                    />
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <label htmlFor={`tip-order-${index}`} className="block text-sm font-semibold text-gray-700">
                          {translations['sessionTypes.displayOrder'] ?? 'Kolejność wyświetlania'}
                        </label>
                        <input
                          id={`tip-order-${index}`}
                          type="number"
                          value={tip.displayOrder}
                          min={0}
                          onChange={e => handleTipChange(index, { displayOrder: Number(e.target.value) })}
                          className="w-full rounded border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
                          required
                        />
                      </div>
                      <div className="flex items-center gap-3 pt-6">
                        <input
                          id={`tip-active-${index}`}
                          type="checkbox"
                          checked={tip.isActive}
                          onChange={e => handleTipChange(index, { isActive: e.target.checked })}
                          className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                        />
                        <label htmlFor={`tip-active-${index}`} className="text-sm font-semibold text-gray-700">
                          {translations['sessionTypes.isActive'] ?? 'Aktywny'}
                        </label>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveTip(index)}
                      className="rounded border border-red-200 px-3 py-1 text-sm text-red-700 hover:bg-red-50"
                    >
                      {translations['common.remove'] ?? 'Usuń'}
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  onClick={handleAddTip}
                  className="rounded border border-dashed border-blue-300 px-4 py-2 text-blue-700 hover:bg-blue-50"
                >
                  {translations['sessionTypes.addTip'] ?? 'Dodaj podpowiedź'}
                </button>
              </fieldset>

              <fieldset className="space-y-4">
                <legend className="text-lg font-semibold text-gray-800">{questionsHeader}</legend>
                <p className="text-sm text-gray-500">
                  {translations['sessionTypes.questionsDescription'] ?? 'Pytania pomagają przygotować terapeutę lub pacjenta do danej sesji.'}
                </p>
                {form.questions.map((question, index) => (
                  <div key={question.id ?? `new-question-${index}`} className="rounded border border-gray-200 p-4 space-y-3 bg-gray-50">
                    <MarkdownEditor
                      id={`question-content-${index}`}
                      value={question.content}
                      onChange={content => handleQuestionChange(index, { content })}
                      label={translations['sessionTypes.questionContent'] ?? 'Treść pytania'}
                      rows={4}
                      required
                    />
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <label htmlFor={`question-order-${index}`} className="block text-sm font-semibold text-gray-700">
                          {translations['sessionTypes.displayOrder'] ?? 'Kolejność wyświetlania'}
                        </label>
                        <input
                          id={`question-order-${index}`}
                          type="number"
                          value={question.displayOrder}
                          min={0}
                          onChange={e => handleQuestionChange(index, { displayOrder: Number(e.target.value) })}
                          className="w-full rounded border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
                          required
                        />
                      </div>
                      <div className="flex items-center gap-3 pt-6">
                        <input
                          id={`question-active-${index}`}
                          type="checkbox"
                          checked={question.isActive}
                          onChange={e => handleQuestionChange(index, { isActive: e.target.checked })}
                          className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                        />
                        <label htmlFor={`question-active-${index}`} className="text-sm font-semibold text-gray-700">
                          {translations['sessionTypes.isActive'] ?? 'Aktywny'}
                        </label>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveQuestion(index)}
                      className="rounded border border-red-200 px-3 py-1 text-sm text-red-700 hover:bg-red-50"
                    >
                      {translations['common.remove'] ?? 'Usuń'}
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  onClick={handleAddQuestion}
                  className="rounded border border-dashed border-blue-300 px-4 py-2 text-blue-700 hover:bg-blue-50"
                >
                  {translations['sessionTypes.addQuestion'] ?? 'Dodaj pytanie'}
                </button>
              </fieldset>

              <div className="flex flex-wrap gap-4">
                <button
                  type="submit"
                  disabled={saving}
                  className="rounded bg-blue-600 px-6 py-3 text-white hover:bg-blue-700 disabled:opacity-50"
                >
                  {saving
                    ? translations['common.saving'] ?? 'Zapisywanie...'
                    : translations['common.save'] ?? 'Zapisz'}
                </button>
                {isEditing && (
                  <button
                    type="button"
                    onClick={resetForm}
                    className="rounded border border-gray-300 px-6 py-3 text-gray-700 hover:bg-gray-100"
                  >
                    {translations['common.cancel'] ?? 'Anuluj'}
                  </button>
                )}
              </div>
            </form>
          </section>
        </div>
      </main>
    </div>
  )
}

export default SessionTypeManagement

