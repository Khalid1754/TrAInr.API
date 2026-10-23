import { useState, type FormEvent } from 'react'
import { api, type PlanResult } from '../api'
import { useLang } from '../i18n'
import { useAsync } from './useAsync'

export default function Plan() {
  const [wishes, setWishes] = useState('')
  const [plan, setPlan] = useState<PlanResult | null>(null)
  const { busy, error, run } = useAsync()
  const { t } = useLang()

  async function submit(e: FormEvent) {
    e.preventDefault()
    const result = await run(() => api.aiPlan(wishes))
    if (result) setPlan(result)
  }

  return (
    <section>
      <h2>{t('AI-tränarens veckoschema')}</h2>
      <p className="muted">{t('Schemat bygger på din profil och dina loggade pass.')}</p>
      <form onSubmit={submit}>
        <label>
          {t('Önskemål (valfritt)')}
          <input value={wishes} onChange={(e) => setWishes(e.target.value)}
            placeholder={t('t.ex. vill fokusera på marklyft, har ont i axeln')} />
        </label>
        <button type="submit" disabled={busy}>{busy ? t('Tänker…') : t('Ta fram schema')}</button>
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
                  <tr><th>{t('Övning')}</th><th>{t('Set')}</th><th>{t('Reps')}</th><th>{t('Vikt')}</th><th>{t('Tips')}</th></tr>
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
          <h3>{t('Progression')}</h3>
          <ul className="compact">
            {plan.progressionTips.map((t, i) => <li key={i}>{t}</li>)}
          </ul>
        </div>
      )}
    </section>
  )
}
