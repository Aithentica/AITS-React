import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { getTranscriptionHubUrl } from '../../config/signalr'
import type { Translations } from '../../i18n'

export interface SessionTranscriptionSegmentDto {
  id: number
  speakerTag: string
  startOffset: string
  endOffset: string
  content: string
}

export interface SessionTranscriptionDto {
  id: number
  source: number
  transcriptText: string
  createdAt: string
  sourceFileName?: string
  sourceContentType?: string
  filePath?: string
  createdByUserId?: string
  segments?: SessionTranscriptionSegmentDto[]
}

interface SessionTranscriptionsProps {
  sessionId: number
  token: string
  items: SessionTranscriptionDto[]
  onRefresh: () => Promise<void>
  t: Translations
}

type SubmitSource = 'AudioFile' | 'VideoFile' | 'FinalTranscriptUpload'
type RealtimeStatus = 'disconnected' | 'connecting' | 'recording' | 'stopping' | 'error'

const SOURCE_MAP: Record<number, string> = {
  11: 'ManualText',
  12: 'TextFile',
  13: 'AudioRecording',
  14: 'AudioUpload',
  15: 'RealtimeRecording',
  16: 'AudioFile',
  17: 'VideoFile',
  18: 'FinalTranscriptUpload'
}

function sourceLabel(source: number, t: Translations): string {
  const key = SOURCE_MAP[source]
  switch (key) {
    case 'ManualText':
      return t['sessions.transcriptions.source.manual'] ?? 'Tekst ręczny'
    case 'TextFile':
    case 'FinalTranscriptUpload':
      return t['sessions.transcriptions.source.textFile'] ?? 'Plik transkryptu'
    case 'AudioRecording':
      return t['sessions.transcriptions.source.audioRecording'] ?? 'Nagranie mikrofonem'
    case 'AudioUpload':
    case 'AudioFile':
      return t['sessions.transcriptions.source.audioUpload'] ?? 'Plik audio'
    case 'VideoFile':
      return t['sessions.transcriptions.source.video'] ?? 'Plik wideo'
    case 'RealtimeRecording':
      return t['sessions.transcriptions.source.realtime'] ?? 'Nagrywanie na żywo'
    default:
      return t['sessions.transcriptions.source.unknown'] ?? 'Nieznane źródło'
  }
}

function formatDuration(value?: string | null): string {
  if (!value) return '00:00'
  if (value.includes(':')) return value
  const match = /PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/.exec(value)
  if (!match) return value
  const hours = parseInt(match[1] ?? '0', 10)
  const minutes = parseInt(match[2] ?? '0', 10)
  const seconds = parseInt(match[3] ?? '0', 10)
  const totalMinutes = hours * 60 + minutes
  return `${totalMinutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
}

function downsampleTo16k(input: Float32Array, inputRate: number): Int16Array {
  if (inputRate === 16000) {
    const buffer = new Int16Array(input.length)
    for (let i = 0; i < input.length; i++) {
      const s = Math.max(-1, Math.min(1, input[i]))
      buffer[i] = s < 0 ? s * 0x8000 : s * 0x7fff
    }
    return buffer
  }

  const ratio = inputRate / 16000
  const outputLength = Math.floor(input.length / ratio)
  const output = new Int16Array(outputLength)
  let offsetResult = 0
  let offsetInput = 0

  while (offsetResult < outputLength) {
    const nextOffsetInput = Math.round((offsetResult + 1) * ratio)
    let accum = 0
    let count = 0
    for (let i = offsetInput; i < nextOffsetInput && i < input.length; i++) {
      accum += input[i]
      count++
    }
    const sample = count > 0 ? accum / count : 0
    output[offsetResult] = sample < 0 ? sample * 0x8000 : sample * 0x7fff
    offsetResult++
    offsetInput = nextOffsetInput
  }

  return output
}

function pcm16ToBase64(samples: Int16Array): string {
  const bytes = new Uint8Array(samples.buffer, samples.byteOffset, samples.byteLength)
  let binary = ''
  const chunkSize = 0x8000
  for (let i = 0; i < bytes.length; i += chunkSize) {
    const chunk = bytes.subarray(i, i + chunkSize)
    binary += String.fromCharCode(...chunk)
  }
  return btoa(binary)
}

export function SessionTranscriptions({ sessionId, token, items, onRefresh, t }: SessionTranscriptionsProps) {
  const audioUploadRef = useRef<HTMLInputElement | null>(null)
  const videoUploadRef = useRef<HTMLInputElement | null>(null)
  const transcriptUploadRef = useRef<HTMLInputElement | null>(null)

  const connectionRef = useRef<HubConnection | null>(null)
  const audioContextRef = useRef<AudioContext | null>(null)
  const processorRef = useRef<ScriptProcessorNode | null>(null)
  const sourceNodeRef = useRef<MediaStreamAudioSourceNode | null>(null)
  const gainNodeRef = useRef<GainNode | null>(null)
  const streamRef = useRef<MediaStream | null>(null)

  const [fileLoading, setFileLoading] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isPreviewOpen, setIsPreviewOpen] = useState(false)

  const [realtimeStatus, setRealtimeStatus] = useState<RealtimeStatus>('disconnected')
  const [realtimeTranscript, setRealtimeTranscript] = useState('')
  const [realtimeSegments, setRealtimeSegments] = useState<SessionTranscriptionSegmentDto[]>([])

  const resetFeedback = useCallback(() => {
    setMessage(null)
    setError(null)
  }, [])

  const cleanupRealtime = useCallback(async (options: { keepConnection?: boolean } = {}) => {
    processorRef.current?.disconnect()
    processorRef.current = null
    gainNodeRef.current?.disconnect()
    gainNodeRef.current = null
    sourceNodeRef.current?.disconnect()
    sourceNodeRef.current = null

    if (audioContextRef.current) {
      try {
        await audioContextRef.current.close()
      } catch {
        // ignorujemy
      }
      audioContextRef.current = null
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop())
      streamRef.current = null
    }

    if (!options.keepConnection && connectionRef.current) {
      try {
        await connectionRef.current.stop()
      } catch {
        // ignorujemy
      }
      connectionRef.current = null
    }
  }, [])

  const submitFile = useCallback(async (source: SubmitSource, file: File) => {
    resetFeedback()
    setFileLoading(true)
    try {
      const formData = new FormData()
      formData.append('sourceType', source)
      formData.append('file', file)

      const response = await fetch(`/api/sessions/${sessionId}/transcriptions`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`
        },
        body: formData
      })

      if (!response.ok) {
        const payload = await response.json().catch(() => ({})) as { error?: string }
        throw new Error(payload.error ?? `${response.status} ${response.statusText}`)
      }

      await onRefresh()
      setMessage(t['sessions.transcriptions.success'] ?? 'Transkrypcja została zapisana (poprzednia wersja została zastąpiona).')
    } catch (err) {
      setError(err instanceof Error ? err.message : (t['sessions.transcriptions.error'] ?? 'Nie udało się przetworzyć pliku.'))
    } finally {
      setFileLoading(false)
    }
  }, [onRefresh, resetFeedback, sessionId, t, token])

  const handleAudioUpload = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return
    submitFile('AudioFile', file).finally(() => {
      event.target.value = ''
    })
  }, [submitFile])

  const handleVideoUpload = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return
    submitFile('VideoFile', file).finally(() => {
      event.target.value = ''
    })
  }, [submitFile])

  const handleTranscriptUpload = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return
    submitFile('FinalTranscriptUpload', file).finally(() => {
      event.target.value = ''
    })
  }, [submitFile])

  const startRealtime = useCallback(async () => {
    if (realtimeStatus === 'recording' || realtimeStatus === 'connecting') return
    resetFeedback()

    try {
      setRealtimeStatus('connecting')

      const hubUrl = getTranscriptionHubUrl()

      const connection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.None)
        .build()

      connection.on('RealtimeStatus', (payload?: { status?: string; message?: string }) => {
        const status = payload?.status
        if (status === 'error') {
          setRealtimeStatus('error')
          setError(payload?.message ?? (t['sessions.transcriptions.realtime.error'] ?? 'Wystąpił błąd podczas nagrywania.'))
        } else if (status === 'stopped') {
          setRealtimeStatus('disconnected')
        }
      })

      connection.on('RealtimeUpdate', (payload?: { transcript?: string; segments?: unknown[] }) => {
        setRealtimeTranscript(payload?.transcript ?? '')
        if (Array.isArray(payload?.segments)) {
          const mapped = payload.segments.map((segment, index) => {
            const seg = segment as Record<string, unknown>
            return {
              id: typeof seg.id === 'number' ? seg.id : index,
              speakerTag: (seg.speakerTag ?? seg.SpeakerTag ?? `Mówca ${index + 1}`) as string,
              startOffset: formatDuration((seg.startOffset ?? seg.StartOffset) as string | undefined),
              endOffset: formatDuration((seg.endOffset ?? seg.EndOffset) as string | undefined),
              content: (seg.content ?? seg.Content ?? seg.text ?? seg.Text ?? '') as string
            }
          })
          setRealtimeSegments(mapped)
        }
      })

      connection.on('RealtimePersisted', async () => {
        await onRefresh()
        setMessage(t['sessions.transcriptions.success'] ?? 'Transkrypcja została zapisana (poprzednia wersja została zastąpiona).')
        setRealtimeTranscript('')
        setRealtimeSegments([])
        try {
          await connection.stop()
        } catch {
          // ignorujemy
        }
        connectionRef.current = null
        setRealtimeStatus('disconnected')
      })

      connection.onclose(() => {
        connectionRef.current = null
        setRealtimeStatus('disconnected')
      })

      await connection.start()
      await connection.invoke('StartRealtime', sessionId)
      connectionRef.current = connection

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      streamRef.current = stream

      const audioContext = new AudioContext()
      audioContextRef.current = audioContext

      const source = audioContext.createMediaStreamSource(stream)
      sourceNodeRef.current = source

      const gainNode = audioContext.createGain()
      gainNode.gain.value = 0
      gainNodeRef.current = gainNode

      const processor = audioContext.createScriptProcessor(4096, 1, 1)
      processorRef.current = processor

      processor.onaudioprocess = event => {
        const connectionInstance = connectionRef.current
        if (!connectionInstance) return
        const inputBuffer = event.inputBuffer.getChannelData(0)
        const pcm16 = downsampleTo16k(inputBuffer, audioContext.sampleRate)
        if (pcm16.length === 0) return
        const payload = pcm16ToBase64(pcm16)
        connectionInstance.invoke('UploadChunk', sessionId, payload).catch(() => {
          // tłumimy jednorazowe błędy przesyłu
        })
      }

      source.connect(processor)
      processor.connect(gainNode)
      gainNode.connect(audioContext.destination)

      await audioContext.resume()
      setRealtimeTranscript('')
      setRealtimeSegments([])
      setRealtimeStatus('recording')
    } catch (err) {
      await cleanupRealtime()
      setRealtimeStatus('error')
      setError(err instanceof Error ? err.message : (t['sessions.transcriptions.realtime.error'] ?? 'Nie udało się uruchomić nagrywania.'))
    }
  }, [cleanupRealtime, onRefresh, realtimeStatus, resetFeedback, sessionId, t, token])

  const stopRealtime = useCallback(async () => {
    if (!connectionRef.current || (realtimeStatus !== 'recording' && realtimeStatus !== 'connecting')) {
      return
    }

    try {
      setRealtimeStatus('stopping')
      await cleanupRealtime({ keepConnection: true })
      await connectionRef.current.invoke('StopRealtime', sessionId)
    } catch (err) {
      setError(err instanceof Error ? err.message : (t['sessions.transcriptions.realtime.stopError'] ?? 'Nie udało się zatrzymać nagrywania.'))
      await cleanupRealtime()
      setRealtimeStatus('error')
    }
  }, [cleanupRealtime, realtimeStatus, sessionId, t])

  useEffect(() => {
    return () => {
      cleanupRealtime().catch(() => undefined)
    }
  }, [cleanupRealtime])

  const realtimeButtonLabel = useMemo(() => {
    switch (realtimeStatus) {
      case 'connecting':
        return t['sessions.transcriptions.realtime.connecting'] ?? 'Łączenie z usługą Azure...'
      case 'recording':
        return t['sessions.transcriptions.realtime.stop'] ?? 'Zatrzymaj nagrywanie na żywo'
      case 'stopping':
        return t['sessions.transcriptions.realtime.stopping'] ?? 'Zamykanie sesji...'
      case 'error':
        return t['sessions.transcriptions.realtime.retry'] ?? 'Spróbuj ponownie'
      default:
        return t['sessions.transcriptions.realtime.start'] ?? 'Rozpocznij nagrywanie na żywo'
    }
  }, [realtimeStatus, t])

  const latestTranscription = items[0] ?? null

  return (
    <section className="bg-white p-8 rounded-lg shadow-lg space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">{t['sessions.transcriptions.title'] ?? 'Transkrypcje sesji'}</h2>
          <p className="text-sm text-gray-600 mt-1">
            {t['sessions.transcriptions.subtitle'] ?? 'Każda nowa transkrypcja zastępuje poprzednią. Wybierz jedną z metod pozyskania tekstu rozmowy.'}
          </p>
        </div>
        <button
          onClick={onRefresh}
          className="px-4 py-2 text-sm font-semibold text-blue-600 hover:text-blue-800"
        >
          {t['sessions.transcriptions.refresh'] ?? 'Odśwież listę'}
        </button>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{error}</div>
      )}
      {message && (
        <div className="rounded-md border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800">{message}</div>
      )}

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="rounded-lg border border-gray-200 p-5 space-y-4 bg-gray-50">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold text-gray-900">{t['sessions.transcriptions.realtime.title'] ?? 'Nagrywanie z diarizacją w czasie rzeczywistym'}</h3>
            <span className="text-xs font-medium uppercase tracking-wide text-gray-500">
              {realtimeStatus === 'recording' ? (t['sessions.transcriptions.realtime.statusRecording'] ?? 'Nagrywanie trwa') : t[`sessions.transcriptions.realtime.status.${realtimeStatus}`] ?? ''}
            </span>
          </div>
          <p className="text-sm text-gray-600">
            {t['sessions.transcriptions.realtime.description'] ?? 'Połącz się z Azure Speech i uzyskaj transkrypcję z diarizacją dla maksymalnie 3 osób. Wyniki aktualizują się na bieżąco.'}
          </p>
          <button
            type="button"
            onClick={realtimeStatus === 'recording' || realtimeStatus === 'connecting' ? stopRealtime : startRealtime}
            disabled={realtimeStatus === 'stopping'}
            className={`w-full px-4 py-2 rounded-lg font-semibold text-white transition-colors ${
              realtimeStatus === 'recording' ? 'bg-red-600 hover:bg-red-700' : 'bg-green-600 hover:bg-green-700'
            } ${realtimeStatus === 'stopping' ? 'opacity-50 cursor-not-allowed' : ''}`}
          >
            {realtimeButtonLabel}
          </button>
          {(realtimeTranscript || realtimeSegments.length > 0) && (
            <div className="rounded-md border border-gray-200 bg-white p-4 max-h-64 overflow-y-auto space-y-3">
              {realtimeSegments.map(segment => (
                <div key={`${segment.speakerTag}-${segment.startOffset}-${segment.endOffset}`} className="text-sm">
                  <div className="flex items-center gap-2 text-gray-600">
                    <span className="font-semibold text-gray-800">{segment.speakerTag}</span>
                    <span>{segment.startOffset}</span>
                    <span>→</span>
                    <span>{segment.endOffset}</span>
                  </div>
                  <p className="text-gray-900 whitespace-pre-wrap">{segment.content}</p>
                </div>
              ))}
              {!realtimeSegments.length && realtimeTranscript && (
                <p className="text-sm text-gray-800 whitespace-pre-wrap">{realtimeTranscript}</p>
              )}
            </div>
          )}
        </div>

        <div className="space-y-4">
          <div className="rounded-lg border border-gray-200 bg-white p-5 space-y-3">
            <h3 className="text-lg font-semibold text-gray-900">{t['sessions.transcriptions.audioUploadTitle'] ?? 'Transkrypcja z pliku audio (WAV/MP3)'}</h3>
            <p className="text-sm text-gray-600">{t['sessions.transcriptions.audioUploadHint'] ?? 'Plik zostanie przesłany do Azure Speech i przetworzony z diarizacją.'}</p>
            <input
              ref={audioUploadRef}
              type="file"
              accept=".wav,.mp3,audio/wav,audio/mpeg"
              onChange={handleAudioUpload}
              disabled={fileLoading || realtimeStatus === 'recording'}
              data-testid="audio-upload-input"
              className="hidden"
            />
            <button
              type="button"
              onClick={() => audioUploadRef.current?.click()}
              disabled={fileLoading || realtimeStatus === 'recording'}
              className="w-full rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {t['sessions.transcriptions.audioUploadButton'] ?? 'Wybierz plik audio'}
            </button>
          </div>

          <div className="rounded-lg border border-gray-200 bg-white p-5 space-y-3">
            <h3 className="text-lg font-semibold text-gray-900">{t['sessions.transcriptions.videoUploadTitle'] ?? 'Transkrypcja z pliku wideo (MP4/MOV/MKV/AVI)'}</h3>
            <p className="text-sm text-gray-600">{t['sessions.transcriptions.videoUploadHint'] ?? 'Ścieżka audio zostanie wyodrębniona lokalnie i przetworzona w Azure Speech.'}</p>
            <input
              ref={videoUploadRef}
              type="file"
              accept=".mp4,.mov,.mkv,.avi,video/mp4,video/quicktime,video/x-matroska,video/x-msvideo"
              onChange={handleVideoUpload}
              disabled={fileLoading || realtimeStatus === 'recording'}
              data-testid="video-upload-input"
              className="hidden"
            />
            <button
              type="button"
              onClick={() => videoUploadRef.current?.click()}
              disabled={fileLoading || realtimeStatus === 'recording'}
              className="w-full rounded-lg bg-purple-600 px-4 py-2 font-semibold text-white hover:bg-purple-700 disabled:opacity-50"
            >
              {t['sessions.transcriptions.videoUploadButton'] ?? 'Wybierz plik wideo'}
            </button>
          </div>

          <div className="rounded-lg border border-gray-200 bg-white p-5 space-y-3">
            <h3 className="text-lg font-semibold text-gray-900">{t['sessions.transcriptions.transcriptUploadTitle'] ?? 'Wgraj gotową transkrypcję (TXT/VTT/SRT)'}</h3>
            <p className="text-sm text-gray-600">{t['sessions.transcriptions.transcriptUploadHint'] ?? 'Plik zostanie zapisany jako finalny transkrypt bez ponownego przetwarzania.'}</p>
            <input
              ref={transcriptUploadRef}
              type="file"
              accept=".txt,.vtt,.srt,text/plain,text/vtt,application/x-subrip"
              onChange={handleTranscriptUpload}
              disabled={fileLoading}
              data-testid="transcript-upload-input"
              className="hidden"
            />
            <button
              type="button"
              onClick={() => transcriptUploadRef.current?.click()}
              disabled={fileLoading}
              className="w-full rounded-lg bg-gray-700 px-4 py-2 font-semibold text-white hover:bg-gray-800 disabled:opacity-50"
            >
              {t['sessions.transcriptions.transcriptUploadButton'] ?? 'Wybierz plik transkryptu'}
            </button>
          </div>
        </div>
      </div>

      <div className="pt-4 border-t border-gray-200">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-4">
          <h3 className="text-xl font-semibold text-gray-900">{t['sessions.transcriptions.currentTitle'] ?? 'Aktualna transkrypcja'}</h3>
          {latestTranscription && (
            <button
              type="button"
              onClick={() => setIsPreviewOpen(true)}
              className="self-start md:self-auto inline-flex items-center gap-2 rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-900 transition-colors"
            >
              {t['sessions.transcriptions.preview'] ?? 'Pokaż podgląd'}
            </button>
          )}
        </div>
        {!latestTranscription && (
          <p className="text-sm text-gray-600">{t['sessions.transcriptions.empty'] ?? 'Brak zapisanej transkrypcji dla tej sesji.'}</p>
        )}
      </div>

      {isPreviewOpen && latestTranscription && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4" role="dialog" aria-modal="true">
          <div className="w-full max-w-4xl rounded-lg bg-white shadow-2xl">
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <div>
                <h4 className="text-lg font-semibold text-gray-900">{t['sessions.transcriptions.previewTitle'] ?? 'Podgląd transkrypcji'}</h4>
                <p className="text-xs text-gray-500">{new Date(latestTranscription.createdAt).toLocaleString()}</p>
              </div>
              <button
                type="button"
                className="rounded-full p-2 text-gray-500 hover:text-gray-700"
                aria-label={t['common.close'] ?? 'Zamknij'}
                onClick={() => setIsPreviewOpen(false)}
              >
                ×
              </button>
            </div>
            <div className="space-y-4 px-6 py-5">
              <div className="flex flex-col gap-1">
                <span className="text-sm font-semibold text-gray-800">{sourceLabel(latestTranscription.source, t)}</span>
                {latestTranscription.filePath && (
                  <a
                    href={`${window.location.origin}/${latestTranscription.filePath}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm text-blue-600 hover:text-blue-800"
                  >
                    {t['sessions.transcriptions.download'] ?? 'Pobierz źródło'}
                  </a>
                )}
              </div>
              <div className="max-h-[50vh] overflow-y-auto rounded-md border border-gray-200 bg-gray-50 p-4 space-y-3">
                {latestTranscription.segments && latestTranscription.segments.length > 0 ? (
                  latestTranscription.segments.map(segment => (
                    <div key={segment.id} className="text-sm">
                      <div className="flex items-center gap-2 text-gray-600">
                        <span className="font-semibold text-gray-800">{segment.speakerTag}</span>
                        <span>{formatDuration(segment.startOffset)}</span>
                        <span>→</span>
                        <span>{formatDuration(segment.endOffset)}</span>
                      </div>
                      <p className="text-gray-900 whitespace-pre-wrap">{segment.content}</p>
                    </div>
                  ))
                ) : (
                  <pre className="whitespace-pre-wrap text-sm text-gray-800">{latestTranscription.transcriptText}</pre>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default SessionTranscriptions

