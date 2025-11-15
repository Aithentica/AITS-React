import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createElement } from 'react'
import SessionTranscriptions, { type SessionTranscriptionDto } from './SessionTranscriptions'

const connectionHandlers: Record<string, Array<(payload?: unknown) => void>> = {}
let lastHubUrl: string | undefined
let lastAccessTokenFactory: (() => unknown) | undefined

class MockConnection {
  public on = vi.fn((event: string, handler: (payload?: unknown) => void) => {
    connectionHandlers[event] = connectionHandlers[event] ?? []
    connectionHandlers[event]!.push(handler)
  })

  public invoke = vi.fn().mockResolvedValue(undefined)
  public start = vi.fn().mockResolvedValue(undefined)
  public stop = vi.fn().mockResolvedValue(undefined)
  public onclose = vi.fn((handler: () => void) => {
    connectionHandlers['__close__'] = [handler]
  })
}

let lastConnection: MockConnection | null = null

vi.mock('@microsoft/signalr', () => {
  class Builder {
    private readonly connection = new MockConnection()

    public withUrl(url: string, options?: { accessTokenFactory?: () => unknown }) {
      lastHubUrl = url
      lastAccessTokenFactory = options?.accessTokenFactory
      return this
    }

    public withAutomaticReconnect() {
      return this
    }

    public configureLogging() {
      return this
    }

    public build() {
      lastConnection = this.connection
      return this.connection
    }
  }

  return {
    HubConnectionBuilder: Builder,
    LogLevel: { None: 0 }
  }
})

describe('SessionTranscriptions', () => {
  const token = 'test-token'
  const t: Record<string, string> = {}
  const originalEnv = { ...import.meta.env }

  beforeEach(() => {
    vi.restoreAllMocks()
    Object.keys(connectionHandlers).forEach(key => delete connectionHandlers[key])
    lastConnection = null
    lastHubUrl = undefined
    lastAccessTokenFactory = undefined
    Object.assign(import.meta.env as Record<string, unknown>, originalEnv)
    delete (import.meta.env as Record<string, unknown>).VITE_SIGNALR_BASE_URL
    delete (import.meta.env as Record<string, unknown>).VITE_API_BASE_URL

    const streamMock = {
      getTracks: vi.fn(() => [{ stop: vi.fn() }])
    }
    vi.stubGlobal('navigator', {
      mediaDevices: {
        getUserMedia: vi.fn().mockResolvedValue(streamMock)
      }
    })

    class StubSourceNode {
      connect = vi.fn()
      disconnect = vi.fn()
    }

    class StubGainNode {
      public gain = { value: 0 }
      public connect = vi.fn()
      public disconnect = vi.fn()
    }

    class StubProcessorNode {
      public connect = vi.fn()
      public disconnect = vi.fn()
      public onaudioprocess: ((event: { inputBuffer: { getChannelData: (index: number) => Float32Array } }) => void) | null = null
    }

    class StubAudioContext {
      public sampleRate = 16000
      public destination = {}
      public createMediaStreamSource = vi.fn(() => new StubSourceNode())
      public createGain = vi.fn(() => new StubGainNode())
      public createScriptProcessor = vi.fn(() => new StubProcessorNode())
      public resume = vi.fn().mockResolvedValue(undefined)
      public close = vi.fn().mockResolvedValue(undefined)
    }

    vi.stubGlobal('AudioContext', StubAudioContext as unknown as typeof AudioContext)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
    Object.assign(import.meta.env as Record<string, unknown>, originalEnv)
  })

  it('wysyła plik audio do transkrypcji', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    const refreshMock = vi.fn().mockResolvedValue(undefined)

    render(
      createElement(SessionTranscriptions, {
        sessionId: 1,
        token,
        items: [],
        onRefresh: refreshMock,
        t
      })
    )

    const input = screen.getByTestId('audio-upload-input') as HTMLInputElement
    const file = new File(['AUDIO'], 'nagranie.wav', { type: 'audio/wav' })
    fireEvent.change(input, { target: { files: [file] } })

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1)
      expect(refreshMock).toHaveBeenCalledTimes(1)
    })

    const [, init] = fetchMock.mock.calls[0]
    const body = (init as RequestInit).body as FormData
    expect(body.get('sourceType')).toBe('AudioFile')
    expect(body.get('file')).toBe(file)
  })

  it('wysyła plik wideo do transkrypcji', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)
    const refreshMock = vi.fn().mockResolvedValue(undefined)

    render(
      createElement(SessionTranscriptions, {
        sessionId: 2,
        token,
        items: [],
        onRefresh: refreshMock,
        t
      })
    )

    const input = screen.getByTestId('video-upload-input') as HTMLInputElement
    const file = new File(['VIDEO'], 'nagranie.mp4', { type: 'video/mp4' })
    fireEvent.change(input, { target: { files: [file] } })

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1)
    })

    const [, init] = fetchMock.mock.calls[0]
    const body = (init as RequestInit).body as FormData
    expect(body.get('sourceType')).toBe('VideoFile')
    expect(body.get('file')).toBe(file)
  })

  it('wysyła gotowy transkrypt', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    render(
      createElement(SessionTranscriptions, {
        sessionId: 3,
        token,
        items: [],
        onRefresh: vi.fn().mockResolvedValue(undefined),
        t
      })
    )

    const input = screen.getByTestId('transcript-upload-input') as HTMLInputElement
    const file = new File(['SPEAKER A'], 'transcript.vtt', { type: 'text/vtt' })
    fireEvent.change(input, { target: { files: [file] } })

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1)
    })

    const [, init] = fetchMock.mock.calls[0]
    const body = (init as RequestInit).body as FormData
    expect(body.get('sourceType')).toBe('FinalTranscriptUpload')
    expect(body.get('file')).toBe(file)
  })

  it('aktualizuje podgląd po otrzymaniu danych z kanału SignalR', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    const refreshMock = vi.fn().mockResolvedValue(undefined)

    render(
      createElement(SessionTranscriptions, {
        sessionId: 4,
        token,
        items: [],
        onRefresh: refreshMock,
        t
      })
    )

    const realtimeButton = screen.getByRole('button', { name: /Rozpocznij|nagrywanie/i })
    await act(async () => {
      fireEvent.click(realtimeButton)
    })

    expect(lastConnection).not.toBeNull()
    expect(lastHubUrl).toBe('/hubs/transcriptions')
    expect(lastAccessTokenFactory?.()).toBe(token)
    expect(lastConnection?.start).toHaveBeenCalled()
    expect(lastConnection?.invoke).toHaveBeenCalledWith('StartRealtime', 4)

    // symulujemy aktualizację z Azure
    const updateHandlers = connectionHandlers['RealtimeUpdate']
    expect(updateHandlers).toBeDefined()
    act(() => {
      updateHandlers?.forEach(handler => handler({
        transcript: 'Cześć',
        segments: [
          { speakerTag: 'Speaker1', startOffset: '00:00', endOffset: '00:05', content: 'Dzień dobry' }
        ]
      }))
    })

    expect(screen.getByText('Speaker1')).toBeInTheDocument()
    expect(screen.getByText('Dzień dobry')).toBeInTheDocument()

    // symulujemy zapis transkrypcji
    const persistedHandlers = connectionHandlers['RealtimePersisted']
    await act(async () => {
      persistedHandlers?.forEach(handler => handler())
    })

    expect(refreshMock).toHaveBeenCalled()
  })

  it('prezentuje podgląd zapisanej transkrypcji', async () => {
    const transcription: SessionTranscriptionDto = {
      id: 1,
      source: 16,
      transcriptText: 'Hej',
      createdAt: new Date('2025-01-01T10:00:00Z').toISOString(),
      segments: [
        { id: 1, speakerTag: 'Osoba A', startOffset: '00:00', endOffset: '00:10', content: 'Dzień dobry' }
      ]
    }

    render(
      createElement(SessionTranscriptions, {
        sessionId: 5,
        token,
        items: [transcription],
        onRefresh: vi.fn().mockResolvedValue(undefined),
        t
      })
    )

    const button = screen.getByRole('button', { name: /Pokaż/i })
    fireEvent.click(button)

    const dialog = await screen.findByRole('dialog')
    expect(dialog).toBeInTheDocument()
    expect(within(dialog).getByText('Osoba A')).toBeInTheDocument()
    expect(within(dialog).getByText('00:00')).toBeInTheDocument()

    const close = screen.getByRole('button', { name: /Zamknij/i })
    fireEvent.click(close)

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('odczytuje adres SignalR z konfiguracji środowiska', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    const refreshMock = vi.fn().mockResolvedValue(undefined)

    Object.assign(import.meta.env as Record<string, unknown>, {
      VITE_SIGNALR_BASE_URL: 'https://api.example.com'
    })

    render(
      createElement(SessionTranscriptions, {
        sessionId: 10,
        token,
        items: [],
        onRefresh: refreshMock,
        t
      })
    )

    const realtimeButton = screen.getByRole('button', { name: /Rozpocznij|nagrywanie/i })
    await act(async () => {
      fireEvent.click(realtimeButton)
    })

    expect(lastHubUrl).toBe('https://api.example.com/hubs/transcriptions')
  })

  it('usuwa sufiks /api z bazowego adresu API', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    const refreshMock = vi.fn().mockResolvedValue(undefined)

    Object.assign(import.meta.env as Record<string, unknown>, {
      VITE_API_BASE_URL: 'https://api.example.com/api'
    })

    render(
      createElement(SessionTranscriptions, {
        sessionId: 11,
        token,
        items: [],
        onRefresh: refreshMock,
        t
      })
    )

    const realtimeButton = screen.getByRole('button', { name: /Rozpocznij|nagrywanie/i })
    await act(async () => {
      fireEvent.click(realtimeButton)
    })

    expect(lastHubUrl).toBe('https://api.example.com/hubs/transcriptions')
  })

  it('zwraca ścieżkę względną gdy baza API to /api', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    const refreshMock = vi.fn().mockResolvedValue(undefined)

    Object.assign(import.meta.env as Record<string, unknown>, {
      VITE_API_BASE_URL: '/api'
    })

    render(
      createElement(SessionTranscriptions, {
        sessionId: 12,
        token,
        items: [],
        onRefresh: refreshMock,
        t
      })
    )

    const realtimeButton = screen.getByRole('button', { name: /Rozpocznij|nagrywanie/i })
    await act(async () => {
      fireEvent.click(realtimeButton)
    })

    expect(lastHubUrl).toBe('/hubs/transcriptions')
  })
})

