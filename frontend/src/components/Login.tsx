import { useState, type FormEvent } from 'react'
import { api } from '../api'
import { useAsync } from './useAsync'

export default function Login({ onLoggedIn }: { onLoggedIn: () => void }) {
  const [password, setPassword] = useState('')
  const { busy, error, run } = useAsync()

  async function submit(e: FormEvent) {
    e.preventDefault()
    const ok = await run(async () => {
      await api.login(password)
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
        {/* Användarnamnet syns inte men hjälper iPhone att spara lösenordet i nyckelringen. */}
        <input type="text" name="username" autoComplete="username" value="trainr" readOnly hidden />
        <label>
          Lösenord
          <input type="password" name="password" autoComplete="current-password" autoFocus
            value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy || !password}>Logga in</button>
      </form>
    </main>
  )
}
