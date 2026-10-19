import { useEffect, useState, type FormEvent } from 'react'
import { api, type ExerciseInput, type WorkoutEstimate } from '../api'
import { useAsync } from './useAsync'

const suggestions = [
  'Knäböj', 'Marklyft', 'Bänkpress', 'Militärpress', 'Skivstångsrodd', 'Chins', 'Dips', 'Utfallssteg',
  'Hip thrust', 'Bicepscurl', 'Triceps pushdown', 'Sidolyft', 'Benpress', 'Latsdrag', 'Löpning', 'Roddmaskin',
]

const emptyRow = (): ExerciseInput => ({ name: '', sets: 3, reps: 10, weightKg: 0, durationMin: null })
const today = () => new Date().toISOString().slice(0, 10)

export default function LogWorkout({ onSaved }: { onSaved: () => void }) {
  const [name, setName] = useState('')
  const [date, setDate] = useState(today)
  const [notes, setNotes] = useState('')
  const [rows, setRows] = useState<ExerciseInput[]>([emptyRow()])
  const [estimate, setEstimate] = useState<{ volumeKg: number; estimate: WorkoutEstimate } | null>(null)
  const { busy, error, run } = useAsync()

  // Snabbuppskattning från backenden medan man fyller i.
  useEffect(() => {
    const exercises = rows.filter((r) => r.name.trim())
    const timer = setTimeout(() => {
      if (!exercises.length) return setEstimate(null)
      api.estimate({ name: '', date: today(), notes: '', exercises }).then(setEstimate).catch(() => setEstimate(null))
    }, 300)
    return () => clearTimeout(timer)
  }, [rows])

  function updateRow(i: number, patch: Partial<ExerciseInput>) {
    setRows((prev) => prev.map((r, j) => (j === i ? { ...r, ...patch } : r)))
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    const saved = await run(() => api.createWorkout({ name, date, notes, exercises: rows.filter((r) => r.name.trim()) }))
    if (saved) onSaved()
  }

  const num = (v: string) => (v === '' ? 0 : Number(v))

  return (
    <section>
      <h2>Nytt träningspass</h2>
      <form onSubmit={submit}>
        <div className="row">
          <label>
            Namn <input value={name} onChange={(e) => setName(e.target.value)} placeholder="t.ex. Ben & rygg" required />
          </label>
          <label>
            Datum <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
          </label>
        </div>
        <table className="exercises">
          <thead>
            <tr>
              <th>Övning</th><th>Set</th><th>Reps</th><th>Vikt (kg)</th><th>Min (kondition)</th><th />
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={i}>
                <td>
                  <input list="exercise-suggestions" value={r.name} placeholder="Bänkpress" required
                    onChange={(e) => updateRow(i, { name: e.target.value })} />
                </td>
                <td><input type="number" min={0} value={r.sets} onChange={(e) => updateRow(i, { sets: num(e.target.value) })} /></td>
                <td><input type="number" min={0} value={r.reps} onChange={(e) => updateRow(i, { reps: num(e.target.value) })} /></td>
                <td><input type="number" min={0} step={0.5} value={r.weightKg} onChange={(e) => updateRow(i, { weightKg: num(e.target.value) })} /></td>
                <td>
                  <input type="number" min={0} placeholder="–" value={r.durationMin ?? ''}
                    onChange={(e) => updateRow(i, { durationMin: e.target.value === '' ? null : Number(e.target.value) })} />
                </td>
                <td>
                  <button type="button" className="icon" title="Ta bort" disabled={rows.length === 1}
                    onClick={() => setRows((prev) => prev.filter((_, j) => j !== i))}>×</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <datalist id="exercise-suggestions">
          {suggestions.map((s) => <option key={s} value={s} />)}
        </datalist>
        <button type="button" className="secondary" onClick={() => setRows((prev) => [...prev, emptyRow()])}>
          + Lägg till övning
        </button>
        <label>
          Anteckningar <textarea rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Hur kändes passet?" />
        </label>
        {estimate && (
          <div className="muted">
            Snabbuppskattning: ca {estimate.estimate.totalKcal} kcal · {estimate.estimate.totalMinutes} min · total volym{' '}
            {Math.round(estimate.volumeKg)} kg
          </div>
        )}
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy}>{busy ? 'Sparar…' : 'Spara pass'}</button>
      </form>
    </section>
  )
}
