import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import NavBar from '../../components/NavBar'
import { loadTranslations, type Translations } from '../../i18n'

type AdminUser = {
  id: string
  email: string
  userName?: string
  phoneNumber?: string
  roles: string[]
  isLockedOut: boolean
}

type CreateUserForm = {
  email: string
  password: string
  roles: string[]
}

const DEFAULT_FORM: CreateUserForm = {
  email: '',
  password: '',
  roles: ['Pacjent']
}

const AVAILABLE_ROLES = ['Administrator', 'Terapeuta', 'TerapeutaFreeAccess', 'Pacjent'] as const

const UsersAdmin = () => {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl' | 'en'>('pl')
  const [translations, setTranslations] = useState<Translations>({})
  const [users, setUsers] = useState<AdminUser[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateUserForm>({ ...DEFAULT_FORM })
  const [saving, setSaving] = useState(false)
  const [editingUserId, setEditingUserId] = useState<string | null>(null)
  const [editingRoles, setEditingRoles] = useState<string[]>([])
  const [editingUserData, setEditingUserData] = useState<{ email: string; userName: string; phoneNumber: string } | null>(null)
  const [editingMode, setEditingMode] = useState<'roles' | 'data' | null>(null)
  const [actionsError, setActionsError] = useState<string | null>(null)

  const roleLabels = useMemo(() => ({
    Administrator: translations['roles.administrator'] ?? 'Administrator',
    Terapeuta: translations['roles.therapist'] ?? 'Terapeuta',
    TerapeutaFreeAccess: translations['roles.therapist.free'] ?? 'Terapeuta (Free Access)',
    Pacjent: translations['roles.patient'] ?? 'Pacjent'
  }), [translations])

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

    void loadUsers()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function loadUsers() {
    try {
      setLoading(true)
      setError(null)
      const token = localStorage.getItem('token')
      const res = await fetch('/api/admin/users', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (!res.ok) {
        throw new Error(`Błąd ${res.status}`)
      }
      const data = await res.json()
      if (Array.isArray(data)) setUsers(data)
      else setUsers(data.users ?? [])
    } catch (err) {
      console.error('Error loading admin users:', err)
      setError('Nie udało się pobrać listy użytkowników.')
    } finally {
      setLoading(false)
    }
  }

  function updateFormField<K extends keyof CreateUserForm>(field: K, value: CreateUserForm[K]) {
    setForm(prev => ({ ...prev, [field]: value }))
  }

  function toggleFormRole(role: string) {
    setForm(prev => {
      const hasRole = prev.roles.includes(role)
      return {
        ...prev,
        roles: hasRole ? prev.roles.filter(r => r !== role) : [...prev.roles, role]
      }
    })
  }

  function startEditingRoles(user: AdminUser) {
    setEditingUserId(user.id)
    setEditingRoles(user.roles)
    setEditingMode('roles')
    setActionsError(null)
  }

  function startEditingData(user: AdminUser) {
    setEditingUserId(user.id)
    setEditingUserData({
      email: user.email,
      userName: user.userName ?? '',
      phoneNumber: user.phoneNumber ?? ''
    })
    setEditingMode('data')
    setActionsError(null)
  }

  function cancelEditing() {
    setEditingUserId(null)
    setEditingRoles([])
    setEditingUserData(null)
    setEditingMode(null)
  }

  function toggleEditingRole(role: string) {
    setEditingRoles(prev => prev.includes(role) ? prev.filter(r => r !== role) : [...prev, role])
  }

  async function createUser(e: React.FormEvent) {
    e.preventDefault()
    if (!form.email || !form.password) {
      setActionsError('Podaj adres e-mail i hasło.')
      return
    }
    try {
      setSaving(true)
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch('/api/admin/users', {
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
      await loadUsers()
    } catch (err) {
      console.error('Error creating user:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się utworzyć użytkownika.')
    } finally {
      setSaving(false)
    }
  }

  async function saveRoles() {
    if (!editingUserId) return
    try {
      setSaving(true)
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/admin/users/${editingUserId}/roles`, {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ roles: editingRoles })
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      cancelEditing()
      await loadUsers()
    } catch (err) {
      console.error('Error updating roles:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się zaktualizować ról użytkownika.')
    } finally {
      setSaving(false)
    }
  }

  async function saveUserData() {
    if (!editingUserId || !editingUserData) return
    try {
      setSaving(true)
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/admin/users/${editingUserId}`, {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(editingUserData)
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      cancelEditing()
      await loadUsers()
    } catch (err) {
      console.error('Error updating user data:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się zaktualizować danych użytkownika.')
    } finally {
      setSaving(false)
    }
  }

  async function toggleLock(user: AdminUser) {
    try {
      setActionsError(null)
      const token = localStorage.getItem('token')
      const res = await fetch(`/api/admin/users/${user.id}/lock`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ lock: !user.isLockedOut })
      })
      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        throw new Error(payload?.error ?? `Błąd ${res.status}`)
      }
      await loadUsers()
    } catch (err) {
      console.error('Error toggling lockout:', err)
      setActionsError(err instanceof Error ? err.message : 'Nie udało się zmienić stanu użytkownika.')
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={() => { localStorage.clear(); navigate('/login') }} navigate={navigate} />
      <main className="w-full py-8 px-8 space-y-8">
        <header className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{translations['admin.users.title'] ?? 'Zarządzanie użytkownikami'}</h1>
            <p className="text-sm text-gray-600 mt-1 max-w-3xl">
              {translations['admin.users.subtitle'] ?? 'Twórz nowych użytkowników, nadaj role i kontroluj dostęp do systemu.'}
            </p>
          </div>
          <button
            onClick={loadUsers}
            className="self-start md:self-auto text-sm font-semibold text-blue-600 hover:text-blue-800"
          >
            {translations['common.refresh'] ?? 'Odśwież listę'}
          </button>
        </header>

        <section className="bg-white p-6 rounded-xl shadow-md border border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">{translations['admin.users.create'] ?? 'Dodaj nowego użytkownika'}</h2>
          {actionsError && (
            <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {actionsError}
            </div>
          )}
          <form onSubmit={createUser} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-700">E-mail</label>
              <input
                type="email"
                value={form.email}
                onChange={e => updateFormField('email', e.target.value)}
                className="rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                placeholder="użytkownik@example.com"
                required
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-sm font-semibold text-gray-700">Hasło tymczasowe</label>
              <input
                type="password"
                value={form.password}
                onChange={e => updateFormField('password', e.target.value)}
                className="rounded-lg border border-gray-300 px-3 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                placeholder="Silne hasło"
                required
              />
            </div>
            <div className="flex flex-col gap-2">
              <span className="text-sm font-semibold text-gray-700">Role</span>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {AVAILABLE_ROLES.map(role => (
                  <label key={role} className="flex items-center gap-2 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={form.roles.includes(role)}
                      onChange={() => toggleFormRole(role)}
                      className="h-4 w-4"
                    />
                    {roleLabels[role]}
                  </label>
                ))}
              </div>
            </div>
            <div className="md:col-span-3 flex justify-end gap-3">
              <button
                type="submit"
                disabled={saving}
                className="bg-blue-600 text-white px-5 py-2 rounded-lg font-semibold hover:bg-blue-700 disabled:opacity-50"
              >
                {translations['admin.users.create.submit'] ?? 'Utwórz użytkownika'}
              </button>
            </div>
          </form>
        </section>

        <section className="bg-white p-6 rounded-xl shadow-md border border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">{translations['admin.users.list'] ?? 'Lista użytkowników'}</h2>
          {loading ? (
            <p className="text-sm text-gray-600">{translations['common.loading'] ?? 'Ładowanie...'}</p>
          ) : error ? (
            <p className="text-sm text-red-600">{error}</p>
          ) : users.length === 0 ? (
            <p className="text-sm text-gray-600">{translations['admin.users.empty'] ?? 'Brak użytkowników.'}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200 text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.users.email'] ?? 'E-mail'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.users.roles'] ?? 'Role'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.users.status'] ?? 'Status'}</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">{translations['admin.users.actions'] ?? 'Akcje'}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {users.map(user => {
                    const isEditingRoles = editingUserId === user.id && editingMode === 'roles'
                    const isEditingData = editingUserId === user.id && editingMode === 'data'
                    return (
                      <tr key={user.id} className="hover:bg-gray-50">
                        <td className="px-4 py-3">
                          {isEditingData && editingUserData ? (
                            <div className="space-y-2">
                              <input
                                type="email"
                                value={editingUserData.email}
                                onChange={e => setEditingUserData({ ...editingUserData, email: e.target.value })}
                                className="w-full rounded-lg border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                              />
                              <input
                                type="text"
                                value={editingUserData.userName}
                                onChange={e => setEditingUserData({ ...editingUserData, userName: e.target.value })}
                                placeholder="Nazwa użytkownika"
                                className="w-full rounded-lg border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                              />
                              <input
                                type="tel"
                                value={editingUserData.phoneNumber}
                                onChange={e => setEditingUserData({ ...editingUserData, phoneNumber: e.target.value })}
                                placeholder="Telefon"
                                className="w-full rounded-lg border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
                              />
                            </div>
                          ) : (
                            <>
                              <div className="font-semibold text-gray-900">{user.email}</div>
                              {user.userName && <div className="text-xs text-gray-500">{user.userName}</div>}
                              {user.phoneNumber && <div className="text-xs text-gray-500">{user.phoneNumber}</div>}
                            </>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {isEditingRoles ? (
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                              {AVAILABLE_ROLES.map(role => (
                                <label key={role} className="flex items-center gap-2 text-sm text-gray-700">
                                  <input
                                    type="checkbox"
                                    checked={editingRoles.includes(role)}
                                    onChange={() => toggleEditingRole(role)}
                                    className="h-4 w-4"
                                  />
                                  {roleLabels[role]}
                                </label>
                              ))}
                            </div>
                          ) : (
                            <div className="flex flex-wrap gap-2">
                              {user.roles.map(role => (
                                <span key={role} className="inline-flex items-center rounded-full bg-blue-50 px-3 py-1 text-xs font-semibold text-blue-700">
                                  {roleLabels[role as keyof typeof roleLabels] ?? role}
                                </span>
                              ))}
                              {user.roles.length === 0 && <span className="text-xs text-gray-500">{translations['admin.users.noRoles'] ?? 'Brak przypisanych ról'}</span>}
                            </div>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          {user.isLockedOut ? (
                            <span className="text-xs font-semibold text-red-600">{translations['admin.users.locked'] ?? 'Zablokowany'}</span>
                          ) : (
                            <span className="text-xs font-semibold text-green-600">{translations['admin.users.active'] ?? 'Aktywny'}</span>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex flex-wrap gap-2">
                            {isEditingRoles ? (
                              <>
                                <button
                                  onClick={saveRoles}
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
                            ) : isEditingData ? (
                              <>
                                <button
                                  onClick={saveUserData}
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
                                  onClick={() => startEditingRoles(user)}
                                  className="text-xs font-semibold text-blue-600 hover:text-blue-800"
                                >
                                  {translations['admin.users.editRoles'] ?? 'Edytuj role'}
                                </button>
                                <button
                                  onClick={() => startEditingData(user)}
                                  className="text-xs font-semibold text-blue-600 hover:text-blue-800"
                                >
                                  {translations['admin.users.editData'] ?? 'Edytuj dane'}
                                </button>
                                <button
                                  onClick={() => toggleLock(user)}
                                  className="text-xs font-semibold text-orange-600 hover:text-orange-700"
                                >
                                  {user.isLockedOut ? (translations['admin.users.unlock'] ?? 'Odblokuj') : (translations['admin.users.lock'] ?? 'Zablokuj')}
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

export default UsersAdmin


