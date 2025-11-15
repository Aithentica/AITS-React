import { describe, expect, it } from 'vitest'
import type { Translations } from '../../i18n'
import { getWeekCalendarLabels } from './weekCalendarLabels'

describe('getWeekCalendarLabels', () => {
  it('zwraca tłumaczenia jeśli są dostępne', () => {
    const translations: Translations = {
      'calendar.weeklyTitle': 'Custom title',
      'calendar.previousWeek': 'Custom previous',
      'calendar.today': 'Custom today',
      'calendar.nextWeek': 'Custom next'
    }

    const labels = getWeekCalendarLabels('pl', translations)

    expect(labels).toEqual({
      title: 'Custom title',
      previousWeek: 'Custom previous',
      today: 'Custom today',
      nextWeek: 'Custom next'
    })
  })

  it('zwraca angielskie wartości domyślne, gdy brak tłumaczeń', () => {
    const translations: Translations = {}

    const labels = getWeekCalendarLabels('en', translations)

    expect(labels).toEqual({
      title: 'Weekly calendar',
      previousWeek: '← Previous',
      today: 'Today',
      nextWeek: 'Next →'
    })
  })

  it('zwraca polskie wartości domyślne, gdy brak tłumaczeń', () => {
    const translations: Translations = {}

    const labels = getWeekCalendarLabels('pl', translations)

    expect(labels).toEqual({
      title: 'Kalendarz tygodniowy',
      previousWeek: '← Poprzedni',
      today: 'Dzisiaj',
      nextWeek: 'Następny →'
    })
  })
})


