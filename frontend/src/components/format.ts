import type { RecentExercise } from '../api'

// "3 × 8 × 70 kg", "3 × 10" eller "20 min", som det visas i "Förra gången".
export const describeLast = (r: RecentExercise) =>
  r.durationMin ? `${r.durationMin} min` : r.weightKg > 0 ? `${r.sets} × ${r.reps} × ${r.weightKg} kg` : `${r.sets} × ${r.reps}`
