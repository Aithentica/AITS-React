import type { Translations } from '../../i18n'

type SupportedCulture = 'pl' | 'en'

export interface WeekCalendarLabels {
  title: string
  previousWeek: string
  today: string
  nextWeek: string
}

const fallbackLabels: Record<SupportedCulture, WeekCalendarLabels> = {
  pl: {
    title: 'Kalendarz tygodniowy',
    previousWeek: '← Poprzedni',
    today: 'Dzisiaj',
    nextWeek: 'Następny →'
  },
  en: {
    title: 'Weekly calendar',
    previousWeek: '← Previous',
    today: 'Today',
    nextWeek: 'Next →'
  }
}

export function getWeekCalendarLabels(culture: SupportedCulture, translations: Translations): WeekCalendarLabels {
  const fallback = fallbackLabels[culture] ?? fallbackLabels.pl

  return {
    title: translations['calendar.weeklyTitle'] ?? fallback.title,
    previousWeek: translations['calendar.previousWeek'] ?? fallback.previousWeek,
    today: translations['calendar.today'] ?? fallback.today,
    nextWeek: translations['calendar.nextWeek'] ?? fallback.nextWeek
  }
}


