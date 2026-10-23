import { useEffect, useRef, useState } from 'react'
import { useLang } from '../i18n'

const presets = [60, 90, 120, 180]
const clock = () => Date.now()
const format = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`

function loadPreset() {
  try {
    return Number(localStorage.getItem('trainr-rest')) || 90
  } catch {
    return 90
  }
}

// Ett kort pip när vilan är slut. Fungerar även på iPhone, där vibration saknas.
function beep() {
  try {
    const ctx = new AudioContext()
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.frequency.value = 880
    gain.gain.setValueAtTime(0.2, ctx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.6)
    osc.connect(gain).connect(ctx.destination)
    osc.start()
    osc.stop(ctx.currentTime + 0.6)
    osc.onended = () => ctx.close()
  } catch {
    // ljud går inte att spela, timern syns ändå
  }
  navigator.vibrate?.([200, 100, 200])
}

// Vilotimer längst ner på skärmen. Räknar mot en sluttid, så den går rätt även om telefonen låses en stund.
export default function RestTimer() {
  const [preset, setPreset] = useState(loadPreset)
  const [endsAt, setEndsAt] = useState<number | null>(null)
  const [now, setNow] = useState(clock)
  const [done, setDone] = useState(false)
  const beeped = useRef(false)
  const { t } = useLang()

  const left = endsAt === null ? preset : Math.max(0, Math.ceil((endsAt - now) / 1000))

  useEffect(() => {
    if (endsAt === null) return
    const id = setInterval(() => setNow(clock()), 250)
    return () => clearInterval(id)
  }, [endsAt])

  useEffect(() => {
    if (endsAt !== null && left === 0 && !beeped.current) {
      beeped.current = true
      beep()
      setEndsAt(null)
      setDone(true)
    }
  }, [left, endsAt])

  function start(seconds = preset) {
    beeped.current = false
    setDone(false)
    const t = clock()
    setNow(t)
    setEndsAt(t + seconds * 1000)
  }

  function choose(seconds: number) {
    setPreset(seconds)
    try {
      localStorage.setItem('trainr-rest', String(seconds))
    } catch {
      // inte viktigt om det inte går att spara
    }
    start(seconds)
  }

  const running = endsAt !== null
  return (
    <div className={`rest-timer ${done ? 'done' : ''}`} role="timer" aria-live="polite">
      <div className="rest-time">
        <span className="muted">{done ? t('Vilan är slut!') : t('Vila')}</span>
        <b>{format(running ? left : preset)}</b>
      </div>
      {running ? (
        <div className="rest-buttons">
          <button type="button" className="secondary" onClick={() => setEndsAt((e) => (e ?? clock()) + 15000)}>+15 s</button>
          <button type="button" className="danger" onClick={() => setEndsAt(null)}>{t('Stoppa')}</button>
        </div>
      ) : (
        <div className="rest-buttons">
          {presets.map((s) => (
            <button key={s} type="button" className={s === preset ? 'chip active' : 'chip'} onClick={() => choose(s)}>{format(s)}</button>
          ))}
          <button type="button" onClick={() => start()}>{t('Starta')}</button>
        </div>
      )}
    </div>
  )
}
