import { useEffect, useState, type FormEvent } from 'react'
import { api, type Profile } from '../api'
import { useAsync } from './useAsync'

export default function ProfileForm({ email }: { email: string | null }) {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [saved, setSaved] = useState(false)
  const { busy, error, run } = useAsync()

  useEffect(() => {
    api.getProfile().then(setProfile)
  }, [])

  if (!profile) return <p className="muted">Laddar…</p>

  const set = <K extends keyof Profile>(key: K, value: Profile[K]) => setProfile({ ...profile, [key]: value })

  async function submit(e: FormEvent) {
    e.preventDefault()
    const result = await run(() => api.saveProfile(profile!))
    if (result) {
      setProfile(result)
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    }
  }

  return (
    <section>
      <h2>Profil</h2>
      {email && <p className="muted">Inloggad som {email}</p>}
      <form onSubmit={submit}>
        <div className="row">
          <label>Namn <input value={profile.name} onChange={(e) => set('name', e.target.value)} /></label>
          <label>Ålder <input type="number" min={10} max={100} value={profile.age} onChange={(e) => set('age', Number(e.target.value))} /></label>
          <label>
            Kön
            <select value={profile.sex} onChange={(e) => set('sex', e.target.value)}>
              <option value="unspecified">Vill ej ange</option>
              <option value="man">Man</option>
              <option value="kvinna">Kvinna</option>
            </select>
          </label>
        </div>
        <div className="row">
          <label>Kroppsvikt (kg) <input type="number" step={0.1} min={30} value={profile.bodyWeightKg} onChange={(e) => set('bodyWeightKg', Number(e.target.value))} /></label>
          <label>Längd (cm) <input type="number" min={100} value={profile.heightCm} onChange={(e) => set('heightCm', Number(e.target.value))} /></label>
          <label>Träningsdagar/vecka <input type="number" min={1} max={7} value={profile.daysPerWeek} onChange={(e) => set('daysPerWeek', Number(e.target.value))} /></label>
        </div>
        <div className="row">
          <label>
            Mål
            <select value={profile.goal} onChange={(e) => set('goal', e.target.value)}>
              {['muskelbygge', 'styrka', 'viktnedgång', 'kondition', 'allmän hälsa'].map((g) => <option key={g}>{g}</option>)}
            </select>
          </label>
          <label>
            Erfarenhet
            <select value={profile.experience} onChange={(e) => set('experience', e.target.value)}>
              {['nybörjare', 'medel', 'avancerad'].map((x) => <option key={x}>{x}</option>)}
            </select>
          </label>
        </div>
        <label>
          Kostpreferenser / allergier
          <input value={profile.dietaryPreferences} onChange={(e) => set('dietaryPreferences', e.target.value)}
            placeholder="t.ex. vegetarian, laktosintolerant" />
        </label>
        {error && <p className="error">{error}</p>}
        <div>
          <button type="submit" disabled={busy}>Spara profil</button> {saved && <span className="muted">Sparat ✓</span>}
        </div>
      </form>
    </section>
  )
}
