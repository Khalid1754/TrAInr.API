import { useEffect, useState, type FormEvent } from 'react'
import { api, type ExerciseInput, type ExerciseLibrary, type Workout, type WorkoutEstimate } from '../api'
import ExercisePicker from './ExercisePicker'
import { describeLast } from './format'
import RestTimer from './RestTimer'
import { useAsync } from './useAsync'

const emptyRow = (): ExerciseInput => ({ name: '', sets: 3, reps: 10, weightKg: 0, durationMin: null })
const today = () => new Date().toISOString().slice(0, 10)
const shortDate = (iso: string) => new Date(iso).toLocaleDateString('sv-SE', { day: 'numeric', month: 'numeric' })
const copyRows = (w: Workout) => w.exercises.map((e) => ({ ...e }))

// template = ett tidigare pass som ska göras igen (från Historik).
export default function LogWorkout({ onSaved, template }: { onSaved: () => void; template?: Workout | null }) {
  const [name, setName] = useState(template?.name ?? '')
  const [date, setDate] = useState(today)
  const [notes, setNotes] = useState('')
  const [rows, setRows] = useState<ExerciseInput[]>(() => (template ? copyRows(template) : [emptyRow()]))
  const [estimate, setEstimate] = useState<{ volumeKg: number; estimate: WorkoutEstimate } | null>(null)
  const [library, setLibrary] = useState<ExerciseLibrary | null>(null)
  const [previous, setPrevious] = useState<Workout[]>([])
  const [picking, setPicking] = useState(false)
  const { busy, error, run } = useAsync()

  useEffect(() => {
    api.exercises().then(setLibrary).catch(() => {})
    api.listWorkouts().then((w) => setPrevious(w.slice(0, 15))).catch(() => {})
  }, [])

  const recent = new Map((library?.recent ?? []).map((r) => [r.name.toLowerCase(), r]))
  const suggestions = [...new Set([...(library?.recent ?? []).map((r) => r.name), ...(library?.library ?? []).map((e) => e.name)])]

  function copyWorkout(id: string) {
    const w = previous.find((p) => String(p.id) === id)
    if (!w) return
    setName(w.name)
    setRows(copyRows(w))
  }

  // Ny rad från biblioteket, ifylld med samma värden som förra gången.
  function addExercise(exerciseName: string) {
    const last = recent.get(exerciseName.toLowerCase())
    const isCardio = library?.library.find((e) => e.name === exerciseName)?.category === 'cardio'
    const row: ExerciseInput = last
      ? { name: exerciseName, sets: last.sets, reps: last.reps, weightKg: last.weightKg, durationMin: last.durationMin }
      : isCardio ? { name: exerciseName, sets: 1, reps: 1, weightKg: 0, durationMin: 20 } : { ...emptyRow(), name: exerciseName }
    setRows((prev) => [...prev.filter((r) => r.name.trim()), row])
    setPicking(false)
  }

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
        {previous.length > 0 && (
          <label>
            Kopiera ett tidigare pass
            <select value="" onChange={(e) => copyWorkout(e.target.value)}>
              <option value="">Välj pass…</option>
              {previous.map((w) => <option key={w.id} value={w.id}>{shortDate(w.date)} · {w.name}</option>)}
            </select>
          </label>
        )}
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
                  {recent.has(r.name.trim().toLowerCase()) && (
                    <small className="muted last-time">
                      Förra gången ({shortDate(recent.get(r.name.trim().toLowerCase())!.lastDate)}): {describeLast(recent.get(r.name.trim().toLowerCase())!)}
                    </small>
                  )}
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
        {picking && library ? (
          <ExercisePicker data={library} onPick={addExercise} onClose={() => setPicking(false)} />
        ) : (
          <div className="actions">
            {library && <button type="button" className="secondary" onClick={() => setPicking(true)}>+ Välj övning</button>}
            <button type="button" className="secondary" onClick={() => setRows((prev) => [...prev, emptyRow()])}>+ Tom rad</button>
          </div>
        )}
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
      <RestTimer />
    </section>
  )
}
