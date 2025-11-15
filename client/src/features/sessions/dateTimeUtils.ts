const LOCALE_BY_CULTURE = {
  pl: 'pl-PL',
  en: 'en-US'
} as const

export type SupportedCulture = keyof typeof LOCALE_BY_CULTURE

export const APP_TIME_ZONE = new Intl.DateTimeFormat().resolvedOptions().timeZone

export function ensureFullHour(value: string): string {
  if (!value) {
    return value
  }
  const [datePart, timePart] = value.split('T')
  if (!datePart || !timePart) {
    return value
  }
  const [hourPart] = timePart.split(':')
  const normalizedHour = (hourPart ?? '00').padStart(2, '0')
  return `${datePart}T${normalizedHour}:00`
}

export function formatForDateTimeLocalInput(date: Date | string): string {
  if (!date) {
    return ''
  }
  const value = typeof date === 'string' ? new Date(date) : new Date(date.getTime())
  if (Number.isNaN(value.getTime())) {
    return ''
  }
  const year = value.getFullYear()
  const month = String(value.getMonth() + 1).padStart(2, '0')
  const day = String(value.getDate()).padStart(2, '0')
  const hours = String(value.getHours()).padStart(2, '0')
  const minutes = String(value.getMinutes()).padStart(2, '0')
  return `${year}-${month}-${day}T${hours}:${minutes}`
}

export function formatTimeWithZone(date: Date | string, culture: SupportedCulture, timeZone: string = APP_TIME_ZONE): string {
  const locale = LOCALE_BY_CULTURE[culture]
  const value = typeof date === 'string' ? new Date(date) : date
  return value.toLocaleTimeString(locale, {
    hour: '2-digit',
    minute: '2-digit',
    timeZone
  })
}

export function formatDateTimeWithZone(date: Date | string, culture: SupportedCulture, timeZone: string = APP_TIME_ZONE): string {
  const locale = LOCALE_BY_CULTURE[culture]
  const value = typeof date === 'string' ? new Date(date) : date
  return value.toLocaleString(locale, { timeZone })
}

export function formatDateWithZone(date: Date | string, culture: SupportedCulture, options: Intl.DateTimeFormatOptions = {}, timeZone: string = APP_TIME_ZONE): string {
  const locale = LOCALE_BY_CULTURE[culture]
  const value = typeof date === 'string' ? new Date(date) : date
  return value.toLocaleDateString(locale, { timeZone, ...options })
}



