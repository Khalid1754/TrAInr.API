import { useEffect, useState } from 'react'
import { api, setUnauthorizedHandler, type Me, type Workout } from './api'
import Login from './components/Login'
import LogWorkout from './components/LogWorkout'
import History from './components/History'
import Coach from './components/Coach'
import Stats from './components/Stats'
import ProfileForm from './components/ProfileForm'
import Preferences from './components/Preferences'
import { msg, useLang } from './i18n'

const tabs = [
  { id: 'log', label: msg('Logga pass') },
  { id: 'history', label: msg('Historik') },
  { id: 'stats', label: msg('Statistik') },
  { id: 'coach', label: msg('AI-coach') },
  { id: 'profile', label: msg('Profil') },
] as const

type Tab = (typeof tabs)[number]['id']

export default function App() {
  const { t } = useLang()
  const [tab, setTab] = useState<Tab>('log')
  const [template, setTemplate] = useState<Workout | null>(null)
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
          {tabs.map((tabItem) => (
            <button key={tabItem.id} className={tab === tabItem.id ? 'active' : ''} onClick={() => { setTemplate(null); setTab(tabItem.id) }}>
              {t(tabItem.label)}
            </button>
          ))}
          <button onClick={logout}>{t('Logga ut')}</button>
        </nav>
        <Preferences />
      </header>
      {dbError && (
        <div className="banner">
          {t('Appen når inte databasen:')} <code>{dbError}</code>
        </div>
      )}
      {!aiEnabled && (
        <div className="banner">
          {t('AI är avstängt. Sätt GROQ_API_KEY eller GEMINI_API_KEY (gratis) eller ANTHROPIC_API_KEY och starta om backenden för att använda AI-funktionerna. Loggning och grundläggande kaloriuppskattning fungerar ändå.')}
        </div>
      )}
      <main>
        {tab === 'log' && <LogWorkout key={template?.id ?? 'new'} template={template} aiEnabled={aiEnabled} onSaved={() => { setTemplate(null); setTab('history') }} />}
        {tab === 'history' && <History onRepeat={(w) => { setTemplate(w); setTab('log') }} />}
        {tab === 'stats' && <Stats />}
        {tab === 'coach' && <Coach aiEnabled={aiEnabled} />}
        {tab === 'profile' && <ProfileForm email={auth.email} />}
      </main>
    </>
  )
}
