import { useEffect, useState } from 'react'
import { api, type Stats as StatsData } from '../api'
import { BarChart, LineChart } from './Charts'
import { useLang } from '../i18n'
import { weekNumber } from './format'

export default function Stats() {
  const [stats, setStats] = useState<StatsData | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [exerciseName, setExerciseName] = useState('')
  const [measure, setMeasure] = useState<'top' | 'oneRm'>('top')
  const { t, locale } = useLang()
  const kg = (n: number) => `${n.toLocaleString(locale)} kg`
  const shortDate = (iso: string) => new Date(iso).toLocaleDateString(locale, { day: 'numeric', month: 'numeric' })

  useEffect(() => {
    api.stats().then(setStats).catch((e: Error) => setLoadError(e.message))
  }, [])

  if (loadError) return <p className="error">{loadError}</p>
  if (!stats) return <p className="muted">{t('Laddar…')}</p>
  if (stats.summary.workouts === 0) return <section><h2>{t('Statistik')}</h2><p className="muted">{t('Logga ett pass så dyker statistiken upp här.')}</p></section>

  const { summary, weeks } = stats
  const weighted = stats.exercises.filter((e) => e.bestWeightKg > 0)
  const exercise = weighted.find((e) => e.name === exerciseName) ?? weighted[0]

  const weekLabel = (w: (typeof weeks)[number]) => t('v {week}', { week: weekNumber(w.weekStart) })
  const passText = (n: number) => (n === 1 ? t('1 pass') : t('{n} pass', { n }))

  return (
    <section>
      <h2>{t('Statistik')}</h2>
      <div className="card stats">
        <div className="stat"><b>{summary.workouts}</b><span>{t('pass totalt')}</span></div>
        <div className="stat"><b>{summary.workoutsLast30Days}</b><span>{t('senaste 30 dagarna')}</span></div>
        <div className="stat"><b>{summary.weekStreak}</b><span>{t('veckor i rad')}</span></div>
        <div className="stat"><b>{t('{n} ton', { n: (summary.totalVolumeKg / 1000).toLocaleString(locale, { maximumFractionDigits: 1 }) })}</b><span>{t('lyft totalt')}</span></div>
        <div className="stat"><b>{summary.totalKcal.toLocaleString(locale)}</b><span>{t('kcal totalt')}</span></div>
      </div>

      {exercise && (
        <div className="card">
          <h3>{t('Utveckling per övning')}</h3>
          <div className="chart-controls">
            <select value={exercise.name} onChange={(e) => setExerciseName(e.target.value)} aria-label={t('Övning')}>
              {weighted.map((e) => <option key={e.name} value={e.name}>{e.name}</option>)}
            </select>
            <div className="segmented">
              <button type="button" className={measure === 'top' ? 'active' : ''} onClick={() => setMeasure('top')}>{t('Tyngsta vikt')}</button>
              <button type="button" className={measure === 'oneRm' ? 'active' : ''} onClick={() => setMeasure('oneRm')}>{t('Uppskattat maxlyft')}</button>
            </div>
          </div>
          <p className="meta">
            {t('Personbästa:')} <b>{kg(exercise.bestWeightKg)}</b>
            {exercise.bestWeightDate && ` (${shortDate(exercise.bestWeightDate)})`} · {t('uppskattat maxlyft')} {kg(exercise.bestEstimated1RmKg)}
          </p>
          <LineChart
            label={measure === 'top'
              ? t('Tyngsta vikt i {name} per pass', { name: exercise.name })
              : t('Uppskattat maxlyft i {name} per pass', { name: exercise.name })}
            points={exercise.history.map((p) => ({
              label: shortDate(p.date),
              value: measure === 'top' ? p.topWeightKg : p.estimated1RmKg,
              tooltip: (
                <>
                  <b>{shortDate(p.date)}</b>
                  <div>{p.repsAtTop} × {kg(p.topWeightKg)}</div>
                  <div className="muted">{t('Maxlyft ≈')} {kg(p.estimated1RmKg)}</div>
                </>
              ),
            }))}
          />
          <p className="meta">{t('Maxlyftet räknas fram med Epleys formel: vikt × (1 + reps / 30).')}</p>
        </div>
      )}

      <div className="card">
        <h3>{t('Volym per vecka')}</h3>
        <p className="meta">{t('Set × reps × vikt, senaste {n} veckorna.', { n: weeks.length })}</p>
        <BarChart
          label={t('Volym per vecka i kilo')}
          points={weeks.map((w) => ({
            label: weekLabel(w),
            value: w.volumeKg,
            tooltip: <><b>{weekLabel(w)}</b><div>{kg(w.volumeKg)}</div><div className="muted">{passText(w.workouts)}</div></>,
          }))}
        />
      </div>

      <div className="card">
        <h3>{t('Kalorier per vecka')}</h3>
        <p className="meta">{t('AI:ns uppskattning där den finns, annars den vanliga.')}</p>
        <BarChart
          label={t('Förbrända kalorier per vecka')}
          points={weeks.map((w) => ({
            label: weekLabel(w),
            value: w.kcal,
            tooltip: <><b>{weekLabel(w)}</b><div>{w.kcal.toLocaleString(locale)} kcal</div><div className="muted">{passText(w.workouts)}</div></>,
          }))}
        />
      </div>

      {weighted.length > 0 && (
        <div className="card">
          <h3>{t('Personbästa')}</h3>
          <table className="records">
            <thead>
              <tr><th>{t('Övning')}</th><th>{t('Tyngsta vikt')}</th><th>{t('Maxlyft ≈')}</th><th>{t('Pass')}</th></tr>
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
