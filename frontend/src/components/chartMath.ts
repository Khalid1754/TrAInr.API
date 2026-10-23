// Ren matematik för diagrammen, utan React, så att den går att testa.

export const PAD = { top: 16, right: 16, bottom: 28, left: 44 }

// Runda skalsteg (0, 250, 500 …) så att axeln blir lätt att läsa.
export function niceTicks(min: number, max: number, count = 4) {
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

// Visar bara var n:te etikett när de inte får plats (ungefär en per 56 pixlar).
export function showLabel(i: number, total: number, width: number) {
  const fits = Math.max(2, Math.floor((width - PAD.left - PAD.right) / 56))
  if (total <= fits) return true
  const every = Math.ceil(total / fits)
  return (total - 1 - i) % every === 0
}
