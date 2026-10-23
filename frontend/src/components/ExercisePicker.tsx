import { useState } from 'react'
import type { ExerciseLibrary } from '../api'
import { describeLast } from './format'

// Sök bland övningarna i biblioteket och de man har gjort förut, filtrera på muskelgrupp.
export default function ExercisePicker({ data, onPick, onClose }: {
  data: ExerciseLibrary
  onPick: (name: string) => void
  onClose: () => void
}) {
  const [query, setQuery] = useState('')
  const [muscle, setMuscle] = useState(data.recent.length ? 'Senaste' : 'Alla')
  const q = query.trim().toLowerCase()
  const recentByName = new Map(data.recent.map((r) => [r.name.toLowerCase(), r]))
  const libraryNames = new Set(data.library.map((e) => e.name.toLowerCase()))

  // Egna övningar som inte finns i biblioteket visas under "Egna".
  const own = data.recent.filter((r) => !libraryNames.has(r.name.toLowerCase())).map((r) => ({ name: r.name, muscle: 'Egna' }))
  const all = [...data.library, ...own]
  const items =
    muscle === 'Senaste' && !q
      ? data.recent.map((r) => ({ name: r.name, muscle: data.library.find((e) => e.name.toLowerCase() === r.name.toLowerCase())?.muscle ?? 'Egna' }))
      : all.filter((e) => (muscle === 'Alla' || muscle === 'Senaste' || e.muscle === muscle) && (!q || e.name.toLowerCase().includes(q)))
  const filters = [...(data.recent.length ? ['Senaste'] : []), 'Alla', ...data.muscles, ...(own.length ? ['Egna'] : [])]

  return (
    <div className="picker">
      <div className="picker-head">
        <input type="search" placeholder="Sök övning…" autoFocus value={query} onChange={(e) => setQuery(e.target.value)} />
        <button type="button" className="icon" title="Stäng" onClick={onClose}>×</button>
      </div>
      <div className="chips">
        {filters.map((m) => (
          <button key={m} type="button" className={m === muscle ? 'chip active' : 'chip'} onClick={() => setMuscle(m)}>{m}</button>
        ))}
      </div>
      <ul className="picker-list">
        {items.map((e) => {
          const recent = recentByName.get(e.name.toLowerCase())
          return (
            <li key={e.name}>
              <button type="button" onClick={() => onPick(e.name)}>
                <span>{e.name}</span>
                <span className="muted">{recent ? `förra: ${describeLast(recent)}` : e.muscle}</span>
              </button>
            </li>
          )
        })}
        {items.length === 0 && q && (
          <li>
            <button type="button" onClick={() => onPick(query.trim())}>
              <span>Lägg till "{query.trim()}"</span><span className="muted">egen övning</span>
            </button>
          </li>
        )}
      </ul>
    </div>
  )
}
