import { useEffect, useState } from 'react'
import { api, type Workout } from '../api'
import { CaloriesBox, MealBox } from './AiResults'
import { useLang } from '../i18n'
import { useAsync } from './useAsync'

export default function History({ onRepeat }: { onRepeat: (w: Workout) => void }) {
  const [workouts, setWorkouts] = useState<Workout[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const { t } = useLang()

  useEffect(() => {
    api.listWorkouts().then(setWorkouts).catch((e: Error) => setLoadError(e.message))
  }, [])

  if (loadError) return <p className="error">{loadError}</p>
  if (!workouts) return <p className="muted">{t('Laddar…')}</p>

  return (
    <section>
      <h2>{t('Dina pass')}</h2>
      {workouts.length === 0 && <p className="muted">{t('Inga pass loggade ännu.')}</p>}
      {workouts.map((w) => (
        <WorkoutCard
          key={w.id}
          workout={w}
          onRepeat={() => onRepeat(w)}
          onChange={(updated) => setWorkouts((prev) => prev!.map((x) => (x.id === w.id ? updated : x)))}
          onDelete={() => setWorkouts((prev) => prev!.filter((x) => x.id !== w.id))}
        />
      ))}
    </section>
  )
}

function WorkoutCard({ workout: w, onRepeat, onChange, onDelete }: {
  workout: Workout
  onRepeat: () => void
  onChange: (w: Workout) => void
  onDelete: () => void
}) {
  const calories = useAsync()
  const meal = useAsync()
  const remove = useAsync()
  const { t } = useLang()

  async function estimateCalories() {
    const aiCalories = await calories.run(() => api.aiCalories(w.id))
    if (aiCalories) onChange({ ...w, aiCalories })
  }

  async function suggestMeal() {
    const aiMeal = await meal.run(() => api.aiMeal(w.id))
    if (aiMeal) onChange({ ...w, aiMeal })
  }

  async function deleteWorkout() {
    if (!confirm(t('Ta bort passet?'))) return
    await remove.run(() => api.deleteWorkout(w.id))
    onDelete()
  }

  const error = calories.error ?? meal.error ?? remove.error

  return (
    <div className="card">
      <h3>{w.name}</h3>
      <div className="meta">{w.date}{w.notes && ` · ${w.notes}`}</div>
      <ul className="compact">
        {w.exercises.map((e, i) => (
          <li key={i}>
            {e.name}: {e.durationMin ? `${e.durationMin} min` : `${e.sets}×${e.reps} @ ${e.weightKg} kg`}
          </li>
        ))}
      </ul>
      <div className="stats">
        <div className="stat"><b>{w.estimate.totalKcal}</b><span>{t('kcal (snabbuppskattning)')}</span></div>
        <div className="stat"><b>{w.estimate.totalMinutes}</b><span>min</span></div>
        <div className="stat"><b>{Math.round(w.volumeKg)}</b><span>{t('kg volym')}</span></div>
      </div>
      <div className="actions">
        <button onClick={estimateCalories} disabled={calories.busy}>{calories.busy ? t('Tänker…') : t('🔥 AI-kalorier')}</button>
        <button onClick={suggestMeal} disabled={meal.busy}>{meal.busy ? t('Tänker…') : t('🥗 Återhämtningsmåltid')}</button>
        <button className="secondary" onClick={onRepeat}>{t('Kör igen')}</button>
        <button className="danger" onClick={deleteWorkout} disabled={remove.busy}>{t('Ta bort')}</button>
      </div>
      {error && <p className="error">{error}</p>}
      {w.aiCalories && <CaloriesBox c={w.aiCalories} />}
      {w.aiMeal && <MealBox m={w.aiMeal} />}
    </div>
  )
}
