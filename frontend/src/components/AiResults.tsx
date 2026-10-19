import type { CaloriesResult, MealResult } from '../api'

export function CaloriesBox({ c }: { c: CaloriesResult }) {
  return (
    <div className="ai-box">
      <h4>
        🔥 AI: ca {c.totalKcal} kcal ({c.rangeLowKcal}–{c.rangeHighKcal}) + {c.afterburnKcal} kcal efterförbränning
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
  return (
    <div className="ai-box">
      <h4>🥗 {m.title}</h4>
      <p>{m.why}</p>
      <p className="meta">
        {m.timing} · {m.macros.kcal} kcal · protein {m.macros.proteinG} g · kolhydrater {m.macros.carbsG} g · fett {m.macros.fatG} g
      </p>
      <b>Ingredienser</b>
      <ul className="compact">
        {m.ingredients.map((i) => <li key={i.item}>{i.amount} {i.item}</li>)}
      </ul>
      <b>Gör så här</b>
      <ol className="compact">
        {m.steps.map((s, i) => <li key={i}>{s}</li>)}
      </ol>
      <b>Snabba alternativ</b>
      <ul className="compact">
        {m.alternatives.map((a, i) => <li key={i}>{a}</li>)}
      </ul>
    </div>
  )
}
