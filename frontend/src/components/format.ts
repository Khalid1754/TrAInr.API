import type { RecentExercise } from '../api'

// "3 × 8 × 70 kg", "3 × 10" eller "20 min", som det visas i "Förra gången".
export const describeLast = (r: RecentExercise) =>
  r.durationMin ? `${r.durationMin} min` : r.weightKg > 0 ? `${r.sets} × ${r.reps} × ${r.weightKg} kg` : `${r.sets} × ${r.reps}`

// Veckonummer enligt ISO 8601, som i svenska kalendrar.
export function weekNumber(iso: string) {
  const d = new Date(iso)
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  const yearStart = Date.UTC(d.getUTCFullYear(), 0, 1)
  return Math.ceil(((d.getTime() - yearStart) / 86400000 + 1) / 7)
}
