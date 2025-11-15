const HUB_PATH = '/hubs/transcriptions'

function normalizeBaseUrl(rawBase?: string | null): string {
  if (!rawBase) {
    return ''
  }

  const trimmed = rawBase.trim()
  if (!trimmed) {
    return ''
  }

  return trimmed.replace(/\/+$/, '')
}

function stripApiSuffix(base: string): string {
  if (!base) {
    return ''
  }

  if (/\/api$/i.test(base)) {
    return base.slice(0, -4)
  }

  return base
}

export function getTranscriptionHubUrl(): string {
  const env = import.meta.env
  const normalizedBase = normalizeBaseUrl(env?.VITE_SIGNALR_BASE_URL ?? env?.VITE_API_BASE_URL ?? '')
  const sanitizedBase = stripApiSuffix(normalizedBase)

  if (!sanitizedBase) {
    return HUB_PATH
  }

  return `${sanitizedBase}${HUB_PATH}`
}


