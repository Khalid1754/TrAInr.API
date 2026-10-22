import { useEffect, useState } from 'react'
import { api } from './api'
import LogWorkout from './components/LogWorkout'
import History from './components/History'
import Plan from './components/Plan'
import ProfileForm from './components/ProfileForm'

const tabs = [
  { id: 'log', label: 'Logga pass' },
  { id: 'history', label: 'Historik' },
  { id: 'plan', label: 'AI-schema' },
  { id: 'profile', label: 'Profil' },
] as const

type Tab = (typeof tabs)[number]['id']

export default function App() {
  const [tab, setTab] = useState<Tab>('log')
  const [aiEnabled, setAiEnabled] = useState(true)

  useEffect(() => {
    api.status().then((s) => setAiEnabled(s.ai)).catch(() => setAiEnabled(false))
  }, [])

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
        </nav>
      </header>
      {!aiEnabled && (
        <div className="banner">
          AI är avstängt. Sätt <code>GROQ_API_KEY</code> eller <code>GEMINI_API_KEY</code> (gratis) eller <code>ANTHROPIC_API_KEY</code> och starta om backenden för att använda AI-funktionerna.
          Loggning och grundläggande kaloriuppskattning fungerar ändå.
        </div>
      )}
      <main>
        {tab === 'log' && <LogWorkout onSaved={() => setTab('history')} />}
        {tab === 'history' && <History />}
        {tab === 'plan' && <Plan />}
        {tab === 'profile' && <ProfileForm />}
      </main>
    </>
  )
}
