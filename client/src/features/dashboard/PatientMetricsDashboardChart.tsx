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

interface MetricsDataPoint {
  sessionId: number
  sessionDate: string
  weekNumber?: number | null
  lek: number
  depresja: number
  samopoczucie: number
  skalaBecka: number
  problem1: number
  problem2: number
  problem3: number
  problem4: number
}

interface PatientMetricsDashboardChartProps {
  token: string
}

export default function PatientMetricsDashboardChart({ token }: PatientMetricsDashboardChartProps) {
  const [metrics, setMetrics] = useState<MetricsDataPoint[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (token) {
      loadMetrics()
    }
  }, [token])

  async function loadMetrics() {
    try {
      setLoading(true)
      setError(null)
      
      const res = await fetch('/api/patient/sessions/metrics', {
        headers: { 
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      })

      if (!res.ok) {
        const errorText = await res.text().catch(() => '')
        throw new Error(`Nie udało się załadować metryk: ${res.status} ${errorText}`)
      }

      const data = await res.json()
      
      if (!Array.isArray(data)) {
        setMetrics([])
        return
      }
      
      // Znormalizuj nazwy pól (API zwraca camelCase)
      const normalized: MetricsDataPoint[] = data
        .map((item: any) => {
          const sessionDate =
            item.sessionDate ??
            item.SessionDate ??
            item.startDateTime ??
            item.StartDateTime ??
            null
          
          if (!sessionDate) {
            return null
          }

          const toNumber = (value: any) => {
            const num = Number(value)
            return isNaN(num) ? 0 : num
          }

          return {
            sessionId: item.sessionId ?? item.SessionId ?? 0,
            sessionDate,
            weekNumber: item.weekNumber ?? item.WeekNumber ?? null,
            lek: toNumber(item.lek ?? item.Lek),
            depresja: toNumber(item.depresja ?? item.Depresja),
            samopoczucie: toNumber(item.samopoczucie ?? item.Samopoczucie),
            skalaBecka: toNumber(item.skalaBecka ?? item.SkalaBecka),
            problem1: toNumber(item.problem1 ?? item.Problem1),
            problem2: toNumber(item.problem2 ?? item.Problem2),
            problem3: toNumber(item.problem3 ?? item.Problem3),
            problem4: toNumber(item.problem4 ?? item.Problem4)
          }
        })
        .filter(Boolean) as MetricsDataPoint[]
      
      setMetrics(normalized)
    } catch (err) {
      console.error('Error loading metrics:', err)
      setError(err instanceof Error ? err.message : 'Błąd ładowania metryk')
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
          Brak sesji do wyświetlenia. Metryki pojawią się po utworzeniu sesji.
        </div>
      </div>
    )
  }

  // Filtruj tylko sesje z poprawnymi datami i sortuj chronologicznie
  const validMetrics = metrics
    .filter(m => {
      if (!m.sessionDate) return false
      const date = new Date(m.sessionDate)
      return !isNaN(date.getTime())
    })
    .sort((a, b) => {
      const dateA = new Date(a.sessionDate).getTime()
      const dateB = new Date(b.sessionDate).getTime()
      return dateA - dateB
    })

  if (validMetrics.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-lg p-6">
        <h3 className="text-xl font-bold text-gray-800 mb-4">Metryki sesji</h3>
        <div className="text-center text-gray-500 py-8">
          Brak poprawnych danych do wyświetlenia.
        </div>
      </div>
    )
  }

  // Formatuj daty
  const formatDate = (dateStr: string | Date) => {
    try {
      const date = dateStr instanceof Date ? dateStr : new Date(dateStr)
      if (isNaN(date.getTime())) {
        return String(dateStr)
      }
      return date.toLocaleDateString('pl-PL', { 
        day: '2-digit', 
        month: 'short', 
        year: 'numeric' 
      })
    } catch {
      return String(dateStr)
    }
  }

  // Twórz etykiety z dat sesji
  const labels = validMetrics.map(m => formatDate(m.sessionDate))

  // Przygotuj dane wykresu - konwertuj wszystkie wartości na liczby
  const chartData = {
    labels,
    datasets: [
      {
        label: 'Lęk',
        data: validMetrics.map(m => {
          const value = Number(m.lek) || 0
          return Math.max(0, Math.min(10, isNaN(value) ? 0 : value))
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
          const value = Number(m.depresja) || 0
          return Math.max(0, Math.min(10, isNaN(value) ? 0 : value))
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
          const value = Number(m.samopoczucie) || 0
          return Math.max(0, Math.min(10, isNaN(value) ? 0 : value))
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
          const value = Number(m.skalaBecka) || 0
          return Math.max(0, Math.min(10, isNaN(value) ? 0 : value))
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
      <div className="h-80">
        <Line data={chartData} options={options} />
      </div>
      {validMetrics.length > 0 && (
        <div className="mt-4 pt-4 border-t border-gray-200">
          <p className="text-xs text-gray-500">
              Dane z {validMetrics.length} {validMetrics.length === 1 ? 'sesji' : 'sesji'} • 
              Zakres: {formatDate(validMetrics[0].sessionDate)} - {formatDate(validMetrics[validMetrics.length - 1].sessionDate)}
          </p>
        </div>
      )}
    </div>
  )
}

