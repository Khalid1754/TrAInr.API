import { useEffect, useState } from 'react'
import { api, setUnauthorizedHandler, type Me } from './api'
import Login from './components/Login'
import LogWorkout from './components/LogWorkout'
import History from './components/History'
import Plan from './components/Plan'
import Stats from './components/Stats'
import ProfileForm from './components/ProfileForm'

const tabs = [
  { id: 'log', label: 'Logga pass' },
  { id: 'history', label: 'Historik' },
  { id: 'stats', label: 'Statistik' },
  { id: 'plan', label: 'AI-schema' },
  { id: 'profile', label: 'Profil' },
] as const

type Tab = (typeof tabs)[number]['id']

export default function App() {
  const [tab, setTab] = useState<Tab>('log')
  const [aiEnabled, setAiEnabled] = useState(true)
  const [dbError, setDbError] = useState<string | null>(null)
  const [auth, setAuth] = useState<Me | null>(null)

  const loadMe = () =>
    api.me().then(setAuth).catch(() => setAuth({ loggedIn: false, email: null, inviteRequired: false }))

  useEffect(() => {
    setUnauthorizedHandler(() => setAuth((a) => ({ inviteRequired: false, ...a, loggedIn: false, email: null })))
    loadMe()
  }, [])

  useEffect(() => {
    if (auth?.loggedIn) api.status().then((s) => { setAiEnabled(s.ai); setDbError(s.database) }).catch(() => setAiEnabled(false))
  }, [auth?.loggedIn])

  async function logout() {
    await api.logout()
    setTab('log')
    await loadMe()
  }

  if (!auth) return null
  if (!auth.loggedIn) return <Login inviteRequired={auth.inviteRequired} onLoggedIn={loadMe} />

  return (
    <>
      <header>
        <h1>
          Tr<span>AI</span>nr
        </h1>
        <nav>
          {tabs.map((t) => (
            <button key={t.id} className={tab === t.id ? 'active' : ''} onClick={() => setTab(t.id)}>
              {t.label}
            </button>
          ))}
          <button onClick={logout}>Logga ut</button>
        </nav>
      </header>
      {dbError && (
        <div className="banner">
          Appen når inte databasen: <code>{dbError}</code>
        </div>
      )}
      {!aiEnabled && (
        <div className="banner">
          AI är avstängt. Sätt <code>GROQ_API_KEY</code> eller <code>GEMINI_API_KEY</code> (gratis) eller <code>ANTHROPIC_API_KEY</code> och starta om backenden för att använda AI-funktionerna.
          Loggning och grundläggande kaloriuppskattning fungerar ändå.
        </div>
      )}
      <main>
        {tab === 'log' && <LogWorkout onSaved={() => setTab('history')} />}
        {tab === 'history' && <History />}
        {tab === 'stats' && <Stats />}
        {tab === 'plan' && <Plan />}
        {tab === 'profile' && <ProfileForm email={auth.email} />}
      </main>
    </>
  )
}
