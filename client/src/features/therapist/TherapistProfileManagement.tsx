import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import NavBar from '../../components/NavBar'
import { loadTranslations, type Translations } from '../../i18n'

interface TherapistProfile {
  therapistId: string
  firstName: string
  lastName: string
  companyName?: string
  taxId?: string
  regon?: string
  businessAddress?: string
  businessCity?: string
  businessPostalCode?: string
  businessCountry?: string
  isCompany: boolean
  createdAt: string
  updatedAt?: string
  documentsCount: number
}

export function TherapistProfileManagement() {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl' | 'en'>('pl')
  const [translations, setTranslations] = useState<Translations>({})
  const [profile, setProfile] = useState<TherapistProfile | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isEditing, setIsEditing] = useState(false)
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    companyName: '',
    taxId: '',
    regon: '',
    businessAddress: '',
    businessCity: '',
    businessPostalCode: '',
    businessCountry: 'Polska',
    isCompany: false
  })

  useEffect(() => {
    loadTranslations(culture).then(setTranslations).catch(() => setTranslations({}))
  }, [culture])

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) {
      navigate('/login')
      return
    }
    loadProfile()
  }, [navigate])

  async function loadProfile() {
    try {
      setLoading(true)
      const token = localStorage.getItem('token')
      const response = await fetch('/api/therapist/profile', {
        headers: { 'Authorization': `Bearer ${token}` }
      })

      if (response.status === 404) {
        setProfile(null)
        setIsEditing(true)
        return
      }

      if (!response.ok) {
        throw new Error('Nie udało się załadować profilu')
      }

      const data = await response.json()
      setProfile(data)
      setForm({
        firstName: data.firstName || '',
        lastName: data.lastName || '',
        companyName: data.companyName || '',
        taxId: data.taxId || '',
        regon: data.regon || '',
        businessAddress: data.businessAddress || '',
        businessCity: data.businessCity || '',
        businessPostalCode: data.businessPostalCode || '',
        businessCountry: data.businessCountry || 'Polska',
        isCompany: data.isCompany || false
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas ładowania profilu')
    } finally {
      setLoading(false)
    }
  }

  async function saveProfile() {
    try {
      setSaving(true)
      setError(null)
      const token = localStorage.getItem('token')
      const url = profile ? '/api/therapist/profile' : '/api/therapist/profile'
      const method = profile ? 'PUT' : 'POST'

      const response = await fetch(url, {
        method,
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(form)
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Nie udało się zapisać profilu')
      }

      await loadProfile()
      setIsEditing(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Błąd podczas zapisywania profilu')
    } finally {
      setSaving(false)
    }
  }

  function validateNip(nip: string): boolean {
    if (!nip) return true
    const cleanNip = nip.replace(/[\s-]/g, '')
    if (cleanNip.length !== 10 || !/^\d+$/.test(cleanNip)) return false
    
    const weights = [6, 5, 7, 2, 3, 4, 5, 6, 7]
    let sum = 0
    for (let i = 0; i < 9; i++) {
      sum += parseInt(cleanNip[i]) * weights[i]
    }
    const checkDigit = sum % 11
    return checkDigit !== 10 && checkDigit === parseInt(cleanNip[9])
  }

  function validateRegon(regon: string): boolean {
    if (!regon) return true
    const cleanRegon = regon.replace(/[\s-]/g, '')
    if (cleanRegon.length !== 9 && cleanRegon.length !== 14) return false
    if (!/^\d+$/.test(cleanRegon)) return false
    
    if (cleanRegon.length === 9) {
      const weights = [8, 9, 2, 3, 4, 5, 6, 7]
      let sum = 0
      for (let i = 0; i < 8; i++) {
        sum += parseInt(cleanRegon[i]) * weights[i]
      }
      const checkDigit = sum % 11 === 10 ? 0 : sum % 11
      return checkDigit === parseInt(cleanRegon[8])
    }
    
    return true // Dla 14-cyfrowego REGON uproszczona walidacja
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto w-full">
          <div className="bg-white rounded-lg shadow-lg p-6">
            <div className="text-center">Ładowanie profilu...</div>
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
            <h1 className="text-3xl font-bold text-gray-900">Profil terapeuty</h1>
            {profile && !isEditing && (
              <button
                onClick={() => setIsEditing(true)}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
              >
                Edytuj
              </button>
            )}
          </div>

          {error && (
            <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
              {error}
            </div>
          )}

          {!profile && !isEditing ? (
            <div className="text-center py-12">
              <p className="text-gray-600 mb-4">Profil terapeuty nie został jeszcze utworzony.</p>
              <button
                onClick={() => setIsEditing(true)}
                className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700"
              >
                Utwórz profil
              </button>
            </div>
          ) : (
            <form
              onSubmit={(e) => {
                e.preventDefault()
                saveProfile()
              }}
              className="space-y-6"
            >
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Imię *
                  </label>
                  <input
                    type="text"
                    value={form.firstName}
                    onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                    className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                    required
                    disabled={!isEditing}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Nazwisko *
                  </label>
                  <input
                    type="text"
                    value={form.lastName}
                    onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                    className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                    required
                    disabled={!isEditing}
                  />
                </div>
              </div>

              <div className="border-t pt-6">
                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                  Dane działalności gospodarczej / firmy
                </h2>
                <div className="mb-4">
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      checked={form.isCompany}
                      onChange={(e) => setForm({ ...form, isCompany: e.target.checked })}
                      className="mr-2"
                      disabled={!isEditing}
                    />
                    <span className="text-sm font-medium text-gray-700">
                      Firma / Spółka (zamiast działalności gospodarczej)
                    </span>
                  </label>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Nazwa firmy / działalności
                    </label>
                    <input
                      type="text"
                      value={form.companyName}
                      onChange={(e) => setForm({ ...form, companyName: e.target.value })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      disabled={!isEditing}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      NIP
                    </label>
                    <input
                      type="text"
                      value={form.taxId}
                      onChange={(e) => {
                        const value = e.target.value
                        setForm({ ...form, taxId: value })
                        if (value && !validateNip(value)) {
                          setError('Nieprawidłowy numer NIP')
                        } else if (error?.includes('NIP')) {
                          setError(null)
                        }
                      }}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      placeholder="XXX-XXX-XX-XX"
                      disabled={!isEditing}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      REGON
                    </label>
                    <input
                      type="text"
                      value={form.regon}
                      onChange={(e) => {
                        const value = e.target.value
                        setForm({ ...form, regon: value })
                        if (value && !validateRegon(value)) {
                          setError('Nieprawidłowy numer REGON')
                        } else if (error?.includes('REGON')) {
                          setError(null)
                        }
                      }}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      placeholder="XXX-XXX-XXX lub XXX-XXX-XXX-XXXXX"
                      disabled={!isEditing}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Kraj
                    </label>
                    <input
                      type="text"
                      value={form.businessCountry}
                      onChange={(e) => setForm({ ...form, businessCountry: e.target.value })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      disabled={!isEditing}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Ulica
                    </label>
                    <input
                      type="text"
                      value={form.businessAddress}
                      onChange={(e) => setForm({ ...form, businessAddress: e.target.value })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      disabled={!isEditing}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Miasto
                    </label>
                    <input
                      type="text"
                      value={form.businessCity}
                      onChange={(e) => setForm({ ...form, businessCity: e.target.value })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      disabled={!isEditing}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Kod pocztowy
                    </label>
                    <input
                      type="text"
                      value={form.businessPostalCode}
                      onChange={(e) => setForm({ ...form, businessPostalCode: e.target.value })}
                      className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                      placeholder="XX-XXX"
                      disabled={!isEditing}
                    />
                  </div>
                </div>
              </div>

              {isEditing && (
                <div className="flex gap-4 pt-6 border-t">
                  <button
                    type="submit"
                    disabled={saving}
                    className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  >
                    {saving ? 'Zapisywanie...' : 'Zapisz'}
                  </button>
                  {profile && (
                    <button
                      type="button"
                      onClick={() => {
                        setIsEditing(false)
                        loadProfile()
                        setError(null)
                      }}
                      className="bg-gray-300 text-gray-700 px-6 py-2 rounded-lg hover:bg-gray-400"
                    >
                      Anuluj
                    </button>
                  )}
                </div>
              )}

              {profile && !isEditing && (
                <div className="pt-6 border-t text-sm text-gray-600">
                  <p>Utworzono: {new Date(profile.createdAt).toLocaleString('pl-PL')}</p>
                  {profile.updatedAt && (
                    <p>Zaktualizowano: {new Date(profile.updatedAt).toLocaleString('pl-PL')}</p>
                  )}
                  <p>Liczba dokumentów: {profile.documentsCount}</p>
                </div>
              )}
            </form>
          )}
          </div>
        </div>
      </div>
    </div>
  )
}

