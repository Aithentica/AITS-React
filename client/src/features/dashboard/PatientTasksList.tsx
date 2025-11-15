import { useEffect, useState } from 'react'

interface Task {
  id: number
  title: string
  description: string
  dueDate: string
  isCompleted: boolean
  completedAt: string | null
  createdAt: string
  therapistName: string
  sessionDate: string | null
}

interface PatientTasksListProps {
  token: string
}

export default function PatientTasksList({ token }: PatientTasksListProps) {
  const [tasks, setTasks] = useState<Task[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadTasks()
  }, [token])

  async function loadTasks() {
    try {
      setLoading(true)
      setError(null)
      const res = await fetch('/api/patient/sessions/tasks', {
        headers: { Authorization: `Bearer ${token}` }
      })

      if (!res.ok) {
        throw new Error('Nie udało się załadować zadań')
      }

      const data = await res.json()
      setTasks(data)
    } catch (err) {
      console.error('Error loading tasks:', err)
      setError('Błąd ładowania zadań')
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

  function isOverdue(dueDate: string, isCompleted: boolean) {
    if (isCompleted) return false
    return new Date(dueDate) < new Date()
  }

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Zadania zlecone przez terapeutę</h3>
        <div className="text-center text-gray-500 py-8">Ładowanie...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Zadania zlecone przez terapeutę</h3>
        <div className="text-center text-red-600 py-8">{error}</div>
      </div>
    )
  }

  if (tasks.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Zadania zlecone przez terapeutę</h3>
        <div className="text-center text-gray-500 py-8">
          Brak zadań do wykonania.
        </div>
      </div>
    )
  }

  const pendingTasks = tasks.filter(t => !t.isCompleted)
  const completedTasks = tasks.filter(t => t.isCompleted)

  return (
    <div className="bg-white rounded-lg shadow-lg p-6">
      <h3 className="text-xl font-bold text-gray-800 mb-4">Zadania zlecone przez terapeutę</h3>
      
      {pendingTasks.length > 0 && (
        <div className="mb-6">
          <h4 className="text-lg font-semibold text-gray-700 mb-3">Do wykonania</h4>
          <div className="space-y-3">
            {pendingTasks.map(task => (
              <div
                key={task.id}
                className={`p-4 rounded-lg border-2 ${
                  isOverdue(task.dueDate, task.isCompleted)
                    ? 'border-red-300 bg-red-50'
                    : 'border-blue-200 bg-blue-50'
                }`}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h5 className="font-semibold text-gray-800 mb-1">{task.title}</h5>
                    <p className="text-sm text-gray-600 mb-2">{task.description}</p>
                    <div className="flex items-center gap-4 text-xs text-gray-500">
                      <span>Termin: {formatDate(task.dueDate)}</span>
                      {task.therapistName && (
                        <span>Zlecone przez: {task.therapistName}</span>
                      )}
                    </div>
                  </div>
                  {isOverdue(task.dueDate, task.isCompleted) && (
                    <span className="px-2 py-1 text-xs font-semibold text-red-800 bg-red-200 rounded">
                      Przeterminowane
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {completedTasks.length > 0 && (
        <div>
          <h4 className="text-lg font-semibold text-gray-700 mb-3">Wykonane</h4>
          <div className="space-y-3">
            {completedTasks.map(task => (
              <div
                key={task.id}
                className="p-4 rounded-lg border-2 border-green-200 bg-green-50 opacity-75"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h5 className="font-semibold text-gray-800 mb-1 line-through">{task.title}</h5>
                    <p className="text-sm text-gray-600 mb-2">{task.description}</p>
                    <div className="flex items-center gap-4 text-xs text-gray-500">
                      <span>Wykonano: {task.completedAt ? formatDate(task.completedAt) : formatDate(task.createdAt)}</span>
                    </div>
                  </div>
                  <span className="px-2 py-1 text-xs font-semibold text-green-800 bg-green-200 rounded">
                    ✓ Wykonane
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

