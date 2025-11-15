import { useEffect, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

interface SessionTypeSidebarProps {
  token: string
  sessionTypeId?: number | null
}

interface SessionTypeQuestion {
  id: number
  content: string
  displayOrder: number
}

interface SessionTypeTip {
  id: number
  content: string
  displayOrder: number
}

interface SessionType {
  id: number
  name: string
  description?: string
  questions: SessionTypeQuestion[]
  tips: SessionTypeTip[]
}

export default function SessionTypeSidebar({ token, sessionTypeId }: SessionTypeSidebarProps) {
  const [sessionType, setSessionType] = useState<SessionType | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (sessionTypeId) {
      loadSessionType(sessionTypeId)
    } else {
      setSessionType(null)
    }
  }, [sessionTypeId, token])

  async function loadSessionType(id: number) {
    try {
      setLoading(true)
      const res = await fetch(`/api/sessiontypes/${id}/details`, {
        headers: { 'Authorization': `Bearer ${token}` }
      })
      if (res.ok) {
        const data = await res.json()
        setSessionType(data)
      } else {
        setSessionType(null)
      }
    } catch (err) {
      console.error('Error loading session type:', err)
      setSessionType(null)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-4 sticky top-4">
      {/* Pytania do sesji */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
        <h3 className="text-sm font-semibold text-blue-700 uppercase tracking-wide mb-3">Pytania do sesji</h3>
        {loading ? (
          <p className="text-sm text-gray-500 italic">Ładowanie...</p>
        ) : sessionTypeId && sessionType ? (
          sessionType.questions && sessionType.questions.length > 0 ? (
            <ul className="space-y-3">
              {sessionType.questions
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .map((question) => (
                  <li key={question.id} className="text-sm text-gray-700 pl-3 border-l-2 border-blue-200">
                    <div className="prose prose-sm max-w-none">
                      <ReactMarkdown remarkPlugins={[remarkGfm]}>
                        {question.content}
                      </ReactMarkdown>
                    </div>
                  </li>
                ))}
            </ul>
          ) : (
            <p className="text-sm text-gray-500 italic">Brak pytań dla tego typu sesji</p>
          )
        ) : (
          <p className="text-sm text-gray-500 italic">Wybierz typ sesji, aby zobaczyć pytania</p>
        )}
      </div>

      {/* Podpowiedzi do sesji */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
        <h3 className="text-sm font-semibold text-green-700 uppercase tracking-wide mb-3">Podpowiedzi do sesji</h3>
        {loading ? (
          <p className="text-sm text-gray-500 italic">Ładowanie...</p>
        ) : sessionTypeId && sessionType ? (
          sessionType.tips && sessionType.tips.length > 0 ? (
            <ul className="space-y-3">
              {sessionType.tips
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .map((tip) => (
                  <li key={tip.id} className="text-sm text-gray-700 pl-3 border-l-2 border-green-200">
                    <div className="prose prose-sm max-w-none">
                      <ReactMarkdown remarkPlugins={[remarkGfm]}>
                        {tip.content}
                      </ReactMarkdown>
                    </div>
                  </li>
                ))}
            </ul>
          ) : (
            <p className="text-sm text-gray-500 italic">Brak podpowiedzi dla tego typu sesji</p>
          )
        ) : (
          <p className="text-sm text-gray-500 italic">Wybierz typ sesji, aby zobaczyć podpowiedzi</p>
        )}
      </div>
    </div>
  )
}
