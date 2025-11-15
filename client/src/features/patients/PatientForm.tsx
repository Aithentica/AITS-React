import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { loadTranslations, type Translations } from '../../i18n'
import { formatDateWithZone } from '../sessions/dateTimeUtils'
import NavBar from '../../components/NavBar'

interface PatientInformationTypeDto {
  id: number
  code: string
  name: string
  description?: string | null
  displayOrder: number
}

interface PatientInformationEntryForm {
  id?: number
  patientInformationTypeId: number
  typeName: string
  content: string
  createdAt?: string | null
  updatedAt?: string | null
}


export default function PatientForm({ id }: { id?: number }) {
  const navigate = useNavigate()
  const [culture, setCulture] = useState<'pl'|'en'>('pl')
  const [t, setT] = useState<Translations>({})
  const [form, setForm] = useState({
    firstName: '', lastName: '', email: '', phone: '', 
    dateOfBirth: '', gender: '', pesel: '',
    street: '', streetNumber: '', apartmentNumber: '', city: '', postalCode: '', country: 'Polska',
    lastSessionSummary: ''
  })
  const [hasUserAccount, setHasUserAccount] = useState(false)
  const [password, setPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [createUserAccount, setCreateUserAccount] = useState(false)
  const [informationTypes, setInformationTypes] = useState<PatientInformationTypeDto[]>([])
  const [informationEntries, setInformationEntries] = useState<PatientInformationEntryForm[]>([])
  const [loading, setLoading] = useState(!!id)
  const [saving, setSaving] = useState(false)
  const [selectedSection, setSelectedSection] = useState<'personal' | 'address' | 'summary' | 'account' | 'info' | null>(null)
  const [selectedInfoTypeId, setSelectedInfoTypeId] = useState<number | null>(null)
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set(['basic', 'therapeutic']))
  const [expandedProblemDefinition, setExpandedProblemDefinition] = useState(false)

  useEffect(() => {
    loadTranslations(culture).then(setT)
  }, [culture])

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { navigate('/login'); return }
    let cancelled = false

    async function initialize() {
      try {
        const authToken = token!
        const types = await loadInformationTypes(authToken)
        if (cancelled) return
        setInformationTypes(types)
        if (id) {
          setLoading(true)
          await loadPatient(authToken, types)
        } else {
          setInformationEntries(prev => prev.length > 0 ? prev : types.map(type => ({
            patientInformationTypeId: type.id,
            typeName: type.name,
            content: '',
            createdAt: null,
            updatedAt: null
          })))
          setLoading(false)
        }
      } catch (err) {
        if (!cancelled) {
          console.error('Error initializing patient form:', err)
          setLoading(false)
        }
      }
    }

    initialize()
    return () => { cancelled = true }
  }, [id, navigate])

  // Ustaw domyślną wybraną sekcję po załadowaniu danych
  useEffect(() => {
    if (informationEntries.length > 0 && selectedInfoTypeId === null && selectedSection === null) {
      setSelectedSection('personal')
    }
  }, [informationEntries, selectedInfoTypeId, selectedSection])

  const toggleGroup = (group: string) => {
    setExpandedGroups(prev => {
      const newSet = new Set(prev)
      if (newSet.has(group)) {
        newSet.delete(group)
      } else {
        newSet.add(group)
      }
      return newSet
    })
  }

  const handleSectionSelect = (section: 'personal' | 'address' | 'summary' | 'account' | 'info', infoTypeId?: number) => {
    setSelectedSection(section)
    if (section === 'info' && infoTypeId) {
      setSelectedInfoTypeId(infoTypeId)
    } else {
      setSelectedInfoTypeId(null)
    }
  }

  async function loadInformationTypes(authToken: string): Promise<PatientInformationTypeDto[]> {
    try {
      const res = await fetch('/api/patients/information-types', { headers: { 'Authorization': `Bearer ${authToken}` } })
      if (!res.ok) {
        console.error(`Error fetching patient information types: ${res.status}`)
        return []
      }
      const data = await res.json()
      return Array.isArray(data) ? data : []
    } catch (err) {
      console.error('Error fetching patient information types:', err)
      return []
    }
  }

  async function loadPatient(authToken: string, types?: PatientInformationTypeDto[]) {
    if (!id) return
    try {
      const res = await fetch(`/api/patients/${id}`, { headers: { 'Authorization': `Bearer ${authToken}` } })
      if (!res.ok) {
        console.error(`Error loading patient: ${res.status}`)
        return
      }
      const p = await res.json()
      setForm({
        firstName: p.firstName || '', lastName: p.lastName || '', email: p.email || '', phone: p.phone || '',
        dateOfBirth: p.dateOfBirth ? p.dateOfBirth.split('T')[0] : '', gender: p.gender || '', pesel: p.pesel || '',
        street: p.street || '', streetNumber: p.streetNumber || '', apartmentNumber: p.apartmentNumber || '',
        city: p.city || '', postalCode: p.postalCode || '', country: p.country || 'Polska',
        lastSessionSummary: p.lastSessionSummary || ''
      })
      setHasUserAccount(p.hasUserAccount || p.userId ? true : false)
      setPassword('')
      setNewPassword('')
      setCreateUserAccount(false)

      const dictionary = types ?? informationTypes
      const entries = Array.isArray(p.informationEntries) ? p.informationEntries : []
      const mappedEntries: PatientInformationEntryForm[] = entries.map((entry: any) => {
        const meta = dictionary.find(type => type.id === entry.patientInformationTypeId)
        return {
          id: entry.id,
          patientInformationTypeId: entry.patientInformationTypeId,
          typeName: entry.typeName ?? meta?.name ?? '',
          content: entry.content ?? '',
          createdAt: entry.createdAt ?? null,
          updatedAt: entry.updatedAt ?? null
        }
      })
      const missingEntries = dictionary
        .filter(type => !mappedEntries.some(entry => entry.patientInformationTypeId === type.id))
        .map(type => ({
          patientInformationTypeId: type.id,
          typeName: type.name,
          content: '',
          createdAt: null,
          updatedAt: null
        }))

      const getOrder = (typeId: number) =>
        dictionary.find(type => type.id === typeId)?.displayOrder ?? Number.MAX_SAFE_INTEGER

      setInformationEntries([...mappedEntries, ...missingEntries].sort((a, b) => getOrder(a.patientInformationTypeId) - getOrder(b.patientInformationTypeId)))
    } catch (err) {
      console.error('Error loading patient:', err)
    } finally {
      setLoading(false)
    }
  }

  function handleInformationEntryChange(typeId: number, value: string) {
    setInformationEntries(prev => {
      const existingEntry = prev.find(entry => entry.patientInformationTypeId === typeId)
      if (existingEntry) {
        return prev.map(entry => entry.patientInformationTypeId === typeId
          ? { ...entry, content: value }
          : entry)
      } else {
        const type = informationTypes.find(t => t.id === typeId)
        return [...prev, {
          patientInformationTypeId: typeId,
          typeName: type?.name ?? '',
          content: value,
          createdAt: null,
          updatedAt: null
        }]
      }
    })
  }

  async function save() {
    setSaving(true)
    try {
      const token = localStorage.getItem('token')
      const url = id ? `/api/patients/${id}` : '/api/patients'
      const method = id ? 'PUT' : 'POST'
      const payload: any = {
        ...form,
        lastSessionSummary: form.lastSessionSummary,
        informationEntries: informationEntries.map(entry => ({
          patientInformationTypeId: entry.patientInformationTypeId,
          content: entry.content?.trim() ? entry.content.trim() : null
        })),
        dateOfBirth: form.dateOfBirth ? new Date(form.dateOfBirth).toISOString() : null
      }
      
      // Dodaj zarządzanie hasłem
      if (!id) {
        // Tworzenie nowego pacjenta
        if (createUserAccount && password) {
          payload.password = password
          payload.createUserAccount = true
        } else if (password) {
          // Jeśli podano hasło bez checkboxa, też utwórz konto
          payload.password = password
          payload.createUserAccount = true
        }
      } else {
        // Edycja istniejącego pacjenta
        if (!hasUserAccount && (createUserAccount || password)) {
          if (password) {
            payload.password = password
            // Jeśli podano hasło, utwórz konto
            payload.createUserAccount = true
          }
        } else if (hasUserAccount && newPassword) {
          payload.newPassword = newPassword
        }
      }
      const res = await fetch(url, {
        method,
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      if (res.ok) navigate('/patients')
      else {
        const error = await res.text()
        alert(`Błąd: ${error}`)
      }
    } catch (err) {
      console.error('Error saving patient:', err)
      alert('Błąd podczas zapisywania')
    } finally {
      setSaving(false)
    }
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('roles')
    navigate('/login')
  }

  if (loading) return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>

  const selectedEntry = informationEntries.find(e => e.patientInformationTypeId === selectedInfoTypeId)

  return (
    <div className="min-h-screen bg-gray-50">
      <NavBar culture={culture} setCulture={setCulture} onLogout={logout} navigate={navigate} />
      <main className="w-full py-8 px-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-3xl font-bold text-gray-800">{id && form.firstName && form.lastName ? `${form.firstName} ${form.lastName}` : (id ? t['patients.edit'] ?? 'Edytuj pacjenta' : t['patients.new'] ?? 'Nowy pacjent')}</h1>
          <div className="flex gap-4">
            <button onClick={save} disabled={saving} className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 shadow-md transition-all font-medium">
              {t['sessions.save'] ?? 'Zapisz'}
            </button>
            <button onClick={() => navigate('/patients')} className="bg-gray-600 text-white px-6 py-2 rounded-lg hover:bg-gray-700 shadow-md transition-all font-medium">Anuluj</button>
          </div>
        </div>
        
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Lewa kolumna - Nawigacja z akordeonem */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-lg shadow-lg border border-gray-200 sticky top-4">
              {/* Grupa: Dane podstawowe */}
              <div className="border-b border-gray-200">
                <button
                  onClick={() => toggleGroup('basic')}
                  className="w-full px-4 py-3 text-left font-semibold text-gray-800 hover:bg-gray-50 transition-colors flex items-center justify-between"
                >
                  <span>Dane podstawowe</span>
                  <svg
                    className={`w-5 h-5 transform transition-transform ${expandedGroups.has('basic') ? 'rotate-180' : ''}`}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
                {expandedGroups.has('basic') && (
                  <div className="px-2 pb-2 space-y-1">
                    <button
                      onClick={() => handleSectionSelect('personal')}
                      className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                        selectedSection === 'personal'
                          ? 'bg-blue-600 text-white shadow-md'
                          : 'text-gray-700 hover:bg-gray-100'
                      }`}
                    >
                      Dane osobowe
                    </button>
                    <button
                      onClick={() => handleSectionSelect('address')}
                      className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                        selectedSection === 'address'
                          ? 'bg-blue-600 text-white shadow-md'
                          : 'text-gray-700 hover:bg-gray-100'
                      }`}
                    >
                      Dane adresowe
                    </button>
                    <button
                      onClick={() => handleSectionSelect('summary')}
                      className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                        selectedSection === 'summary'
                          ? 'bg-blue-600 text-white shadow-md'
                          : 'text-gray-700 hover:bg-gray-100'
                      }`}
                    >
                      Podsumowanie sesji
                    </button>
                    <button
                      onClick={() => handleSectionSelect('account')}
                      className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                        selectedSection === 'account'
                          ? 'bg-blue-600 text-white shadow-md'
                          : 'text-gray-700 hover:bg-gray-100'
                      }`}
                    >
                      Konto użytkownika
                    </button>
                  </div>
                )}
              </div>

              {/* Grupa: Informacje terapeutyczne */}
              <div>
                <button
                  onClick={() => toggleGroup('therapeutic')}
                  className="w-full px-4 py-3 text-left font-semibold text-gray-800 hover:bg-gray-50 transition-colors flex items-center justify-between"
                >
                  <span>Informacje terapeutyczne</span>
                  <svg
                    className={`w-5 h-5 transform transition-transform ${expandedGroups.has('therapeutic') ? 'rotate-180' : ''}`}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
                {expandedGroups.has('therapeutic') && (
                  <div className="px-2 pb-2 space-y-1 max-h-[600px] overflow-y-auto">
                    {(() => {
                      const conceptualizationEntry = informationEntries.find(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code === 'CONCEPTUALIZATION'
                      })
                      // Konceptualizacja nie ma już podsekcji - jest zwykłą pozycją listy
                      const smartGoalsEntry = informationEntries.find(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code === 'SMART_GOALS'
                      })
                      // Cele SMART nie mają już podsekcji w liście - są zwykłą pozycją listy
                      const problemDefinitionEntry = informationEntries.find(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code === 'PROBLEM_DEFINITION'
                      })
                      const problemDefinitionSubsections = informationEntries.filter(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code === 'PROBLEM_DEFINITION_MAIN_SYMPTOMS' || 
                               type?.code === 'PROBLEM_DEFINITION_DAILY_IMPACT' || 
                               type?.code === 'PROBLEM_DEFINITION_SEVERITY' ||
                               type?.code === 'PROBLEM_DEFINITION_AFFECTED_AREAS' ||
                               type?.code === 'PROBLEM_DEFINITION_PRIORITIZATION' ||
                               type?.code === 'PROBLEM_DEFINITION_SOLUTION_MEASURE' ||
                               type?.code === 'PROBLEM_DEFINITION_SELF_WORK'
                      })
                      const developmentalInterviewEntry = informationEntries.find(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code === 'DEVELOPMENTAL_INTERVIEW'
                      })
                      const developmentalInterviewSubsections = informationEntries.filter(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code === 'DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES' || 
                               type?.code === 'DEVELOPMENTAL_INTERVIEW_ADOLESCENCE' || 
                               type?.code === 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL' ||
                               type?.code === 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY'
                      })
                      const otherEntries = informationEntries.filter(e => {
                        const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                        return type?.code !== 'CONCEPTUALIZATION' &&
                               type?.code !== 'CONCEPTUALIZATION_LEVEL1' &&
                               type?.code !== 'CONCEPTUALIZATION_LEVEL2' &&
                               type?.code !== 'CONCEPTUALIZATION_SUMMARY' &&
                               type?.code !== 'SMART_GOALS' &&
                               type?.code !== 'SMART_GOALS_CONNECTIONS' &&
                               type?.code !== 'SMART_GOALS_DEFINITION' &&
                               type?.code !== 'SMART_GOALS_METRICS' &&
                               type?.code !== 'SMART_GOALS_ACTION_PLAN' &&
                               type?.code !== 'SMART_GOALS_BARRIERS' &&
                               type?.code !== 'SMART_GOALS_REVIEW' &&
                               type?.code !== 'SMART_GOALS_PRIORITY' &&
                               type?.code !== 'PROBLEM_DEFINITION' &&
                               type?.code !== 'PROBLEM_DEFINITION_MAIN_SYMPTOMS' &&
                               type?.code !== 'PROBLEM_DEFINITION_DAILY_IMPACT' &&
                               type?.code !== 'PROBLEM_DEFINITION_SEVERITY' &&
                               type?.code !== 'PROBLEM_DEFINITION_AFFECTED_AREAS' &&
                               type?.code !== 'PROBLEM_DEFINITION_PRIORITIZATION' &&
                               type?.code !== 'PROBLEM_DEFINITION_SOLUTION_MEASURE' &&
                               type?.code !== 'PROBLEM_DEFINITION_SELF_WORK' &&
                               type?.code !== 'DEVELOPMENTAL_INTERVIEW' &&
                               type?.code !== 'DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES' &&
                               type?.code !== 'DEVELOPMENTAL_INTERVIEW_ADOLESCENCE' &&
                               type?.code !== 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL' &&
                               type?.code !== 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY'
                      })

                      return (
                        <>
                          {conceptualizationEntry && (
                            <button
                              onClick={() => handleSectionSelect('info', conceptualizationEntry.patientInformationTypeId)}
                              className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                                selectedSection === 'info' && selectedInfoTypeId === conceptualizationEntry.patientInformationTypeId
                                  ? 'bg-blue-600 text-white shadow-md'
                                  : 'text-gray-700 hover:bg-gray-100'
                              }`}
                            >
                              <span className="flex-1 truncate">{conceptualizationEntry.typeName}</span>
                            </button>
                          )}
                          {smartGoalsEntry && (
                            <button
                              onClick={() => handleSectionSelect('info', smartGoalsEntry.patientInformationTypeId)}
                              className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                                selectedSection === 'info' && selectedInfoTypeId === smartGoalsEntry.patientInformationTypeId
                                  ? 'bg-blue-600 text-white shadow-md'
                                  : 'text-gray-700 hover:bg-gray-100'
                              }`}
                            >
                              <span className="flex-1 truncate">{smartGoalsEntry.typeName}</span>
                            </button>
                          )}
                          {problemDefinitionEntry && (
                            <>
                              <button
                                onClick={() => {
                                  const newExpanded = !expandedProblemDefinition
                                  setExpandedProblemDefinition(newExpanded)
                                  if (newExpanded) {
                                    handleSectionSelect('info', problemDefinitionEntry.patientInformationTypeId)
                                  }
                                }}
                                className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all flex items-center justify-between ${
                                  selectedSection === 'info' && selectedInfoTypeId === problemDefinitionEntry.patientInformationTypeId
                                    ? 'bg-blue-600 text-white shadow-md'
                                    : 'text-gray-700 hover:bg-gray-100'
                                }`}
                              >
                                <span className="flex-1 truncate">{problemDefinitionEntry.typeName}</span>
                                <svg
                                  className={`w-4 h-4 transform transition-transform ${expandedProblemDefinition ? 'rotate-180' : ''}`}
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                                </svg>
                              </button>
                              {expandedProblemDefinition && (
                                <div className="ml-4 space-y-1">
                                  {problemDefinitionSubsections.map(subsection => {
                                    const isSelected = selectedSection === 'info' && subsection.patientInformationTypeId === selectedInfoTypeId
                                    const hasContent = subsection.content && subsection.content.trim().length > 0
                                    return (
                                      <button
                                        key={subsection.patientInformationTypeId}
                                        onClick={() => handleSectionSelect('info', subsection.patientInformationTypeId)}
                                        className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all flex items-center justify-between ${
                                          isSelected
                                            ? 'bg-blue-600 text-white shadow-md'
                                            : 'text-gray-700 hover:bg-gray-100'
                                        }`}
                                      >
                                        <span className="flex-1 truncate">{subsection.typeName}</span>
                                        {hasContent && (
                                          <span className={`ml-2 flex-shrink-0 text-xs ${isSelected ? 'text-blue-100' : 'text-green-600'}`}>
                                            â—Ź
                                          </span>
                                        )}
                                      </button>
                                    )
                                  })}
                                </div>
                              )}
                            </>
                          )}
                          {developmentalInterviewEntry && (
                            <button
                              onClick={() => handleSectionSelect('info', developmentalInterviewEntry.patientInformationTypeId)}
                              className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all ${
                                selectedSection === 'info' && selectedInfoTypeId === developmentalInterviewEntry.patientInformationTypeId
                                  ? 'bg-blue-600 text-white shadow-md'
                                  : 'text-gray-700 hover:bg-gray-100'
                              }`}
                            >
                              <span className="flex-1 truncate">{developmentalInterviewEntry.typeName}</span>
                            </button>
                          )}
                          {otherEntries.map(entry => {
                            const isSelected = selectedSection === 'info' && entry.patientInformationTypeId === selectedInfoTypeId
                            const hasContent = entry.content && entry.content.trim().length > 0
                            return (
                              <button
                                key={entry.patientInformationTypeId}
                                onClick={() => handleSectionSelect('info', entry.patientInformationTypeId)}
                                className={`w-full text-left px-3 py-2 rounded-md text-sm transition-all flex items-center justify-between ${
                                  isSelected
                                    ? 'bg-blue-600 text-white shadow-md'
                                    : 'text-gray-700 hover:bg-gray-100'
                                }`}
                              >
                                <span className="flex-1 truncate">{entry.typeName}</span>
                                {hasContent && (
                                  <span className={`ml-2 flex-shrink-0 text-xs ${isSelected ? 'text-blue-100' : 'text-green-600'}`}>
                                    â—Ź
                                  </span>
                                )}
                              </button>
                            )
                          })}
                        </>
                      )
                    })()}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Prawa kolumna - Zawartość wybranej sekcji */}
          <div className="lg:col-span-10">
            <div className="bg-white rounded-lg shadow-lg border border-gray-200 p-6 min-h-[600px]">
              {selectedSection === 'personal' && (
                <div className="space-y-6">
                  <h2 className="text-2xl font-bold text-gray-800 mb-6">Dane osobowe</h2>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.firstName'] ?? 'Imię'}</label>
                      <input value={form.firstName} onChange={e=>setForm({...form, firstName: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" required />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.lastName'] ?? 'Nazwisko'}</label>
                      <input value={form.lastName} onChange={e=>setForm({...form, lastName: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" required />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.email'] ?? 'E-mail'}</label>
                      <input type="email" value={form.email} onChange={e=>setForm({...form, email: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" required />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.phone'] ?? 'Telefon'}</label>
                      <input type="tel" value={form.phone} onChange={e=>setForm({...form, phone: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.dateOfBirth'] ?? 'Data urodzenia'}</label>
                      <input type="date" value={form.dateOfBirth} onChange={e=>setForm({...form, dateOfBirth: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.gender'] ?? 'Płeć'}</label>
                      <select value={form.gender} onChange={e=>setForm({...form, gender: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2">
                        <option value="">-- Wybierz --</option>
                        <option value="M">Mężczyzna</option>
                        <option value="F">Kobieta</option>
                        <option value="Other">Inna</option>
                      </select>
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.pesel'] ?? 'PESEL'}</label>
                      <input value={form.pesel} onChange={e=>setForm({...form, pesel: e.target.value})} maxLength={11} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                  </div>
                </div>
              )}

              {selectedSection === 'address' && (
                <div className="space-y-6">
                  <h2 className="text-2xl font-bold text-gray-800 mb-6">Dane adresowe</h2>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <div className="md:col-span-2 lg:col-span-3 form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.street'] ?? 'Ulica'}</label>
                      <input value={form.street} onChange={e=>setForm({...form, street: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.streetNumber'] ?? 'Numer'}</label>
                      <input value={form.streetNumber} onChange={e=>setForm({...form, streetNumber: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.apartmentNumber'] ?? 'Nr lokalu'}</label>
                      <input value={form.apartmentNumber} onChange={e=>setForm({...form, apartmentNumber: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.city'] ?? 'Miasto'}</label>
                      <input value={form.city} onChange={e=>setForm({...form, city: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.postalCode'] ?? 'Kod pocztowy'}</label>
                      <input value={form.postalCode} onChange={e=>setForm({...form, postalCode: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                    <div className="form-group">
                      <label className="block text-sm font-medium text-gray-700 mb-2">{t['patients.country'] ?? 'Kraj'}</label>
                      <input value={form.country} onChange={e=>setForm({...form, country: e.target.value})} className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2" />
                    </div>
                  </div>
                </div>
              )}

              {selectedSection === 'summary' && (
                <div className="space-y-6">
                  <h2 className="text-2xl font-bold text-gray-800 mb-6">{t['patients.lastSessionSummary'] ?? 'Podsumowanie ostatniej sesji'}</h2>
                  <textarea
                    value={form.lastSessionSummary}
                    onChange={e=>setForm({...form, lastSessionSummary: e.target.value})}
                    className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                    rows={15}
                    placeholder="Wprowadź podsumowanie ostatniej sesji..."
                  />
                </div>
              )}

              {selectedSection === 'account' && (
                <div className="space-y-6">
                  <h2 className="text-2xl font-bold text-gray-800 mb-6">Konto użytkownika</h2>
                  
                  {id && hasUserAccount && (
                    <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg">
                      <div className="flex items-center gap-2">
                        <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <p className="text-green-800 font-medium">Pacjent ma aktywne konto użytkownika</p>
                      </div>
                      <p className="text-sm text-green-700 mt-2">Pacjent może logować się do systemu używając adresu email: <strong>{form.email}</strong></p>
                    </div>
                  )}

                  {id && !hasUserAccount && (
                    <div className="mb-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                      <div className="flex items-center gap-2">
                        <svg className="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                        <p className="text-yellow-800 font-medium">Pacjent nie ma konta użytkownika</p>
                      </div>
                      <p className="text-sm text-yellow-700 mt-2">Możesz utworzyć konto użytkownika dla tego pacjenta, aby mógł logować się do systemu.</p>
                    </div>
                  )}

                  {!id && (
                    <div className="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                      <p className="text-blue-800 font-medium">Utworzenie konta użytkownika (opcjonalne)</p>
                      <p className="text-sm text-blue-700 mt-2">Możesz od razu utworzyć konto użytkownika dla pacjenta, aby mógł logować się do systemu.</p>
                    </div>
                  )}

                  <div className="bg-white p-6 rounded-lg border border-gray-200 space-y-6">
                    {!id ? (
                      // Tworzenie nowego pacjenta
                      <>
                        <div className="flex items-center gap-3">
                          <input
                            type="checkbox"
                            id="createAccount"
                            checked={createUserAccount}
                            onChange={e => {
                              setCreateUserAccount(e.target.checked)
                              if (!e.target.checked) setPassword('')
                            }}
                            className="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                          />
                          <label htmlFor="createAccount" className="text-gray-700 font-medium cursor-pointer">
                            Utwórz konto użytkownika dla tego pacjenta
                          </label>
                        </div>
                        
                        {createUserAccount && (
                          <div className="ml-8 space-y-4">
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-2">
                                Hasło dostępu *
                              </label>
                              <input
                                type="password"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                                placeholder="Wprowadź hasło dla pacjenta"
                                required={createUserAccount}
                              />
                              <p className="text-xs text-gray-500 mt-1">Pacjent będzie mógł logować się używając adresu email i tego hasła</p>
                            </div>
                          </div>
                        )}
                      </>
                    ) : !hasUserAccount ? (
                      // Edycja - pacjent nie ma konta
                      <>
                        <div className="flex items-center gap-3">
                          <input
                            type="checkbox"
                            id="createAccount"
                            checked={createUserAccount}
                            onChange={e => {
                              setCreateUserAccount(e.target.checked)
                              if (!e.target.checked) setPassword('')
                            }}
                            className="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                          />
                          <label htmlFor="createAccount" className="text-gray-700 font-medium cursor-pointer">
                            Utwórz konto użytkownika dla tego pacjenta
                          </label>
                        </div>
                        
                        {(createUserAccount || password) && (
                          <div className="ml-8 space-y-4">
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-2">
                                Hasło dostępu *
                              </label>
                              <input
                                type="password"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                                placeholder="Wprowadź hasło dla pacjenta"
                                required={createUserAccount || !!password}
                              />
                              <p className="text-xs text-gray-500 mt-1">Pacjent będzie mógł logować się używając adresu email i tego hasła</p>
                            </div>
                          </div>
                        )}
                      </>
                    ) : (
                      // Edycja - pacjent ma konto
                      <div className="space-y-4">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">
                            Nowe hasło (pozostaw puste, aby nie zmieniać)
                          </label>
                          <input
                            type="password"
                            value={newPassword}
                            onChange={e => setNewPassword(e.target.value)}
                            className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2"
                            placeholder="Wprowadź nowe hasło"
                          />
                          <p className="text-xs text-gray-500 mt-1">Jeśli wprowadzisz nowe hasło, zostanie ono zmienione. Stare hasło nie jest wymagane.</p>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {selectedSection === 'info' && selectedEntry ? (
                (() => {
                  const selectedType = informationTypes.find(t => t.id === selectedEntry.patientInformationTypeId)
                  const isConceptualization = selectedType?.code === 'CONCEPTUALIZATION'
                  const isConceptualizationSubsection = selectedType?.code === 'CONCEPTUALIZATION_LEVEL1' ||
                                                         selectedType?.code === 'CONCEPTUALIZATION_LEVEL2' ||
                                                         selectedType?.code === 'CONCEPTUALIZATION_SUMMARY'
                  const isSmartGoals = selectedType?.code === 'SMART_GOALS'
                  const isProblemDefinition = selectedType?.code === 'PROBLEM_DEFINITION'
                  const isProblemDefinitionSubsection = selectedType?.code === 'PROBLEM_DEFINITION_MAIN_SYMPTOMS' ||
                                                        selectedType?.code === 'PROBLEM_DEFINITION_DAILY_IMPACT' ||
                                                        selectedType?.code === 'PROBLEM_DEFINITION_SEVERITY' ||
                                                        selectedType?.code === 'PROBLEM_DEFINITION_AFFECTED_AREAS' ||
                                                        selectedType?.code === 'PROBLEM_DEFINITION_PRIORITIZATION' ||
                                                        selectedType?.code === 'PROBLEM_DEFINITION_SOLUTION_MEASURE' ||
                                                        selectedType?.code === 'PROBLEM_DEFINITION_SELF_WORK'
                  const isDevelopmentalInterview = selectedType?.code === 'DEVELOPMENTAL_INTERVIEW'
                  const isDevelopmentalInterviewSubsection = selectedType?.code === 'DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES' ||
                                                             selectedType?.code === 'DEVELOPMENTAL_INTERVIEW_ADOLESCENCE' ||
                                                             selectedType?.code === 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL' ||
                                                             selectedType?.code === 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY'
                  
                  // Podsekcje Konceptualizacji nie powinny być wyświetlane jako osobne sekcje
                  // są one dostępne tylko w głównej sekcji Konceptualizacji
                  if (isConceptualizationSubsection) {
                    return null
                  }
                  
                  if (isConceptualization) {
                    const level1Entry = informationEntries.find(e => {
                      const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                      return type?.code === 'CONCEPTUALIZATION_LEVEL1'
                    })
                    const level2Entry = informationEntries.find(e => {
                      const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                      return type?.code === 'CONCEPTUALIZATION_LEVEL2'
                    })
                    const summaryEntry = informationEntries.find(e => {
                      const type = informationTypes.find(t => t.id === e.patientInformationTypeId)
                      return type?.code === 'CONCEPTUALIZATION_SUMMARY'
                    })

                    // Funkcja pomocnicza do tworzenia lub znajdowania wpisu
                    const getOrCreateEntry = (code: string) => {
                      const type = informationTypes.find(t => t.code === code)
                      if (!type) return null
                      const entry = informationEntries.find(e => e.patientInformationTypeId === type.id)
                      return entry || {
                        patientInformationTypeId: type.id,
                        typeName: type.name,
                        content: '',
                        createdAt: null,
                        updatedAt: null
                      }
                    }

                    const level1 = level1Entry || getOrCreateEntry('CONCEPTUALIZATION_LEVEL1')
                    const level2 = level2Entry || getOrCreateEntry('CONCEPTUALIZATION_LEVEL2')
                    const summary = summaryEntry || getOrCreateEntry('CONCEPTUALIZATION_SUMMARY')

                    return (
                      <div className="space-y-6">
                        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                          <h2 className="text-2xl font-bold text-gray-800">{selectedEntry.typeName}</h2>
                        </div>
                        
                        {level1 && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Poziom 1: "Jak?" (mapa procesów)
                            </label>
                            <textarea
                              value={level1.content || ''}
                              onChange={e => handleInformationEntryChange(level1.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder='Wprowadź informacje dotyczące: Poziom 1: "Jak?" (mapa procesów)'
                              rows={10}
                            />
                            {(level1.updatedAt ?? level1.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(level1.updatedAt ?? level1.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {level2 && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Poziom 2: "Dlaczego?" (mechanizmy podtrzymujące)
                            </label>
                            <textarea
                              value={level2.content || ''}
                              onChange={e => handleInformationEntryChange(level2.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder='Wprowadź informacje dotyczące: Poziom 2: "Dlaczego?" (mechanizmy podtrzymujące)'
                              rows={10}
                            />
                            {(level2.updatedAt ?? level2.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(level2.updatedAt ?? level2.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {summary && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Podsumowanie: "Co zmieniamy?" (cele zmiany)
                            </label>
                            <textarea
                              value={summary.content || ''}
                              onChange={e => handleInformationEntryChange(summary.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder='Wprowadź informacje dotyczące: Podsumowanie: "Co zmieniamy?" (cele zmiany)'
                              rows={10}
                            />
                            {(summary.updatedAt ?? summary.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(summary.updatedAt ?? summary.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}
                      </div>
                    )
                  }

                  if (isSmartGoals) {
                    const getEntry = (code: string) => {
                      const type = informationTypes.find(t => t.code === code)
                      if (!type) return null
                      const entry = informationEntries.find(e => e.patientInformationTypeId === type.id)
                      return entry || {
                        patientInformationTypeId: type.id,
                        typeName: type.name,
                        content: '',
                        createdAt: null,
                        updatedAt: null
                      }
                    }

                    const connectionsEntry = getEntry('SMART_GOALS_CONNECTIONS')
                    const definitionEntry = getEntry('SMART_GOALS_DEFINITION')
                    const metricsEntry = getEntry('SMART_GOALS_METRICS')
                    const actionPlanEntry = getEntry('SMART_GOALS_ACTION_PLAN')
                    const barriersEntry = getEntry('SMART_GOALS_BARRIERS')
                    const reviewEntry = getEntry('SMART_GOALS_REVIEW')
                    const priorityEntry = getEntry('SMART_GOALS_PRIORITY')

                    return (
                      <div className="space-y-6">
                        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                          <h2 className="text-2xl font-bold text-gray-800">{selectedEntry.typeName}</h2>
                        </div>
                        
                        {connectionsEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Powiązania
                            </label>
                            <textarea
                              value={connectionsEntry.content || ''}
                              onChange={e => handleInformationEntryChange(connectionsEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Powiązania celu z diagnozą, wartościami, obszarami życia..."
                              rows={10}
                            />
                            {(connectionsEntry.updatedAt ?? connectionsEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(connectionsEntry.updatedAt ?? connectionsEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {definitionEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Definicja SMART
                            </label>
                            <textarea
                              value={definitionEntry.content || ''}
                              onChange={e => handleInformationEntryChange(definitionEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Szczegółowa definicja: Specyficzny, Mierzalny, Atrakcyjny, Realny, Terminowy..."
                              rows={10}
                            />
                            {(definitionEntry.updatedAt ?? definitionEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(definitionEntry.updatedAt ?? definitionEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {metricsEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Metryka i monitoring
                            </label>
                            <textarea
                              value={metricsEntry.content || ''}
                              onChange={e => handleInformationEntryChange(metricsEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Jak będziemy mierzyć postęp? Jak często monitorować?"
                              rows={10}
                            />
                            {(metricsEntry.updatedAt ?? metricsEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(metricsEntry.updatedAt ?? metricsEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {actionPlanEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Plan działania
                            </label>
                            <textarea
                              value={actionPlanEntry.content || ''}
                              onChange={e => handleInformationEntryChange(actionPlanEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Konkretny plan działań krok po kroku..."
                              rows={10}
                            />
                            {(actionPlanEntry.updatedAt ?? actionPlanEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(actionPlanEntry.updatedAt ?? actionPlanEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {barriersEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Bariery i wsparcie
                            </label>
                            <textarea
                              value={barriersEntry.content || ''}
                              onChange={e => handleInformationEntryChange(barriersEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Potencjalne przeszkody oraz dostępne zasoby/wsparcie..."
                              rows={10}
                            />
                            {(barriersEntry.updatedAt ?? barriersEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(barriersEntry.updatedAt ?? barriersEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {reviewEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Przegląd i weryfikacja
                            </label>
                            <textarea
                              value={reviewEntry.content || ''}
                              onChange={e => handleInformationEntryChange(reviewEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Kiedy i jak weryfikujemy postęp oraz trafność celu..."
                              rows={10}
                            />
                            {(reviewEntry.updatedAt ?? reviewEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(reviewEntry.updatedAt ?? reviewEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {priorityEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Priorytet
                            </label>
                            <textarea
                              value={priorityEntry.content || ''}
                              onChange={e => handleInformationEntryChange(priorityEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[200px]"
                              placeholder="Jak ważny jest ten cel względem innych? Uzasadnij priorytet..."
                              rows={10}
                            />
                            {(priorityEntry.updatedAt ?? priorityEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(priorityEntry.updatedAt ?? priorityEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}
                      </div>
                    )
                  }

                  if (isProblemDefinitionSubsection) {
                    const getLabelForSubsection = (code: string | undefined) => {
                      switch (code) {
                        case 'PROBLEM_DEFINITION_MAIN_SYMPTOMS': return 'Główne objawy i problemy'
                        case 'PROBLEM_DEFINITION_DAILY_IMPACT': return 'Wpływ na codzienne funkcjonowanie'
                        case 'PROBLEM_DEFINITION_SEVERITY': return 'Nasilenie i częstotliwość'
                        case 'PROBLEM_DEFINITION_AFFECTED_AREAS': return 'Obszary życia najbardziej dotknięte'
                        case 'PROBLEM_DEFINITION_PRIORITIZATION': return 'Priorytetyzacja problemów'
                        case 'PROBLEM_DEFINITION_SOLUTION_MEASURE': return 'Miara rozwiązania - skąd pacjent będzie wiedział, że problem się rozwiązał?'
                        case 'PROBLEM_DEFINITION_SELF_WORK': return 'Proponowana Praca własna'
                        default: return selectedEntry.typeName
                      }
                    }
                    const getPlaceholderForSubsection = (code: string | undefined) => {
                      switch (code) {
                        case 'PROBLEM_DEFINITION_MAIN_SYMPTOMS': return 'Wypisz kluczowe objawy i problemy do pracy...'
                        case 'PROBLEM_DEFINITION_DAILY_IMPACT': return 'Jak problem wpływa na codzienne funkcjonowanie pacjenta...'
                        case 'PROBLEM_DEFINITION_SEVERITY': return 'Opisz nasilenie i częstotliwość występowania objawów...'
                        case 'PROBLEM_DEFINITION_AFFECTED_AREAS': return 'Które obszary życia są najbardziej dotknięte (praca, relacje, zdrowie itd.)...'
                        case 'PROBLEM_DEFINITION_PRIORITIZATION': return 'Które problemy są najbardziej istotne do podjęcia w pierwszej kolejności i dlaczego...'
                        case 'PROBLEM_DEFINITION_SOLUTION_MEASURE': return 'Konkretne, obserwowalne wskaźniki poprawy lub rozwiązania problemu...'
                        case 'PROBLEM_DEFINITION_SELF_WORK': return 'Propozycja pracy własnej dla pacjenta...'
                        default: return `Wprowadź informacje dotyczące: ${selectedEntry.typeName}`
                      }
                    }

                    return (
                      <div className="space-y-6">
                        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                          <h2 className="text-2xl font-bold text-gray-800">{getLabelForSubsection(selectedType?.code)}</h2>
                          {(selectedEntry.updatedAt ?? selectedEntry.createdAt) && (
                            <span className="text-sm text-gray-500">
                              {(t['patients.informationPanel.lastUpdated'] ?? 'Ostatnia aktualizacja')}: {formatDateWithZone(selectedEntry.updatedAt ?? selectedEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                            </span>
                          )}
                        </div>
                        <textarea
                          value={selectedEntry.content || ''}
                          onChange={e => handleInformationEntryChange(selectedEntry.patientInformationTypeId, e.target.value)}
                          className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[300px] resize-y"
                          placeholder={getPlaceholderForSubsection(selectedType?.code)}
                          rows={12}
                        />
                      </div>
                    )
                  }

                  if (isDevelopmentalInterviewSubsection) {
                    const getLabelForSubsection = (code: string | undefined) => {
                      switch (code) {
                        case 'DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES': return 'Wcześne doświadczenia'
                        case 'DEVELOPMENTAL_INTERVIEW_ADOLESCENCE': return 'Adolescencja'
                        case 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL': return 'Dorosłość - Pytania ogólne'
                        case 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY': return 'Dorosłość - Pytania w kierunku cech nieprawidłowej osobowości'
                        default: return selectedEntry.typeName
                      }
                    }
                    const getPlaceholderForSubsection = (code: string | undefined) => {
                      switch (code) {
                        case 'DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES': return 'Wprowadź informacje dotyczące wczesnych doświadczeń...'
                        case 'DEVELOPMENTAL_INTERVIEW_ADOLESCENCE': return 'Wprowadź informacje dotyczące adolescencji...'
                        case 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL': return 'Wprowadź informacje dotyczące dorosłości - pytania ogólne...'
                        case 'DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY': return 'Wprowadź informacje dotyczące dorosłości - pytania w kierunku cech nieprawidłowej osobowości...'
                        default: return `Wprowadź informacje dotyczące: ${selectedEntry.typeName}`
                      }
                    }

                    return (
                      <div className="space-y-6">
                        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                          <h2 className="text-2xl font-bold text-gray-800">{getLabelForSubsection(selectedType?.code)}</h2>
                          {(selectedEntry.updatedAt ?? selectedEntry.createdAt) && (
                            <span className="text-sm text-gray-500">
                              {(t['patients.informationPanel.lastUpdated'] ?? 'Ostatnia aktualizacja')}: {formatDateWithZone(selectedEntry.updatedAt ?? selectedEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                            </span>
                          )}
                        </div>
                        <textarea
                          value={selectedEntry.content || ''}
                          onChange={e => handleInformationEntryChange(selectedEntry.patientInformationTypeId, e.target.value)}
                          className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[300px] resize-y"
                          placeholder={getPlaceholderForSubsection(selectedType?.code)}
                          rows={12}
                        />
                      </div>
                    )
                  }

                  if (isProblemDefinition) {
                    // Funkcja pomocnicza do tworzenia lub znajdowania wpisu
                    const getOrCreateEntry = (code: string) => {
                      const type = informationTypes.find(t => t.code === code)
                      if (!type) return null
                      const entry = informationEntries.find(e => e.patientInformationTypeId === type.id)
                      return entry || {
                        patientInformationTypeId: type.id,
                        typeName: type.name,
                        content: '',
                        createdAt: null,
                        updatedAt: null
                      }
                    }

                    const mainSymptomsEntry = getOrCreateEntry('PROBLEM_DEFINITION_MAIN_SYMPTOMS')
                    const dailyImpactEntry = getOrCreateEntry('PROBLEM_DEFINITION_DAILY_IMPACT')
                    const severityEntry = getOrCreateEntry('PROBLEM_DEFINITION_SEVERITY')
                    const affectedAreasEntry = getOrCreateEntry('PROBLEM_DEFINITION_AFFECTED_AREAS')
                    const prioritizationEntry = getOrCreateEntry('PROBLEM_DEFINITION_PRIORITIZATION')
                    const solutionMeasureEntry = getOrCreateEntry('PROBLEM_DEFINITION_SOLUTION_MEASURE')
                    const selfWorkEntry = getOrCreateEntry('PROBLEM_DEFINITION_SELF_WORK')

                    return (
                      <div className="space-y-6">
                        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                          <h2 className="text-2xl font-bold text-gray-800">{selectedEntry.typeName}</h2>
                        </div>
                        
                        {mainSymptomsEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Główne objawy i problemy
                            </label>
                            <textarea
                              value={mainSymptomsEntry.content || ''}
                              onChange={e => handleInformationEntryChange(mainSymptomsEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Wypisz kluczowe objawy i problemy do pracy..."
                              rows={6}
                            />
                            {(mainSymptomsEntry.updatedAt ?? mainSymptomsEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(mainSymptomsEntry.updatedAt ?? mainSymptomsEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {dailyImpactEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Wpływ na codzienne funkcjonowanie
                            </label>
                            <textarea
                              value={dailyImpactEntry.content || ''}
                              onChange={e => handleInformationEntryChange(dailyImpactEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Jak problem wpływa na codzienne funkcjonowanie pacjenta..."
                              rows={6}
                            />
                            {(dailyImpactEntry.updatedAt ?? dailyImpactEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(dailyImpactEntry.updatedAt ?? dailyImpactEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {severityEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Nasilenie i częstotliwość
                            </label>
                            <textarea
                              value={severityEntry.content || ''}
                              onChange={e => handleInformationEntryChange(severityEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Opisz nasilenie i częstotliwość występowania objawów..."
                              rows={6}
                            />
                            {(severityEntry.updatedAt ?? severityEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(severityEntry.updatedAt ?? severityEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {affectedAreasEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Obszary życia najbardziej dotknięte
                            </label>
                            <textarea
                              value={affectedAreasEntry.content || ''}
                              onChange={e => handleInformationEntryChange(affectedAreasEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Które obszary życia są najbardziej dotknięte (praca, relacje, zdrowie itd.)..."
                              rows={6}
                            />
                            {(affectedAreasEntry.updatedAt ?? affectedAreasEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(affectedAreasEntry.updatedAt ?? affectedAreasEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {prioritizationEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Priorytetyzacja problemów
                            </label>
                            <textarea
                              value={prioritizationEntry.content || ''}
                              onChange={e => handleInformationEntryChange(prioritizationEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Które problemy są najbardziej istotne do podjęcia w pierwszej kolejności i dlaczego..."
                              rows={6}
                            />
                            {(prioritizationEntry.updatedAt ?? prioritizationEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(prioritizationEntry.updatedAt ?? prioritizationEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {solutionMeasureEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Miara rozwiązania – skąd pacjent będzie wiedział, że problem się rozwiązał?
                            </label>
                            <textarea
                              value={solutionMeasureEntry.content || ''}
                              onChange={e => handleInformationEntryChange(solutionMeasureEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Konkretne, obserwowalne wskaźniki poprawy lub rozwiązania problemu..."
                              rows={6}
                            />
                            {(solutionMeasureEntry.updatedAt ?? solutionMeasureEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(solutionMeasureEntry.updatedAt ?? solutionMeasureEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {selfWorkEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Proponowana Praca własna
                            </label>
                            <textarea
                              value={selfWorkEntry.content || ''}
                              onChange={e => handleInformationEntryChange(selfWorkEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Propozycja pracy własnej dla pacjenta..."
                              rows={6}
                            />
                            {(selfWorkEntry.updatedAt ?? selfWorkEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(selfWorkEntry.updatedAt ?? selfWorkEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}
                      </div>
                    )
                  }

                  if (isDevelopmentalInterview) {
                    // Funkcja pomocnicza do tworzenia lub znajdowania wpisu
                    const getOrCreateEntry = (code: string) => {
                      const type = informationTypes.find(t => t.code === code)
                      if (!type) return null
                      const entry = informationEntries.find(e => e.patientInformationTypeId === type.id)
                      return entry || {
                        patientInformationTypeId: type.id,
                        typeName: type.name,
                        content: '',
                        createdAt: null,
                        updatedAt: null
                      }
                    }

                    const earlyExperiencesEntry = getOrCreateEntry('DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES')
                    const adolescenceEntry = getOrCreateEntry('DEVELOPMENTAL_INTERVIEW_ADOLESCENCE')
                    const adulthoodGeneralEntry = getOrCreateEntry('DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL')
                    const adulthoodPersonalityEntry = getOrCreateEntry('DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY')

                    return (
                      <div className="space-y-6">
                        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                          <h2 className="text-2xl font-bold text-gray-800">{selectedEntry.typeName}</h2>
                        </div>
                        
                        {earlyExperiencesEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Wcześne doświadczenia
                            </label>
                            <textarea
                              value={earlyExperiencesEntry.content || ''}
                              onChange={e => handleInformationEntryChange(earlyExperiencesEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Wprowadź informacje dotyczące wczesnych doświadczeń..."
                              rows={6}
                            />
                            {(earlyExperiencesEntry.updatedAt ?? earlyExperiencesEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(earlyExperiencesEntry.updatedAt ?? earlyExperiencesEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {adolescenceEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Adolescencja
                            </label>
                            <textarea
                              value={adolescenceEntry.content || ''}
                              onChange={e => handleInformationEntryChange(adolescenceEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Wprowadź informacje dotyczące adolescencji..."
                              rows={6}
                            />
                            {(adolescenceEntry.updatedAt ?? adolescenceEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(adolescenceEntry.updatedAt ?? adolescenceEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {adulthoodGeneralEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Dorosłość - Pytania ogólne
                            </label>
                            <textarea
                              value={adulthoodGeneralEntry.content || ''}
                              onChange={e => handleInformationEntryChange(adulthoodGeneralEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Wprowadź informacje dotyczące dorosłości - pytania ogólne..."
                              rows={6}
                            />
                            {(adulthoodGeneralEntry.updatedAt ?? adulthoodGeneralEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(adulthoodGeneralEntry.updatedAt ?? adulthoodGeneralEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}

                        {adulthoodPersonalityEntry && (
                          <div className="space-y-2">
                            <label className="block text-lg font-semibold text-gray-700">
                              Dorosłość - Pytania w kierunku cech nieprawidłowej osobowości
                            </label>
                            <textarea
                              value={adulthoodPersonalityEntry.content || ''}
                              onChange={e => handleInformationEntryChange(adulthoodPersonalityEntry.patientInformationTypeId, e.target.value)}
                              className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[150px] resize-y"
                              placeholder="Wprowadź informacje dotyczące dorosłości - pytania w kierunku cech nieprawidłowej osobowości..."
                              rows={6}
                            />
                            {(adulthoodPersonalityEntry.updatedAt ?? adulthoodPersonalityEntry.createdAt) && (
                              <span className="text-xs text-gray-500">
                                Ostatnia aktualizacja: {formatDateWithZone(adulthoodPersonalityEntry.updatedAt ?? adulthoodPersonalityEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                              </span>
                            )}
                          </div>
                        )}
                      </div>
                    )
                  }

                  return (
                    <div className="space-y-6">
                      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2 mb-4">
                        <h2 className="text-2xl font-bold text-gray-800">{selectedEntry.typeName}</h2>
                        {(selectedEntry.updatedAt ?? selectedEntry.createdAt) && (
                          <span className="text-sm text-gray-500">
                            {(t['patients.informationPanel.lastUpdated'] ?? 'Ostatnia aktualizacja')}: {formatDateWithZone(selectedEntry.updatedAt ?? selectedEntry.createdAt!, culture, { year: 'numeric', month: '2-digit', day: '2-digit' })}
                          </span>
                        )}
                      </div>
                      <textarea
                        value={selectedEntry.content}
                        onChange={e => handleInformationEntryChange(selectedEntry.patientInformationTypeId, e.target.value)}
                        className="w-full border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 px-4 py-2 min-h-[500px]"
                        placeholder={`Wprowadź informacje dotyczące: ${selectedEntry.typeName}`}
                        rows={20}
                      />
                      {informationTypes.find(t => t.id === selectedEntry.patientInformationTypeId)?.description && (
                        <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                          <p className="text-sm text-gray-700 italic">
                            {informationTypes.find(t => t.id === selectedEntry.patientInformationTypeId)?.description}
                          </p>
                        </div>
                      )}
                    </div>
                  )
                })()
              ) : selectedSection === 'info' && !selectedEntry ? (
                <div className="flex items-center justify-center h-[600px] text-gray-500">
                  <div className="text-center">
                    <p className="text-lg mb-2">Wybierz sekcję z lewej strony</p>
                    <p className="text-sm">Aby wyświetlić lub edytować informacje terapeutyczne</p>
                  </div>
                </div>
              ) : !selectedSection ? (
                <div className="flex items-center justify-center h-[600px] text-gray-500">
                  <div className="text-center">
                    <p className="text-lg mb-2">Wybierz sekcję z lewej strony</p>
                    <p className="text-sm">Aby rozpocząć edycję danych pacjenta</p>
                  </div>
                </div>
              ) : null}
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}


