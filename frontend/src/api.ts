export interface ExerciseInput {
  name: string
  sets: number
  reps: number
  weightKg: number
  durationMin: number | null
}

export interface WorkoutInput {
  name: string
  date: string
  notes: string
  exercises: ExerciseInput[]
}

export interface ExerciseEstimate {
  name: string
  category: 'compound' | 'isolation' | 'bodyweight' | 'cardio'
  minutes: number
  kcal: number
}

export interface WorkoutEstimate {
  totalKcal: number
  totalMinutes: number
  exercises: ExerciseEstimate[]
}

export interface CaloriesResult {
  totalKcal: number
  rangeLowKcal: number
  rangeHighKcal: number
  exercises: { name: string; kcal: number; comment: string }[]
  afterburnKcal: number
  explanation: string
}

export interface MealResult {
  title: string
  why: string
  timing: string
  macros: { kcal: number; proteinG: number; carbsG: number; fatG: number }
  ingredients: { item: string; amount: string }[]
  steps: string[]
  alternatives: string[]
}

export interface Workout {
  id: number
  name: string
  date: string
  notes: string
  exercises: ExerciseInput[]
  volumeKg: number
  estimate: WorkoutEstimate
  aiCalories: CaloriesResult | null
  aiMeal: MealResult | null
}

export interface PlanResult {
  summary: string
  analysis: string
  days: {
    day: string
    focus: string
    exercises: { name: string; sets: number; reps: string; suggestedWeightKg: number | null; note: string }[]
  }[]
  progressionTips: string[]
}

export interface Profile {
  name: string
  bodyWeightKg: number
  heightCm: number
  age: number
  sex: string
  goal: string
  experience: string
  daysPerWeek: number
  dietaryPreferences: string
}

// App.tsx visar inloggningen igen om servern svarar 401 (t.ex. om lösenordet har bytts).
let onUnauthorized = () => {}
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

async function request<T>(path: string, init: { method?: string; body?: unknown } = {}): Promise<T> {
  const res = await fetch(path, {
    method: init.method ?? 'GET',
    headers: { 'Content-Type': 'application/json' },
    body: init.body === undefined ? undefined : JSON.stringify(init.body),
  })
  if (res.status === 401 && !path.startsWith('/api/auth/')) onUnauthorized()
  if (res.status === 204) return undefined as T
  const data = await res.json().catch(() => ({}))
  if (!res.ok) {
    // ASP.NET:s valideringsfel kommer som { errors: {...} }, våra egna som { error: "..." }.
    const message = data.error ?? (data.errors ? 'Kontrollera fälten och försök igen.' : `Fel ${res.status}`)
    throw new Error(message)
  }
  return data as T
}

export const api = {
  me: () => request<{ loginRequired: boolean; loggedIn: boolean }>('/api/auth/me'),
  login: (password: string) => request<void>('/api/auth/login', { method: 'POST', body: { password } }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  status: () => request<{ ai: boolean; provider: string | null; database: string | null }>('/api/status'),
  getProfile: () => request<Profile>('/api/profile'),
  saveProfile: (p: Profile) => request<Profile>('/api/profile', { method: 'PUT', body: p }),
  listWorkouts: () => request<Workout[]>('/api/workouts'),
  createWorkout: (w: WorkoutInput) => request<Workout>('/api/workouts', { method: 'POST', body: w }),
  deleteWorkout: (id: number) => request<void>(`/api/workouts/${id}`, { method: 'DELETE' }),
  estimate: (w: WorkoutInput) =>
    request<{ volumeKg: number; estimate: WorkoutEstimate }>('/api/estimate', { method: 'POST', body: w }),
  aiCalories: (id: number) => request<CaloriesResult>(`/api/workouts/${id}/ai/calories`, { method: 'POST' }),
  aiMeal: (id: number) => request<MealResult>(`/api/workouts/${id}/ai/meal`, { method: 'POST' }),
  aiPlan: (wishes: string) => request<PlanResult>('/api/ai/plan', { method: 'POST', body: { wishes } }),
}
