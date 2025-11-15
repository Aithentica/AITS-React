import { describe, expect, it } from 'vitest'
import { ensureFullHour, formatDateTimeWithZone, formatForDateTimeLocalInput, formatTimeWithZone } from './dateTimeUtils'

describe('ensureFullHour', () => {
  it('ustawia minuty na pełną godzinę', () => {
    expect(ensureFullHour('2025-11-07T13:45')).toBe('2025-11-07T13:00')
  })

  it('pozostawia wartości z pełną godziną bez zmian', () => {
    expect(ensureFullHour('2025-11-07T07:00')).toBe('2025-11-07T07:00')
  })
})

describe('formatForDateTimeLocalInput', () => {
  it('zwraca pusty ciąg dla niepoprawnej wartości', () => {
    expect(formatForDateTimeLocalInput('')).toBe('')
  })

  it('formatuje lokalny obiekt Date do wartości kontrolki', () => {
    const date = new Date(2025, 10, 7, 9, 15)
    expect(formatForDateTimeLocalInput(date)).toBe('2025-11-07T09:15')
  })
})

describe('formatowanie z uwzględnieniem strefy czasowej', () => {
  it('formatTimeWithZone stosuje wskazaną strefę czasową', () => {
    const date = new Date('2025-11-07T15:00:00Z')
    const utc = formatTimeWithZone(date, 'pl', 'UTC')
    const newYork = formatTimeWithZone(date, 'pl', 'America/New_York')
    expect(utc).not.toEqual(newYork)
  })

  it('formatDateTimeWithZone różnicuje wynik względem strefy czasowej', () => {
    const date = new Date('2025-11-07T18:30:00Z')
    const utc = formatDateTimeWithZone(date, 'en', 'UTC')
    const tokyo = formatDateTimeWithZone(date, 'en', 'Asia/Tokyo')
    expect(utc).not.toEqual(tokyo)
  })
})



