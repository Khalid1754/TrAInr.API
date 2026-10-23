import { useEffect, useState } from 'react'
import { api, type Stats as StatsData } from '../api'
import { BarChart, LineChart } from './Charts'

const kg = (n: number) => `${n.toLocaleString('sv-SE')} kg`
const shortDate = (iso: string) => new Date(iso).toLocaleDateString('sv-SE', { day: 'numeric', month: 'numeric' })

// Veckonummer enligt ISO 8601, som i svenska kalendrar.
function weekNumber(iso: string) {
  const d = new Date(iso)
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  const yearStart = Date.UTC(d.getUTCFullYear(), 0, 1)
  return Math.ceil(((d.getTime() - yearStart) / 86400000 + 1) / 7)
}

export default function Stats() {
  const [stats, setStats] = useState<StatsData | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [exerciseName, setExerciseName] = useState('')
  const [measure, setMeasure] = useState<'top' | 'oneRm'>('top')

  useEffect(() => {
    api.stats().then(setStats).catch((e: Error) => setLoadError(e.message))
  }, [])

  if (loadError) return <p className="error">{loadError}</p>
  if (!stats) return <p className="muted">Laddar…</p>
  if (stats.summary.workouts === 0) return <section><h2>Statistik</h2><p className="muted">Logga ett pass så dyker statistiken upp här.</p></section>

  const { summary, weeks } = stats
  const weighted = stats.exercises.filter((e) => e.bestWeightKg > 0)
  const exercise = weighted.find((e) => e.name === exerciseName) ?? weighted[0]

  const weekLabel = (w: (typeof weeks)[number]) => `v ${weekNumber(w.weekStart)}`
  const passText = (n: number) => `${n} pass`

  return (
    <section>
      <h2>Statistik</h2>
      <div className="card stats">
        <div className="stat"><b>{summary.workouts}</b><span>pass totalt</span></div>
        <div className="stat"><b>{summary.workoutsLast30Days}</b><span>senaste 30 dagarna</span></div>
        <div className="stat"><b>{summary.weekStreak}</b><span>veckor i rad</span></div>
        <div className="stat"><b>{(summary.totalVolumeKg / 1000).toLocaleString('sv-SE', { maximumFractionDigits: 1 })} ton</b><span>lyft totalt</span></div>
        <div className="stat"><b>{summary.totalKcal.toLocaleString('sv-SE')}</b><span>kcal totalt</span></div>
      </div>

      {exercise && (
        <div className="card">
          <h3>Utveckling per övning</h3>
          <div className="chart-controls">
            <select value={exercise.name} onChange={(e) => setExerciseName(e.target.value)} aria-label="Övning">
              {weighted.map((e) => <option key={e.name} value={e.name}>{e.name}</option>)}
            </select>
            <div className="segmented">
              <button type="button" className={measure === 'top' ? 'active' : ''} onClick={() => setMeasure('top')}>Tyngsta vikt</button>
              <button type="button" className={measure === 'oneRm' ? 'active' : ''} onClick={() => setMeasure('oneRm')}>Uppskattat maxlyft</button>
            </div>
          </div>
          <p className="meta">
            Personbästa: <b>{kg(exercise.bestWeightKg)}</b>
            {exercise.bestWeightDate && ` (${shortDate(exercise.bestWeightDate)})`} · uppskattat maxlyft {kg(exercise.bestEstimated1RmKg)}
          </p>
          <LineChart
            label={`${measure === 'top' ? 'Tyngsta vikt' : 'Uppskattat maxlyft'} i ${exercise.name} per pass`}
            points={exercise.history.map((p) => ({
              label: shortDate(p.date),
              value: measure === 'top' ? p.topWeightKg : p.estimated1RmKg,
              tooltip: (
                <>
                  <b>{shortDate(p.date)}</b>
                  <div>{p.repsAtTop} × {kg(p.topWeightKg)}</div>
                  <div className="muted">Maxlyft ≈ {kg(p.estimated1RmKg)}</div>
                </>
              ),
            }))}
          />
          <p className="meta">Maxlyftet räknas fram med Epleys formel: vikt × (1 + reps / 30).</p>
        </div>
      )}

      <div className="card">
        <h3>Volym per vecka</h3>
        <p className="meta">Set × reps × vikt, senaste {weeks.length} veckorna.</p>
        <BarChart
          label="Volym per vecka i kilo"
          points={weeks.map((w) => ({
            label: weekLabel(w),
            value: w.volumeKg,
            tooltip: <><b>{weekLabel(w)}</b><div>{kg(w.volumeKg)}</div><div className="muted">{passText(w.workouts)}</div></>,
          }))}
        />
      </div>

      <div className="card">
        <h3>Kalorier per vecka</h3>
        <p className="meta">AI:ns uppskattning där den finns, annars den vanliga.</p>
        <BarChart
          label="Förbrända kalorier per vecka"
          points={weeks.map((w) => ({
            label: weekLabel(w),
            value: w.kcal,
            tooltip: <><b>{weekLabel(w)}</b><div>{w.kcal.toLocaleString('sv-SE')} kcal</div><div className="muted">{passText(w.workouts)}</div></>,
          }))}
        />
      </div>

      {weighted.length > 0 && (
        <div className="card">
          <h3>Personbästa</h3>
          <table className="records">
            <thead>
              <tr><th>Övning</th><th>Tyngsta vikt</th><th>Maxlyft ≈</th><th>Pass</th></tr>
            </thead>
            <tbody>
              {weighted.map((e) => (
                <tr key={e.name}>
                  <td>{e.name}</td>
                  <td>{kg(e.bestWeightKg)} <span className="muted">{e.bestWeightDate && shortDate(e.bestWeightDate)}</span></td>
                  <td>{kg(e.bestEstimated1RmKg)}</td>
                  <td>{e.sessions}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
