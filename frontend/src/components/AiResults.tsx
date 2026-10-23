import type { CaloriesResult, MealResult } from '../api'
import { useLang } from '../i18n'

export function CaloriesBox({ c }: { c: CaloriesResult }) {
  const { t } = useLang()
  return (
    <div className="ai-box">
      <h4>
        {t('🔥 AI: ca {kcal} kcal ({low}–{high}) + {after} kcal efterförbränning', {
          kcal: c.totalKcal, low: c.rangeLowKcal, high: c.rangeHighKcal, after: c.afterburnKcal,
        })}
      </h4>
      <ul className="compact">
        {c.exercises.map((e) => (
          <li key={e.name}><b>{e.name}</b>: {e.kcal} kcal – {e.comment}</li>
        ))}
      </ul>
      <p className="muted">{c.explanation}</p>
    </div>
  )
}

export function MealBox({ m }: { m: MealResult }) {
  const { t } = useLang()
  return (
    <div className="ai-box">
      <h4>🥗 {m.title}</h4>
      <p>{m.why}</p>
      <p className="meta">
        {m.timing} · {t('{kcal} kcal · protein {protein} g · kolhydrater {carbs} g · fett {fat} g', {
          kcal: m.macros.kcal, protein: m.macros.proteinG, carbs: m.macros.carbsG, fat: m.macros.fatG,
        })}
      </p>
      <b>{t('Ingredienser')}</b>
      <ul className="compact">
        {m.ingredients.map((i) => <li key={i.item}>{i.amount} {i.item}</li>)}
      </ul>
      <b>{t('Gör så här')}</b>
      <ol className="compact">
        {m.steps.map((s, i) => <li key={i}>{s}</li>)}
      </ol>
      <b>{t('Snabba alternativ')}</b>
      <ul className="compact">
        {m.alternatives.map((a, i) => <li key={i}>{a}</li>)}
      </ul>
    </div>
  )
}
