import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'

const Roles = {
  Administrator: 'Administrator',
  Terapeuta: 'Terapeuta',
  Pacjent: 'Pacjent'
} as const

// Komponent ochrony routingu - sprawdza czy użytkownik ma odpowiednią rolę
export default function ProtectedRoute({ children, allowedRoles }: { children: React.ReactNode, allowedRoles: string[] }) {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const storedRoles = localStorage.getItem('roles')
    if (!storedRoles) {
      navigate('/login')
      return
    }
    try {
      const parsed = JSON.parse(storedRoles)
      const userRoles = Array.isArray(parsed) ? parsed : []
      
      // Sprawdź czy użytkownik ma jedną z dozwolonych ról
      const hasAccess = allowedRoles.some(role => userRoles.includes(role))
      
      if (!hasAccess) {
        // Pacjent próbuje dostać się do niedozwolonej strony - przekieruj do dashboardu
        if (userRoles.includes(Roles.Pacjent)) {
          navigate('/dashboard')
        } else {
          navigate('/login')
        }
      }
    } catch {
      navigate('/login')
    } finally {
      setLoading(false)
    }
  }, [allowedRoles, navigate])

  if (loading) {
    return <div className="min-h-screen flex items-center justify-center">Ładowanie...</div>
  }

  const storedRoles = localStorage.getItem('roles')
  if (!storedRoles) {
    return null
  }

  try {
    const parsed = JSON.parse(storedRoles)
    const userRoles = Array.isArray(parsed) ? parsed : []
    const hasAccess = allowedRoles.some(role => userRoles.includes(role))
    
    if (!hasAccess) {
      return null
    }
  } catch {
    return null
  }

  return <>{children}</>
}

