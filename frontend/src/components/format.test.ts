import { describe, expect, it } from 'vitest'
import { niceTicks, showLabel } from './chartMath'
import { describeLast, weekNumber } from './format'

describe('weekNumber', () => {
  it('följer ISO-veckor som i svenska kalendrar', () => {
    expect(weekNumber('2026-10-19')).toBe(43)
    expect(weekNumber('2026-01-01')).toBe(1)
    expect(weekNumber('2027-01-01')).toBe(53) // fredag i vecka 53 året innan
  })
})

describe('niceTicks', () => {
  it('ger runda steg från noll', () => {
    expect(niceTicks(0, 8400)).toEqual([0, 2500, 5000, 7500, 10000])
  })
  it('klarar att alla värden är lika', () => {
    expect(niceTicks(0, 0).length).toBeGreaterThan(1)
  })
})

describe('showLabel', () => {
  it('visar alla etiketter när de får plats och alltid den sista', () => {
    expect([0, 1, 2].every((i) => showLabel(i, 3, 600))).toBe(true)
    expect(showLabel(11, 12, 300)).toBe(true)
    expect(Array.from({ length: 12 }, (_, i) => showLabel(i, 12, 300)).filter(Boolean).length).toBeLessThan(12)
  })
})

describe('describeLast', () => {
  const base = { name: 'x', lastDate: '2026-10-01', times: 1, sets: 3, reps: 8, weightKg: 0, durationMin: null }
  it('visar vikt, reps eller minuter', () => {
    expect(describeLast({ ...base, weightKg: 70 })).toBe('3 × 8 × 70 kg')
    expect(describeLast(base)).toBe('3 × 8')
    expect(describeLast({ ...base, durationMin: 20 })).toBe('20 min')
  })
})
