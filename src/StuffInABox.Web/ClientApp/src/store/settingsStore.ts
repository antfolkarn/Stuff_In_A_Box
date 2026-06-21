import { create } from 'zustand'
import { getSettings, updateSettings } from '../api/settings'
import { useAuthStore } from './authStore'

export type Theme = 'light' | 'dark' | 'system'
export type Design = 'standard' | 'atelier' | 'pop'

// Order only — labels/descriptions live in the i18n dictionary (design.* / theme.*).
export const DESIGNS: Design[] = ['standard', 'atelier', 'pop']
export const THEMES: Theme[] = ['light', 'dark', 'system']

const THEME_KEY = 'sib_theme'
const DESIGN_KEY = 'sib_design'

export function resolveTheme(theme: Theme): 'light' | 'dark' {
  if (theme === 'system') {
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  }
  return theme
}

function applyToDom(theme: Theme, design: Design) {
  document.documentElement.setAttribute('data-theme', resolveTheme(theme))
  document.documentElement.setAttribute('data-design', design)
}

interface SettingsState {
  theme: Theme
  design: Design
  setTheme: (theme: Theme) => void
  setDesign: (design: Design) => void
  toggleTheme: () => void
  loadFromServer: () => Promise<void>
}

function readStored<T extends string>(key: string, allowed: readonly T[], fallback: T): T {
  const v = localStorage.getItem(key)
  return allowed.includes(v as T) ? (v as T) : fallback
}

export const useSettingsStore = create<SettingsState>((set, get) => {
  const initialTheme = readStored<Theme>(THEME_KEY, ['light', 'dark', 'system'], 'system')
  const initialDesign = readStored<Design>(DESIGN_KEY, ['standard', 'atelier', 'pop'], 'standard')
  applyToDom(initialTheme, initialDesign)

  function persist(theme: Theme, design: Design) {
    localStorage.setItem(THEME_KEY, theme)
    localStorage.setItem(DESIGN_KEY, design)
    applyToDom(theme, design)
    // Sync to the DB so settings follow the user across devices — only when
    // signed in (avoids a 401→refresh→reload loop on the login screen).
    if (useAuthStore.getState().isAuthenticated) {
      updateSettings({ theme, design })
    }
  }

  return {
    theme: initialTheme,
    design: initialDesign,

    setTheme: (theme) => {
      set({ theme })
      persist(theme, get().design)
    },
    setDesign: (design) => {
      set({ design })
      persist(get().theme, design)
    },
    toggleTheme: () => {
      const next: Theme = resolveTheme(get().theme) === 'dark' ? 'light' : 'dark'
      set({ theme: next })
      persist(next, get().design)
    },

    loadFromServer: async () => {
      const s = await getSettings()
      if (!s) return
      const theme = (['light', 'dark', 'system'].includes(s.theme) ? s.theme : 'system') as Theme
      const design = (['standard', 'atelier', 'pop'].includes(s.design) ? s.design : 'standard') as Design
      localStorage.setItem(THEME_KEY, theme)
      localStorage.setItem(DESIGN_KEY, design)
      applyToDom(theme, design)
      set({ theme, design })
    },
  }
})
