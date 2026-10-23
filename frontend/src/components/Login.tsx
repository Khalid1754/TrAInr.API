import { useState, type FormEvent } from 'react'
import { api } from '../api'
import { useLang } from '../i18n'
import Preferences from './Preferences'
import { useAsync } from './useAsync'

export default function Login({ inviteRequired, onLoggedIn }: { inviteRequired: boolean; onLoggedIn: () => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [inviteCode, setInviteCode] = useState('')
  const { busy, error, run } = useAsync()
  const { t } = useLang()
  const registering = mode === 'register'

  async function submit(e: FormEvent) {
    e.preventDefault()
    const ok = await run(async () => {
      if (registering) await api.register({ name, email, password, inviteCode })
      else await api.login(email, password)
      return true
    })
    if (ok) onLoggedIn()
  }

  return (
    <main className="login">
      <Preferences />
      <h1>
        Tr<span>AI</span>nr
      </h1>
      <form onSubmit={submit}>
        <h2>{registering ? t('Skapa konto') : t('Logga in')}</h2>
        {registering && (
          <label>
            {t('Namn')}
            <input name="name" autoComplete="name" value={name} onChange={(e) => setName(e.target.value)} />
          </label>
        )}
        <label>
          {t('E-post')}
          <input type="email" name="email" autoComplete="username" autoFocus required
            value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          {t('Lösenord')}
          <input type="password" name="password" required minLength={registering ? 8 : undefined}
            autoComplete={registering ? 'new-password' : 'current-password'} placeholder={registering ? t('minst 8 tecken') : undefined}
            value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>
        {registering && inviteRequired && (
          <label>
            {t('Inbjudningskod')}
            <input name="invite" autoComplete="off" required value={inviteCode} onChange={(e) => setInviteCode(e.target.value)} />
          </label>
        )}
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy || !email || !password}>
          {registering ? t('Skapa konto') : t('Logga in')}
        </button>
        <p className="muted">
          {registering ? t('Har du redan ett konto?') : t('Ny här?')}{' '}
          <button type="button" className="link" onClick={() => setMode(registering ? 'login' : 'register')}>
            {registering ? t('Logga in') : t('Skapa konto')}
          </button>
        </p>
      </form>
    </main>
  )
}
