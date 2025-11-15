import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import NavBar from '../../components/NavBar'
import { loadTranslations, type Translations } from '../../i18n'

type TherapistDto = {
  userId: string
  email: string
  userName?: string
  phoneNumber?: string
  roles: string[]
  freeAccess: boolean
}

type AssignTherapistForm = {
  userId: string
  freeAccess: boolean
}

const DEFAULT_FORM: AssignTherapistForm = {
  userId: '',
  freeAccess: false
}

const TherapistsAdmin = () => {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl' | 'en'>('pl')
  const [translations, setTranslations] = useState<Translations>({})
  const [therapists, setTherapists] = useState<TherapistDto[]>([])
  const [availableUsers, setAvailableUsers] = useState<{ id: string; email: string }[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<AssignTherapistForm>({ ...DEFAULT_FORM })
  const [saving, setSaving] = useState(false)
  const [actionsError, setActionsError] = useState<string | null>(null)
  const [editingTherapistId, setEditingTherapistId] = useState<string | null>(null)
  const [editingData, setEditingData] = useState<{ email: string; userName: string; phoneNumber: string; freeAccess: boolean } | null>(null)

  const accessLabel = useMemo(() => ({
    standard: translations['admin.therapists.access.standard'] ?? 'Standard',
    free: translations['admin.therapists.access.free'] ?? 'Free Access'
  }), [translations])

  const userSelectId = 'therapists-admin-user-select'

  useEffect(() => {
    loadTranslations(culture).then(setTranslations).catch(err => {
      console.error('Error loading translations:', err)
      setTranslations({})
    })
  }, [culture])

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }
    const storedRoles = localStorage.getItem('roles')
    if (storedRoles) {
      try {
        const parsed = JSON.parse(storedRoles)
        if (!Array.isArray(parsed) || !parsed.includes('Administrator')) {
          navigate('/dashboard')
          return
        }
      } catch {
        navigate('/dashboard')
        return
      }
    } else {
      navigate('/dashboard')
      return
    }

    void loadData()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function loadData() {
    await Promise.all([loadTherapists(), loadAvailableUsers()])
  }

  async function loadTherapists() {
    try {
      setLoading(true)
      setError(null)
      const token = localStorage.getItem('token')
      const res = await fetch('/api/admin/therapists', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!res.ok) throw new Error(`Błąd ${res.status}`)
      const data = await res.json()
      if (Array.isArray(data)) setTherapists(data)
      else setTherapists(data.therapists ?? [])
    } catch (err) {
      console.error('Error loading therapists:', err)
      setError('Nie udało się pobrać listy terapeutów.')
    } finally {
      setLoading(false)
    }
  }

  async function loadAvailableUsers() {
    try {
      const token = localStorage.getItem('token')
      const res = await fetch('/api/admin/users', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!res.ok) return
      const data = await res.json()
      const users: any[] = Array.isArray(data) ? data : data.users ?? []
      const filtered = users
        .filter(user => !user.roles?.includes('Terapeuta') && !user.roles?.includes('TerapeutaFreeAccess'))
        .map(user => ({ id: user.id as string, email: user.email as string }))
      setAvailableUsers(filtered)
    } catch (err) {
      console.error('Error loading available users:', err)
    }
  }

  async function assignTherapist(e: React.FormEvent) {
    e.preventDefault()
    if (!form.userId) {
      setActionsError('Wybierz użytkownika do przypisania roli terapeuty.')
      return
    }
    try {
      setSaving(true)
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch('/api/admin/therapists', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(form)
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      setForm({ ...DEFAULT_FORM })
      await loadData()
    } catch (err) {
      console.error('Error assigning therapist:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się przypisać terapeuty.')
    } finally {
      setSaving(false)
    }
  }

  async function toggleFreeAccess(therapist: TherapistDto) {
    try {
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/admin/therapists/${therapist.userId}`, {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: therapist.email,
          userName: therapist.userName,
          phoneNumber: therapist.phoneNumber,
          freeAccess: !therapist.freeAccess
        })
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      await loadData()
    } catch (err) {
      console.error('Error toggling free access:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się zaktualizować uprawnień terapeuty.')
    }
  }

  function startEditing(therapist: TherapistDto) {
    setEditingTherapistId(therapist.userId)
    setEditingData({
      email: therapist.email,
      userName: therapist.userName ?? '',
      phoneNumber: therapist.phoneNumber ?? '',
      freeAccess: therapist.freeAccess
    })
    setActionsError(null)
  }

  function cancelEditing() {
    setEditingTherapistId(null)
    setEditingData(null)
  }

  async function saveTherapist() {
    if (!editingTherapistId || !editingData) return
    try {
      setSaving(true)
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/admin/therapists/${editingTherapistId}`, {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(editingData)
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      setEditingTherapistId(null)
      setEditingData(null)
      await loadData()
    } catch (err) {
      console.error('Error updating therapist:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się zaktualizować danych terapeuty.')
    } finally {
      setSaving(false)
    }
  }

  async function removeTherapist(userId: string) {
    try {
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/admin/therapists/${userId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      await loadData()
    } catch (err) {
      console.error('Error removing therapist:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się usunąć terapeuty.')
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={() => { localStorage.clear(); navigate('/login') }} navigate={navigate} />
      <main className="w-full py-8 px-8 space-y-8">
        <header className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{translations['admin.therapists.title'] ?? 'Zarządzanie terapeutami'}</h1>
            <p className="text-sm text-gray-600 mt-1 max-w-3xl">
              {translations['admin.therapists.subtitle'] ?? 'Kontroluj dostęp terapeutów do aplikacji i przypisuj role terapeutyczne użytkownikom.'}
            </p>
          </div>
          <button
            onClick={loadData}
            className="self-start md:self-auto text-sm font-semibold text-blue-600 hover:text-blue-800"
          >
            {translations['common.refresh'] ?? 'Odśwież listę'}
          </button>
        </header>

        <section className="bg-white p-6 rounded-xl shadow-md border border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">{translations['admin.therapists.assign'] ?? 'Przypisz rolę terapeuty'}</h2>
          {actionsError && (
            <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {actionsError}
            </div>
          )}
          <form onSubmit={assignTherapist} className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
              <label htmlFor={userSelectId} className="block text-sm font-semibold text-gray-700 mb-1">{translations['admin.therapists.selectUser'] ?? 'Wybierz użytkownika'}</label>
              <select
                id={userSelectId}
                value={form.userId}
                onChange={e => setForm(prev => ({ ...prev, userId: e.target.value }))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              >
                <option value="">{translations['admin.therapists.selectUser.placeholder'] ?? '--- wybierz ---'}</option>
                {availableUsers.map(user => (
                  <option key={user.id} value={user.id}>{user.email}</option>
                ))}
              </select>
            </div>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={form.freeAccess}
                onChange={e => setForm(prev => ({ ...prev, freeAccess: e.target.checked }))}
                className="h-4 w-4"
              />
              {translations['admin.therapists.freeAccess'] ?? 'Nadaj Free Access'}
            </label>
            <button
              type="submit"
              disabled={saving}
              className="bg-blue-600 text-white px-5 py-2 rounded-lg font-semibold hover:bg-blue-700 disabled:opacity-50"
            >
              {translations['admin.therapists.assign.submit'] ?? 'Przypisz'}
            </button>
          </form>
          <p className="text-xs text-gray-500 mt-2">
            {translations['admin.therapists.assign.hint'] ?? 'Lista zawiera wyłącznie użytkowników bez przypisanej roli terapeutycznej.'}
          </p>
        </section>

        <section className="bg-white p-6 rounded-xl shadow-md border border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">{translations['admin.therapists.list'] ?? 'Aktywni terapeuci'}</h2>
          {loading ? (
            <p className="text-sm text-gray-600">{translations['common.loading'] ?? 'Ładowanie...'}</p>
          ) : error ? (
            <p className="text-sm text-red-600">{error}</p>
          ) : therapists.length === 0 ? (
            <p className="text-sm text-gray-600">{translations['admin.therapists.empty'] ?? 'Brak przypisanych terapeutów.'}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200 text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.therapists.email'] ?? 'E-mail'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.therapists.userName'] ?? 'Nazwa użytkownika'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.therapists.phone'] ?? 'Telefon'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.therapists.access'] ?? 'Dostęp'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.therapists.actions'] ?? 'Akcje'}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {therapists.map(therapist => {
                    const isEditing = editingTherapistId === therapist.userId
                    return (
                      <tr key={therapist.userId} className="hover:bg-gray-50">
                        <td className="px-4 py-3">
                          {isEditing && editingData ? (
                            <input
                              type="email"
                              value={editingData.email}
                              onChange={e => setEditingData({ ...editingData, email: e.target.value })}
                              className="w-full rounded-lg border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                            />
                          ) : (
                            <div className="font-semibold text-gray-900">{therapist.email}</div>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {isEditing && editingData ? (
                            <input
                              type="text"
                              value={editingData.userName}
                              onChange={e => setEditingData({ ...editingData, userName: e.target.value })}
                              className="w-full rounded-lg border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                            />
                          ) : (
                            <div className="text-sm text-gray-700">{therapist.userName ?? '-'}</div>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {isEditing && editingData ? (
                            <input
                              type="tel"
                              value={editingData.phoneNumber}
                              onChange={e => setEditingData({ ...editingData, phoneNumber: e.target.value })}
                              className="w-full rounded-lg border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                            />
                          ) : (
                            <div className="text-sm text-gray-700">{therapist.phoneNumber ?? '-'}</div>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {isEditing && editingData ? (
                            <label className="flex items-center gap-2 text-sm text-gray-700">
                              <input
                                type="checkbox"
                                checked={editingData.freeAccess}
                                onChange={e => setEditingData({ ...editingData, freeAccess: e.target.checked })}
                                className="h-4 w-4"
                              />
                              {translations['admin.therapists.freeAccess'] ?? 'Free Access'}
                            </label>
                          ) : (
                            <span className="inline-flex items-center rounded-full bg-purple-50 px-3 py-1 text-xs font-semibold text-purple-700">
                              {therapist.freeAccess ? accessLabel.free : accessLabel.standard}
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex flex-wrap gap-2">
                            {isEditing ? (
                              <>
                                <button
                                  onClick={saveTherapist}
                                  disabled={saving}
                                  className="text-xs font-semibold text-green-600 hover:text-green-700 disabled:opacity-50"
                                >
                                  {translations['common.save'] ?? 'Zapisz'}
                                </button>
                                <button
                                  onClick={cancelEditing}
                                  className="text-xs font-semibold text-gray-500 hover:text-gray-700"
                                >
                                  {translations['common.cancel'] ?? 'Anuluj'}
                                </button>
                              </>
                            ) : (
                              <>
                                <button
                                  onClick={() => startEditing(therapist)}
                                  className="text-xs font-semibold text-blue-600 hover:text-blue-800"
                                >
                                  {translations['admin.therapists.edit'] ?? 'Edytuj'}
                                </button>
                                <button
                                  onClick={() => toggleFreeAccess(therapist)}
                                  className="text-xs font-semibold text-blue-600 hover:text-blue-800"
                                >
                                  {therapist.freeAccess ? (translations['admin.therapists.removeFreeAccess'] ?? 'Odbierz Free Access') : (translations['admin.therapists.grantFreeAccess'] ?? 'Nadaj Free Access')}
                                </button>
                                <button
                                  onClick={() => removeTherapist(therapist.userId)}
                                  className="text-xs font-semibold text-red-600 hover:text-red-700"
                                >
                                  {translations['admin.therapists.remove'] ?? 'Usuń z roli terapeuty'}
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </main>
    </div>
  )
}

export default TherapistsAdmin


