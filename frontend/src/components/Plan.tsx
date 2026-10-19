import { useState, type FormEvent } from 'react'
import { api, type PlanResult } from '../api'
import { useAsync } from './useAsync'

export default function Plan() {
  const [wishes, setWishes] = useState('')
  const [plan, setPlan] = useState<PlanResult | null>(null)
  const { busy, error, run } = useAsync()

  async function submit(e: FormEvent) {
    e.preventDefault()
    const result = await run(() => api.aiPlan(wishes))
    if (result) setPlan(result)
  }

  return (
    <section>
      <h2>AI-tränarens veckoschema</h2>
      <p className="muted">Schemat bygger på din profil och dina loggade pass.</p>
      <form onSubmit={submit}>
        <label>
          Önskemål (valfritt)
          <input value={wishes} onChange={(e) => setWishes(e.target.value)}
            placeholder="t.ex. vill fokusera på marklyft, har ont i axeln" />
        </label>
        <button type="submit" disabled={busy}>{busy ? 'Tänker…' : 'Ta fram schema'}</button>
      </form>
      {error && <p className="error">{error}</p>}
      {plan && (
        <div className="card plan">
          <p>{plan.summary}</p>
          <p className="muted">{plan.analysis}</p>
          {plan.days.map((d) => (
            <div className="plan-day" key={d.day}>
              <h3>{d.day} – {d.focus}</h3>
              <table>
                <thead>
                  <tr><th>Övning</th><th>Set</th><th>Reps</th><th>Vikt</th><th>Tips</th></tr>
                </thead>
                <tbody>
                  {d.exercises.map((x, i) => (
                    <tr key={i}>
                      <td>{x.name}</td>
                      <td>{x.sets}</td>
                      <td>{x.reps}</td>
                      <td>{x.suggestedWeightKg != null ? `${x.suggestedWeightKg} kg` : '–'}</td>
                      <td>{x.note}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
          <h3>Progression</h3>
          <ul className="compact">
            {plan.progressionTips.map((t, i) => <li key={i}>{t}</li>)}
          </ul>
        </div>
      )}
    </section>
  )
}
