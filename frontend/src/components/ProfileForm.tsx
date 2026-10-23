import { useEffect, useState, type FormEvent } from 'react'
import { api, type Profile } from '../api'
import { msg, useLang, type Lang } from '../i18n'
import type { Theme } from '../theme'
import { useAsync } from './useAsync'

// Värdena sparas på svenska (AI:n läser dem), bara texten som visas översätts.
const goals = [msg('muskelbygge'), msg('styrka'), msg('viktnedgång'), msg('kondition'), msg('allmän hälsa')]
const levels = [msg('nybörjare'), msg('medel'), msg('avancerad')]

export default function ProfileForm({ email }: { email: string | null }) {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [saved, setSaved] = useState(false)
  const { busy, error, run } = useAsync()
  const { t, lang, setLang, theme, setTheme } = useLang()

  useEffect(() => {
    api.getProfile().then(setProfile)
  }, [])

  if (!profile) return <p className="muted">{t('Laddar…')}</p>

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
      <h2>{t('Profil')}</h2>
      {email && <p className="muted">{t('Inloggad som {email}', { email })}</p>}
      <form onSubmit={submit}>
        <div className="row">
          <label>{t('Namn')} <input value={profile.name} onChange={(e) => set('name', e.target.value)} /></label>
          <label>{t('Ålder')} <input type="number" min={10} max={100} value={profile.age} onChange={(e) => set('age', Number(e.target.value))} /></label>
          <label>
            {t('Kön')}
            <select value={profile.sex} onChange={(e) => set('sex', e.target.value)}>
              <option value="unspecified">{t('Vill ej ange')}</option>
              <option value="man">{t('Man')}</option>
              <option value="kvinna">{t('Kvinna')}</option>
            </select>
          </label>
        </div>
        <div className="row">
          <label>{t('Kroppsvikt (kg)')} <input type="number" step={0.1} min={30} value={profile.bodyWeightKg} onChange={(e) => set('bodyWeightKg', Number(e.target.value))} /></label>
          <label>{t('Längd (cm)')} <input type="number" min={100} value={profile.heightCm} onChange={(e) => set('heightCm', Number(e.target.value))} /></label>
          <label>{t('Träningsdagar/vecka')} <input type="number" min={1} max={7} value={profile.daysPerWeek} onChange={(e) => set('daysPerWeek', Number(e.target.value))} /></label>
        </div>
        <div className="row">
          <label>
            {t('Mål')}
            <select value={profile.goal} onChange={(e) => set('goal', e.target.value)}>
              {goals.map((g) => <option key={g} value={g}>{t(g)}</option>)}
            </select>
          </label>
          <label>
            {t('Erfarenhet')}
            <select value={profile.experience} onChange={(e) => set('experience', e.target.value)}>
              {levels.map((x) => <option key={x} value={x}>{t(x)}</option>)}
            </select>
          </label>
        </div>
        <label>
          {t('Kostpreferenser / allergier')}
          <input value={profile.dietaryPreferences} onChange={(e) => set('dietaryPreferences', e.target.value)}
            placeholder={t('t.ex. vegetarian, laktosintolerant')} />
        </label>
        {error && <p className="error">{error}</p>}
        <div>
          <button type="submit" disabled={busy}>{t('Spara profil')}</button> {saved && <span className="muted">{t('Sparat ✓')}</span>}
        </div>
      </form>

      <h3>{t('Utseende och språk')}</h3>
      <div className="card row">
        <label>
          {t('Tema')}
          <select value={theme} onChange={(e) => setTheme(e.target.value as Theme)}>
            <option value="system">{t('Som telefonen/datorn')}</option>
            <option value="light">{t('Ljust')}</option>
            <option value="dark">{t('Mörkt')}</option>
          </select>
        </label>
        <label>
          {t('Språk')}
          <select value={lang} onChange={(e) => setLang(e.target.value as Lang)}>
            <option value="sv">Svenska</option>
            <option value="en">English</option>
          </select>
        </label>
      </div>
    </section>
  )
}
