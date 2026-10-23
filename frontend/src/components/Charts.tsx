import { useLayoutEffect, useRef, useState, type ReactNode, type RefObject } from 'react'

// Små SVG-diagram utan extra bibliotek. En serie per diagram, så rubriken räcker som förklaring.
// Håll fingret eller musen över en stapel eller punkt för att se värdet.

export interface ChartPoint {
  label: string // text under x-axeln
  value: number
  tooltip: ReactNode
}

const H = 200
const PAD = { top: 16, right: 16, bottom: 28, left: 44 }
const innerH = H - PAD.top - PAD.bottom

// Diagrammet ritas i skärmens verkliga bredd så att texten alltid är lika stor, även på mobilen.
function useWidth() {
  const ref = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(600)
  useLayoutEffect(() => {
    const el = ref.current
    if (!el) return
    const observer = new ResizeObserver(([entry]) => setWidth(Math.max(240, entry.contentRect.width)))
    observer.observe(el)
    return () => observer.disconnect()
  }, [])
  return { ref, width, innerW: width - PAD.left - PAD.right }
}

// Runda skalsteg (0, 250, 500 …) så att axeln blir lätt att läsa.
function niceTicks(min: number, max: number, count = 4) {
  if (max === min) max = min + 1
  const raw = (max - min) / count
  const mag = 10 ** Math.floor(Math.log10(raw))
  const step = [1, 2, 2.5, 5, 10].map((m) => m * mag).find((s) => s >= raw)!
  const lo = Math.floor(min / step) * step
  const hi = Math.ceil(max / step) * step
  const ticks = []
  for (let t = lo; t <= hi + step / 2; t += step) ticks.push(t)
  return ticks
}

const compact = (n: number) => (Math.abs(n) >= 1000 ? `${+(n / 1000).toFixed(1)}k` : `${+n.toFixed(1)}`)

// Visar bara var n:te etikett när de inte får plats (ungefär en per 56 pixlar).
function showLabel(i: number, total: number, width: number) {
  const fits = Math.max(2, Math.floor((width - PAD.left - PAD.right) / 56))
  if (total <= fits) return true
  const every = Math.ceil(total / fits)
  return (total - 1 - i) % every === 0
}

function Frame({ chartRef, width: W, ticks, y, points, x, children, active, label }: {
  chartRef: RefObject<HTMLDivElement | null>
  width: number
  ticks: number[]
  y: (v: number) => number
  points: ChartPoint[]
  x: (i: number) => number
  children: ReactNode
  active: number | null
  label: string
}) {
  return (
    <div className="chart" ref={chartRef}>
      <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} role="img" aria-label={label}>
        {ticks.map((t) => (
          <g key={t}>
            <line className="grid" x1={PAD.left} x2={W - PAD.right} y1={y(t)} y2={y(t)} />
            <text className="axis" x={PAD.left - 8} y={y(t)} dy="0.35em" textAnchor="end">{compact(t)}</text>
          </g>
        ))}
        {points.map((p, i) =>
          showLabel(i, points.length, W) ? (
            <text key={i} className="axis" x={x(i)} y={H - 8} textAnchor="middle">{p.label}</text>
          ) : null,
        )}
        {children}
      </svg>
      {active !== null && (
        <div className="chart-tip" style={{ left: `${Math.min(85, Math.max(15, (x(active) / W) * 100))}%` }}>{points[active].tooltip}</div>
      )}
    </div>
  )
}

export function BarChart({ points, label }: { points: ChartPoint[]; label: string }) {
  const { ref, width, innerW } = useWidth()
  const [active, setActive] = useState<number | null>(null)
  const ticks = niceTicks(0, Math.max(...points.map((p) => p.value), 0))
  const top = ticks[ticks.length - 1]
  const y = (v: number) => PAD.top + innerH - (v / top) * innerH
  const band = innerW / points.length
  const x = (i: number) => PAD.left + band * (i + 0.5)
  const barW = Math.min(24, band - 4)

  return (
    <Frame chartRef={ref} width={width} ticks={ticks} y={y} x={x} points={points} active={active} label={label}>
      {points.map((p, i) => {
        const h = y(0) - y(p.value)
        const r = Math.min(4, h)
        const left = x(i) - barW / 2
        return (
          <g key={i} onPointerEnter={() => setActive(i)} onPointerDown={() => setActive(i)} onPointerLeave={() => setActive(null)}>
            {/* Osynlig, bredare yta så att det är lätt att träffa med fingret. */}
            <rect x={PAD.left + band * i} y={PAD.top} width={band} height={innerH} fill="transparent" />
            {h > 0 && (
              <path className={`bar ${active === i ? 'active' : ''}`}
                d={`M${left},${y(0)} V${y(p.value) + r} Q${left},${y(p.value)} ${left + r},${y(p.value)} H${left + barW - r} Q${left + barW},${y(p.value)} ${left + barW},${y(p.value) + r} V${y(0)} Z`} />
            )}
          </g>
        )
      })}
    </Frame>
  )
}

export function LineChart({ points, label }: { points: ChartPoint[]; label: string }) {
  const { ref, width, innerW } = useWidth()
  const [active, setActive] = useState<number | null>(null)
  const values = points.map((p) => p.value)
  const ticks = niceTicks(Math.max(0, Math.min(...values) * 0.9), Math.max(...values))
  const lo = ticks[0]
  const hi = ticks[ticks.length - 1]
  const y = (v: number) => PAD.top + innerH - ((v - lo) / (hi - lo)) * innerH
  const step = points.length > 1 ? innerW / (points.length - 1) : 0
  const x = (i: number) => (points.length > 1 ? PAD.left + step * i : PAD.left + innerW / 2)
  const path = points.map((p, i) => `${i ? 'L' : 'M'}${x(i)},${y(p.value)}`).join(' ')
  const hitW = points.length > 1 ? step : innerW

  return (
    <Frame chartRef={ref} width={width} ticks={ticks} y={y} x={x} points={points} active={active} label={label}>
      <path className="line-area" d={`${path} L${x(points.length - 1)},${y(lo)} L${x(0)},${y(lo)} Z`} />
      <path className="line" d={path} />
      {active !== null && <line className="crosshair" x1={x(active)} x2={x(active)} y1={PAD.top} y2={y(lo)} />}
      {points.map((p, i) => (
        <g key={i} onPointerEnter={() => setActive(i)} onPointerDown={() => setActive(i)} onPointerLeave={() => setActive(null)}>
          <rect x={x(i) - hitW / 2} y={PAD.top} width={hitW} height={innerH} fill="transparent" />
          {(active === i || i === points.length - 1 || points.length <= 12) && (
            <circle className="dot" cx={x(i)} cy={y(p.value)} r={active === i ? 6 : 4} />
          )}
        </g>
      ))}
    </Frame>
  )
}
