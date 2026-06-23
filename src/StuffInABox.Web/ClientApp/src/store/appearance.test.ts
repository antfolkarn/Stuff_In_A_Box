import { describe, it, expect } from 'vitest'
import { initialAppearance } from './settingsStore'

describe('initialAppearance', () => {
  it('forces Pop + light when signed out, ignoring any cached prefs', () => {
    expect(initialAppearance(false, 'dark', 'atelier')).toEqual({ theme: 'light', design: 'pop' })
    expect(initialAppearance(false, 'system', 'standard')).toEqual({ theme: 'light', design: 'pop' })
  })

  it('honours the cached preferences when signed in', () => {
    expect(initialAppearance(true, 'dark', 'atelier')).toEqual({ theme: 'dark', design: 'atelier' })
    expect(initialAppearance(true, 'system', 'standard')).toEqual({ theme: 'system', design: 'standard' })
  })
})
