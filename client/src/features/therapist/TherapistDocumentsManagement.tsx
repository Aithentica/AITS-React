import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import NavBar from '../../components/NavBar'
import { loadTranslations, type Translations } from '../../i18n'

interface TherapistDocument {
  id: number
  description: string
  fileName: string
  contentType: string
  fileSize: number
  uploadDate: string
  createdAt: string
  updatedAt?: string
}

export function TherapistDocumentsManagement() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl' | 'en'>('pl')
  const [translations, setTranslations] = useState<Translations>({})
  const [documents, setDocuments] = useState<TherapistDocument[]>([])
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showUploadForm, setShowUploadForm] = useState(false)
  const [uploadForm, setUploadForm] = useState({
    description: '',
    file: null as File | null
  })
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editDescription, setEditDescription] = useState('')

  useEffect(() => {
    loadTranslations(culture).then(setTranslations).catch(() => setTranslations({}))
  }, [culture])

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }
    loadDocuments()
  }, [navigate])

  async function loadDocuments() {
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const response = await fetch('/api/therapist/documents', {
        headers: { 'Authorization': `Bearer ${token}` }
      })

      if (!response.ok) {
        throw new Error('Nie udało się załadować dokumentów')
      }

      const data = await response.json()
      setDocuments(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania dokumentów')
    } finally {
      setLoading(false)
    }
  }

  async function uploadDocument() {
    if (!uploadForm.file || !uploadForm.description.trim()) {
      setError('Wypełnij wszystkie pola')
      return
    }

    try {
      setUploading(true)
      setError(null)
      const token = localStorage.getItem('token')
      const formData = new FormData()
      formData.append('file', uploadForm.file)
      formData.append('description', uploadForm.description)

      const response = await fetch('/api/therapist/documents', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` },
        body: formData
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Nie udało się wgrać dokumentu')
      }

      setUploadForm({ description: '', file: null })
      setShowUploadForm(false)
      await loadDocuments()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas wgrywania dokumentu')
    } finally {
      setUploading(false)
    }
  }

  async function downloadDocument(id: number) {
    try {
      const token = localStorage.getItem('token')
      const response = await fetch(`/api/therapist/documents/${id}/download`, {
        headers: { 'Authorization': `Bearer ${token}` }
      })

      if (!response.ok) {
        throw new Error('Nie udało się pobrać dokumentu')
      }

      const blob = await response.blob()
      const doc = documents.find(d => d.id === id)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = doc?.fileName || `document-${id}`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas pobierania dokumentu')
    }
  }

  async function updateDocument(id: number) {
    if (!editDescription.trim()) {
      setError('Opis jest wymagany')
      return
    }

    try {
      setError(null)
      const token = localStorage.getItem('token')
      const response = await fetch(`/api/therapist/documents/${id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ description: editDescription })
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Nie udało się zaktualizować dokumentu')
      }

      setEditingId(null)
      setEditDescription('')
      await loadDocuments()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas aktualizacji dokumentu')
    }
  }

  async function deleteDocument(id: number) {
    if (!confirm('Czy na pewno chcesz usunąć ten dokument?')) {
      return
    }

    try {
      setError(null)
      const token = localStorage.getItem('token')
      const response = await fetch(`/api/therapist/documents/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Nie udało się usunąć dokumentu')
      }

      await loadDocuments()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas usuwania dokumentu')
    }
  }

  function formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB'
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB'
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto w-full">
          <div className="bg-white rounded-lg shadow-lg p-6">
            <div className="text-center">Ładowanie dokumentów...</div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={() => { localStorage.clear(); navigate('/login') }} navigate={navigate} />
      <div className="p-6">
        <div className="max-w-7xl mx-auto w-full">
          <div className="bg-white rounded-lg shadow-lg border border-gray-200 p-6">
          <div className="flex justify-between items-center mb-6">
            <h1 className="text-3xl font-bold text-gray-900">Dokumenty terapeuty</h1>
            <button
              onClick={() => setShowUploadForm(!showUploadForm)}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
            >
              {showUploadForm ? 'Anuluj' : 'Dodaj dokument'}
            </button>
          </div>

          {error && (
            <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
              {error}
            </div>
          )}

          {showUploadForm && (
            <div className="mb-6 p-6 bg-gray-50 rounded-lg border border-gray-200">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">Wgraj nowy dokument</h2>
              <form
                onSubmit={(e) => {
                  e.preventDefault()
                  uploadDocument()
                }}
                className="space-y-4"
              >
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Opis dokumentu *
                  </label>
                  <input
                    type="text"
                    value={uploadForm.description}
                    onChange={(e) => setUploadForm({ ...uploadForm, description: e.target.value })}
                    className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                    placeholder="np. Dyplom ukończenia studiów, Certyfikat..."
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Plik *
                  </label>
                  <input
                    type="file"
                    onChange={(e) => setUploadForm({ ...uploadForm, file: e.target.files?.[0] || null })}
                    className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                    required
                  />
                  {uploadForm.file && (
                    <p className="mt-2 text-sm text-gray-600">
                      Wybrany plik: {uploadForm.file.name} ({(uploadForm.file.size / 1024).toFixed(2)} KB)
                    </p>
                  )}
                </div>
                <div className="flex gap-4">
                  <button
                    type="submit"
                    disabled={uploading}
                    className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  >
                    {uploading ? 'Wgrywanie...' : 'Wgraj dokument'}
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setShowUploadForm(false)
                      setUploadForm({ description: '', file: null })
                      setError(null)
                    }}
                    className="bg-gray-300 text-gray-700 px-6 py-2 rounded-lg hover:bg-gray-400"
                  >
                    Anuluj
                  </button>
                </div>
              </form>
            </div>
          )}

          {documents.length === 0 ? (
            <div className="text-center py-12">
              <p className="text-gray-600 mb-4">Brak dokumentów. Dodaj pierwszy dokument.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full border-collapse">
                <thead>
                  <tr className="bg-gray-100">
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">Opis</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">Nazwa pliku</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">Rozmiar</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">Data wgrania</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">Akcje</th>
                  </tr>
                </thead>
                <tbody>
                  {documents.map((doc) => (
                    <tr key={doc.id} className="border-t hover:bg-gray-50">
                      <td className="px-4 py-3">
                        {editingId === doc.id ? (
                          <input
                            type="text"
                            value={editDescription}
                            onChange={(e) => setEditDescription(e.target.value)}
                            className="w-full border-2 border-gray-300 rounded px-2 py-1"
                            onBlur={() => {
                              if (editDescription.trim()) {
                                updateDocument(doc.id)
                              } else {
                                setEditingId(null)
                                setEditDescription('')
                              }
                            }}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') {
                                updateDocument(doc.id)
                              } else if (e.key === 'Escape') {
                                setEditingId(null)
                                setEditDescription('')
                              }
                            }}
                            autoFocus
                          />
                        ) : (
                          <span>{doc.description}</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-600">{doc.fileName}</td>
                      <td className="px-4 py-3 text-sm text-gray-600">{formatFileSize(doc.fileSize)}</td>
                      <td className="px-4 py-3 text-sm text-gray-600">
                        {new Date(doc.uploadDate).toLocaleString('pl-PL')}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-2">
                          <button
                            onClick={() => downloadDocument(doc.id)}
                            className="text-blue-600 hover:text-blue-800 text-sm"
                          >
                            Pobierz
                          </button>
                          <button
                            onClick={() => {
                              setEditingId(doc.id)
                              setEditDescription(doc.description)
                            }}
                            className="text-green-600 hover:text-green-800 text-sm"
                          >
                            Edytuj
                          </button>
                          <button
                            onClick={() => deleteDocument(doc.id)}
                            className="text-red-600 hover:text-red-800 text-sm"
                          >
                            Usuń
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          </div>
        </div>
      </div>
    </div>
  )
}

