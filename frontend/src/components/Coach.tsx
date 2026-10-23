import { useEffect, useRef, useState, type FormEvent } from 'react'
import { api, type ChatMessage } from '../api'
import Plan from './Plan'
import { msg, useLang } from '../i18n'
import { useAsync } from './useAsync'

const starters = [
  msg('Hur går det med min styrka?'),
  msg('Vad ska jag äta före passet?'),
  msg('Hur många vilodagar behöver jag?'),
  msg('Vilken muskelgrupp tränar jag för lite?'),
]

// Samtalet sparas bara i den här fliken i webbläsaren, så det finns kvar om man byter flik.
const STORAGE_KEY = 'trainr-chat'
function loadChat(): ChatMessage[] {
  try {
    return JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? '[]')
  } catch {
    return []
  }
}

export default function Coach({ aiEnabled }: { aiEnabled: boolean }) {
  const [messages, setMessages] = useState<ChatMessage[]>(loadChat)
  const [text, setText] = useState('')
  const { busy, error, run } = useAsync()
  const end = useRef<HTMLDivElement>(null)
  const { t } = useLang()

  useEffect(() => {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(messages.slice(-20)))
    } catch {
      // inte viktigt om det inte går att spara
    }
    end.current?.scrollIntoView({ block: 'nearest' })
  }, [messages, busy])

  async function send(question: string) {
    const q = question.trim()
    if (!q || busy) return
    const next: ChatMessage[] = [...messages, { role: 'user', text: q }]
    setMessages(next)
    setText('')
    const result = await run(() => api.chat(next.slice(-12)))
    if (result) setMessages([...next, { role: 'coach', text: result.reply }])
  }

  function submit(e: FormEvent) {
    e.preventDefault()
    send(text)
  }

  return (
    <>
      <section>
        <h2>{t('Fråga AI-coachen')}</h2>
        {!aiEnabled ? (
          <p className="muted">{t('AI är avstängt. Lägg in en AI-nyckel för att chatta med coachen.')}</p>
        ) : (
          <div className="card chat">
            <p className="muted">{t('Coachen ser din profil, dina senaste pass och dina personbästa.')}</p>
            <div className="messages">
              {messages.map((m, i) => (
                <div key={i} className={`message ${m.role}`}>{m.text}</div>
              ))}
              {busy && <div className="message coach muted">{t('Tänker…')}</div>}
              <div ref={end} />
            </div>
            {messages.length === 0 && (
              <div className="chips wrap">
                {starters.map((s) => <button key={s} type="button" className="chip" onClick={() => send(t(s))}>{t(s)}</button>)}
              </div>
            )}
            {error && <p className="error">{error}</p>}
            <form className="chat-input" onSubmit={submit}>
              <input value={text} onChange={(e) => setText(e.target.value)} placeholder={t('Skriv en fråga…')} maxLength={2000} />
              <button type="submit" disabled={busy || !text.trim()}>{t('Skicka')}</button>
            </form>
            {messages.length > 0 && (
              <button type="button" className="link" onClick={() => setMessages([])}>{t('Börja om')}</button>
            )}
          </div>
        )}
      </section>
      <Plan />
    </>
  )
}
