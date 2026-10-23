import { useState, type FormEvent } from 'react'
import { api } from '../api'
import { useAsync } from './useAsync'

export default function Login({ inviteRequired, onLoggedIn }: { inviteRequired: boolean; onLoggedIn: () => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [inviteCode, setInviteCode] = useState('')
  const { busy, error, run } = useAsync()
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
      <h1>
        Tr<span>AI</span>nr
      </h1>
      <form onSubmit={submit}>
        <h2>{registering ? 'Skapa konto' : 'Logga in'}</h2>
        {registering && (
          <label>
            Namn
            <input name="name" autoComplete="name" value={name} onChange={(e) => setName(e.target.value)} />
          </label>
        )}
        <label>
          E-post
          <input type="email" name="email" autoComplete="username" autoFocus required
            value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Lösenord
          <input type="password" name="password" required minLength={registering ? 8 : undefined}
            autoComplete={registering ? 'new-password' : 'current-password'} placeholder={registering ? 'minst 8 tecken' : undefined}
            value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>
        {registering && inviteRequired && (
          <label>
            Inbjudningskod
            <input name="invite" autoComplete="off" required value={inviteCode} onChange={(e) => setInviteCode(e.target.value)} />
          </label>
        )}
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy || !email || !password}>
          {registering ? 'Skapa konto' : 'Logga in'}
        </button>
        <p className="muted">
          {registering ? 'Har du redan ett konto? ' : 'Ny här? '}
          <button type="button" className="link" onClick={() => setMode(registering ? 'login' : 'register')}>
            {registering ? 'Logga in' : 'Skapa konto'}
          </button>
        </p>
      </form>
    </main>
  )
}
