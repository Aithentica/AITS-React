import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

type ActivityTrackerProps = {
  endpoint?: string
}

const ActivityTracker = ({ endpoint = '/api/activity' }: ActivityTrackerProps) => {
  const location = useLocation()

  useEffect(() => {
    const initialToken = localStorage.getItem('token')
    if (!initialToken) {
      return
    }

    const path = location.pathname || '/'
    let startTime = new Date()

    const sendLog = (endTime: Date) => {
      const token = localStorage.getItem('token') || initialToken
      if (!token) return

      const durationMs = endTime.getTime() - startTime.getTime()
      if (durationMs < 500) {
        startTime = new Date()
        return
      }

      const payload = {
        path,
        startedAtUtc: startTime.toISOString(),
        endedAtUtc: endTime.toISOString()
      }

      fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload),
        keepalive: true
      }).catch(() => {
        /* ignorujemy błędy sieciowe tracker'a */
      })

      startTime = new Date()
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') {
        sendLog(new Date())
      }
    }

    const handleBeforeUnload = () => {
      sendLog(new Date())
    }

    window.addEventListener('beforeunload', handleBeforeUnload)
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      window.removeEventListener('beforeunload', handleBeforeUnload)
      sendLog(new Date())
    }
  }, [location.pathname, endpoint])

  return null
}

export default ActivityTracker
