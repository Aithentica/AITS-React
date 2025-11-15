import { useEffect, useState } from 'react'
import { Line } from 'react-chartjs-2'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
)

interface MetricsData {
  SessionId: number
  SessionDate: string
  WeekNumber: number
  Lek: number
  Depresja: number
  Samopoczucie: number
  SkalaBecka: number
  Problem1: number
  Problem2: number
  Problem3: number
  Problem4: number
}

interface PatientMetricsChartProps {
  token: string
}

export default function PatientMetricsChart({ token }: PatientMetricsChartProps) {
  const [metrics, setMetrics] = useState<MetricsData[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadMetrics()
  }, [token])

  async function loadMetrics() {
    try {
      setLoading(true)
      setError(null)
      const res = await fetch('/api/patient/sessions/metrics', {
        headers: { Authorization: `Bearer ${token}` }
      })

      if (!res.ok) {
        const errorText = await res.text().catch(() => '')
        console.error('Error loading metrics:', res.status, errorText)
        throw new Error(`Nie udało się załadować metryk: ${res.status}`)
      }

      const data = await res.json()
      
      // Upewnij się, że dane są tablicą
      if (!Array.isArray(data)) {
        setMetrics([])
        return
      }
      
      // Przyjmij wszystkie sesje - nawet z zerowymi wartościami
      setMetrics(data)
    } catch (err) {
      console.error('Error loading metrics:', err)
      setError('Błąd ładowania metryk')
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Metryki sesji</h3>
        <div className="text-center text-gray-500 py-8">Ładowanie...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Metryki sesji</h3>
        <div className="text-center text-red-600 py-8">{error}</div>
      </div>
    )
  }

  if (metrics.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Metryki sesji</h3>
        <div className="text-center text-gray-500 py-8">
          Brak danych do wyświetlenia. Metryki pojawią się po zakończeniu sesji.
        </div>
      </div>
    )
  }

  // Filtruj i sortuj metryki - przyjmij wszystkie sesje z poprawnymi datami
  const validMetrics = metrics
    .filter(m => {
      // Sprawdź czy SessionDate jest poprawna - to jest najważniejsze
      if (!m.SessionDate) {
        return false
      }
      // Sprawdź czy data jest poprawna
      const date = new Date(m.SessionDate)
      if (isNaN(date.getTime())) {
        return false
      }
      return true
    })
    .sort((a, b) => {
      // Sortuj po dacie sesji (chronologicznie)
      const dateA = new Date(a.SessionDate).getTime()
      const dateB = new Date(b.SessionDate).getTime()
      if (isNaN(dateA) || isNaN(dateB)) {
        return 0
      }
      return dateA - dateB
    })

  if (validMetrics.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Metryki sesji</h3>
        <div className="text-center text-gray-500 py-8">
          Brak sesji do wyświetlenia. Metryki pojawią się po utworzeniu sesji.
        </div>
      </div>
    )
  }

  // Formatuj daty dla lepszej czytelności
  const formatDate = (dateStr: string | Date) => {
    try {
      const date = dateStr instanceof Date ? dateStr : new Date(dateStr)
      if (isNaN(date.getTime())) {
        console.warn('Invalid date in formatDate:', dateStr)
        return String(dateStr)
      }
      return date.toLocaleDateString('pl-PL', { day: '2-digit', month: 'short', year: 'numeric' })
    } catch (e) {
      console.warn('Error formatting date:', dateStr, e)
      return String(dateStr)
    }
  }

  // Twórz etykiety - użyj daty sesji jako głównej etykiety
  const labels = validMetrics.map(m => {
    const dateStr = m.SessionDate
    return formatDate(dateStr)
  })

  // Konwertuj wartości na liczby i upewnij się, że są poprawne
  const chartData = {
    labels,
    datasets: [
      {
        label: 'Lęk',
        data: validMetrics.map(m => {
          const value = Number(m.Lek) || 0
          return isNaN(value) ? 0 : Math.max(0, Math.min(10, value))
        }),
        borderColor: 'rgb(239, 68, 68)',
        backgroundColor: 'rgba(239, 68, 68, 0.1)',
        tension: 0.4,
        fill: true,
        pointRadius: 4,
        pointHoverRadius: 6,
        pointBackgroundColor: 'rgb(239, 68, 68)',
        pointBorderColor: '#fff',
        pointBorderWidth: 2
      },
      {
        label: 'Depresja',
        data: validMetrics.map(m => {
          const value = Number(m.Depresja) || 0
          return isNaN(value) ? 0 : Math.max(0, Math.min(10, value))
        }),
        borderColor: 'rgb(59, 130, 246)',
        backgroundColor: 'rgba(59, 130, 246, 0.1)',
        tension: 0.4,
        fill: true,
        pointRadius: 4,
        pointHoverRadius: 6,
        pointBackgroundColor: 'rgb(59, 130, 246)',
        pointBorderColor: '#fff',
        pointBorderWidth: 2
      },
      {
        label: 'Samopoczucie',
        data: validMetrics.map(m => {
          const value = Number(m.Samopoczucie) || 0
          return isNaN(value) ? 0 : Math.max(0, Math.min(10, value))
        }),
        borderColor: 'rgb(34, 197, 94)',
        backgroundColor: 'rgba(34, 197, 94, 0.1)',
        tension: 0.4,
        fill: true,
        pointRadius: 4,
        pointHoverRadius: 6,
        pointBackgroundColor: 'rgb(34, 197, 94)',
        pointBorderColor: '#fff',
        pointBorderWidth: 2
      },
      {
        label: 'Skala Becka',
        data: validMetrics.map(m => {
          const value = Number(m.SkalaBecka) || 0
          return isNaN(value) ? 0 : Math.max(0, Math.min(10, value))
        }),
        borderColor: 'rgb(168, 85, 247)',
        backgroundColor: 'rgba(168, 85, 247, 0.1)',
        tension: 0.4,
        fill: true,
        pointRadius: 4,
        pointHoverRadius: 6,
        pointBackgroundColor: 'rgb(168, 85, 247)',
        pointBorderColor: '#fff',
        pointBorderWidth: 2
      }
    ]
  }

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index' as const,
      intersect: false
    },
    plugins: {
      legend: {
        position: 'top' as const,
        labels: {
          usePointStyle: true,
          padding: 15,
          font: {
            size: 12,
            weight: '500' as const
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: {
          size: 14,
          weight: 'bold' as const
        },
        bodyFont: {
          size: 13
        },
        borderColor: 'rgba(255, 255, 255, 0.1)',
        borderWidth: 1,
        displayColors: true,
        callbacks: {
          label: function(context: any) {
            let label = context.dataset.label || ''
            if (label) {
              label += ': '
            }
            if (context.parsed.y !== null) {
              label += context.parsed.y.toFixed(1)
            }
            return label
          }
        }
      },
      title: {
        display: false
      }
    },
    scales: {
      x: {
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          font: {
            size: 11
          },
          maxRotation: 45,
          minRotation: 0
        }
      },
      y: {
        beginAtZero: true,
        max: 10,
        min: 0,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          stepSize: 1,
          font: {
            size: 11
          },
          callback: function(value: any) {
            return value
          }
        }
      }
    },
    animation: {
      duration: 1000,
      easing: 'easeInOutQuart' as const
    }
  }

  return (
    <div className="bg-white rounded-lg shadow-lg p-6">
      <div className="mb-4">
        <h3 className="text-xl font-bold text-gray-800">Metryki sesji</h3>
        <p className="text-sm text-gray-600 mt-1">
          Wykres przedstawia zmiany w czasie dla różnych parametrów psychologicznych
        </p>
      </div>
      {labels.length > 0 ? (
        <>
          <div className="h-80">
            <Line data={chartData} options={options} />
          </div>
          <div className="mt-4 pt-4 border-t border-gray-200">
            <p className="text-xs text-gray-500">
              Dane z {validMetrics.length} {validMetrics.length === 1 ? 'sesji' : 'sesji'} • 
              Zakres: {formatDate(validMetrics[0].SessionDate)} - {formatDate(validMetrics[validMetrics.length - 1].SessionDate)}
            </p>
          </div>
        </>
      ) : (
        <div className="h-80 flex items-center justify-center">
          <div className="text-center text-gray-500">
            <p className="mb-2">Brak danych do wyświetlenia</p>
            <p className="text-xs text-gray-400">
              Metryki pojawią się po utworzeniu sesji
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

