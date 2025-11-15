import { useEffect, useState } from 'react'

interface Diary {
  id: number
  entryDate: string
  title: string
  content: string
  mood: string | null
  moodRating: number | null
  createdAt: string
  updatedAt: string | null
}

interface PatientDiariesListProps {
  token: string
}

export default function PatientDiariesList({ token }: PatientDiariesListProps) {
  const [diaries, setDiaries] = useState<Diary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadDiaries()
  }, [token])

  async function loadDiaries() {
    try {
      setLoading(true)
      setError(null)
      const res = await fetch('/api/patient/sessions/diaries?limit=5', {
        headers: { Authorization: `Bearer ${token}` }
      })

      if (!res.ok) {
        throw new Error('Nie udało się załadować dzienniczków')
      }

      const data = await res.json()
      setDiaries(data)
    } catch (err) {
      console.error('Error loading diaries:', err)
      setError('Błąd ładowania dzienniczków')
    } finally {
      setLoading(false)
    }
  }

  function formatDate(dateStr: string) {
    return new Date(dateStr).toLocaleDateString('pl-PL', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  }

  function getMoodEmoji(mood: string | null) {
    if (!mood) return '😐'
    const moodLower = mood.toLowerCase()
    if (moodLower.includes('dobr') || moodLower.includes('dobrze')) return '😊'
    if (moodLower.includes('zł') || moodLower.includes('zle')) return '😔'
    if (moodLower.includes('neutral')) return '😐'
    return '😐'
  }

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Dzienniczki</h3>
        <div className="text-center text-gray-500 py-8">Ładowanie...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Dzienniczki</h3>
        <div className="text-center text-red-600 py-8">{error}</div>
      </div>
    )
  }

  if (diaries.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Dzienniczki</h3>
        <div className="text-center text-gray-500 py-8">
          Brak wpisów w dzienniczku. Dodaj pierwszy wpis, aby śledzić swoje samopoczucie.
        </div>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-lg shadow-lg p-6">
      <h3 className="text-xl font-bold text-gray-800 mb-4">Dzienniczki</h3>
      <div className="space-y-4">
        {diaries.map(diary => (
          <div
            key={diary.id}
            className="p-4 rounded-lg border-2 border-purple-200 bg-purple-50"
          >
            <div className="flex items-start justify-between mb-2">
              <div className="flex items-center gap-2">
                <span className="text-2xl">{getMoodEmoji(diary.mood)}</span>
                <h5 className="font-semibold text-gray-800">{diary.title || 'Bez tytułu'}</h5>
              </div>
              <span className="text-xs text-gray-500">{formatDate(diary.entryDate)}</span>
            </div>
            <p className="text-sm text-gray-700 mb-2 line-clamp-3">{diary.content}</p>
            <div className="flex items-center gap-4 text-xs text-gray-500">
              {diary.mood && (
                <span>Nastrój: {diary.mood}</span>
              )}
              {diary.moodRating && (
                <span>Ocena: {diary.moodRating}/10</span>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

