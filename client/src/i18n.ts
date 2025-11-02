export type Translations = Record<string, string>

export async function loadTranslations(culture: string): Promise<Translations> {
  const res = await fetch(`/api/i18n/${culture}`)
  if (!res.ok) return {}
  return await res.json()
}




