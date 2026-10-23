import { useState } from 'react'
import type { ExerciseLibrary } from '../api'
import { msg, useLang } from '../i18n'
import { describeLast } from './format'

// Filtren utöver muskelgrupperna (som kommer från servern på valt språk).
const RECENT = 'recent'
const ALL = 'all'
const OWN = 'own'
const filterNames: Record<string, string> = { [RECENT]: msg('Senaste'), [ALL]: msg('Alla'), [OWN]: msg('Egna') }

// Sök bland övningarna i biblioteket och de man har gjort förut, filtrera på muskelgrupp.
export default function ExercisePicker({ data, onPick, onClose }: {
  data: ExerciseLibrary
  onPick: (name: string) => void
  onClose: () => void
}) {
  const { t } = useLang()
  const [query, setQuery] = useState('')
  const [muscle, setMuscle] = useState(data.recent.length ? RECENT : ALL)
  const q = query.trim().toLowerCase()
  const recentByName = new Map(data.recent.map((r) => [r.name.toLowerCase(), r]))
  const libraryNames = new Set(data.library.map((e) => e.name.toLowerCase()))

  // Egna övningar som inte finns i biblioteket visas under "Egna".
  const own = data.recent.filter((r) => !libraryNames.has(r.name.toLowerCase())).map((r) => ({ name: r.name, muscle: OWN }))
  const all = [...data.library, ...own]
  const items =
    muscle === RECENT && !q
      ? data.recent.map((r) => ({ name: r.name, muscle: data.library.find((e) => e.name.toLowerCase() === r.name.toLowerCase())?.muscle ?? OWN }))
      : all.filter((e) => (muscle === ALL || muscle === RECENT || e.muscle === muscle) && (!q || e.name.toLowerCase().includes(q)))
  const filters = [...(data.recent.length ? [RECENT] : []), ALL, ...data.muscles, ...(own.length ? [OWN] : [])]
  const label = (m: string) => (m in filterNames ? t(filterNames[m]) : m)

  return (
    <div className="picker">
      <div className="picker-head">
        <input type="search" placeholder={t('Sök övning…')} autoFocus value={query} onChange={(e) => setQuery(e.target.value)} />
        <button type="button" className="icon" title={t('Stäng')} onClick={onClose}>×</button>
      </div>
      <div className="chips">
        {filters.map((m) => (
          <button key={m} type="button" className={m === muscle ? 'chip active' : 'chip'} onClick={() => setMuscle(m)}>{label(m)}</button>
        ))}
      </div>
      <ul className="picker-list">
        {items.map((e) => {
          const recent = recentByName.get(e.name.toLowerCase())
          return (
            <li key={e.name}>
              <button type="button" onClick={() => onPick(e.name)}>
                <span>{e.name}</span>
                <span className="muted">{recent ? t('förra: {last}', { last: describeLast(recent) }) : label(e.muscle)}</span>
              </button>
            </li>
          )
        })}
        {items.length === 0 && q && (
          <li>
            <button type="button" onClick={() => onPick(query.trim())}>
              <span>{t('Lägg till "{name}"', { name: query.trim() })}</span><span className="muted">{t('egen övning')}</span>
            </button>
          </li>
        )}
      </ul>
    </div>
  )
}
