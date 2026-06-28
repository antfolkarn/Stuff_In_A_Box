import { create } from 'zustand'
import { getSettings, updateSettings } from '../api/settings'
import { useAuthStore } from './authStore'

export type Theme = 'light' | 'dark' | 'system'
export type Design = 'standard' | 'atelier' | 'pop' | 'nord' | 'console' | 'ledger'

// Order only — labels/descriptions live in the i18n dictionary (design.* / theme.*).
export const DESIGNS: Design[] = ['standard', 'atelier', 'pop', 'nord', 'console', 'ledger']
export const THEMES: Theme[] = ['light', 'dark', 'system']

const THEME_KEY = 'sib_theme'
const DESIGN_KEY = 'sib_design'
const DISPLAYNAME_KEY = 'sib_displayname'

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

// Signed-out the app always shows the Pop design in light mode (a consistent, branded
// entry screen); only once signed in do we honour the user's saved preferences. Kept
// in sync with the pre-paint inline script in index.html.
const LOGGED_OUT_THEME: Theme = 'light'
const LOGGED_OUT_DESIGN: Design = 'pop'

/** Appearance to apply on load: the user's prefs when signed in, else Pop + light. */
export function initialAppearance(
  authed: boolean,
  theme: Theme,
  design: Design,
): { theme: Theme; design: Design } {
  return authed ? { theme, design } : { theme: LOGGED_OUT_THEME, design: LOGGED_OUT_DESIGN }
}

interface SettingsState {
  theme: Theme
  design: Design
  displayName: string   // '' = no nickname; the member list then falls back to email
  setTheme: (theme: Theme) => void
  setDesign: (design: Design) => void
  setDisplayName: (name: string) => void
  toggleTheme: () => void
  applyLoggedOut: () => void
  loadFromServer: () => Promise<void>
}

function readStored<T extends string>(key: string, allowed: readonly T[], fallback: T): T {
  const v = localStorage.getItem(key)
  return allowed.includes(v as T) ? (v as T) : fallback
}

export const useSettingsStore = create<SettingsState>((set, get) => {
  const authed = useAuthStore.getState().isAuthenticated
  const cachedTheme = readStored<Theme>(THEME_KEY, THEMES, 'system')
  const cachedDesign = readStored<Design>(DESIGN_KEY, DESIGNS, 'standard')
  const initialDisplayName = localStorage.getItem(DISPLAYNAME_KEY) ?? ''
  const init = initialAppearance(authed, cachedTheme, cachedDesign)
  applyToDom(init.theme, init.design)

  // Persist the current state to localStorage + the DB. Reads from get() so every
  // setter just calls set({...}) then persist().
  function persist() {
    const { theme, design, displayName } = get()
    applyToDom(theme, design)
    // Only signed-in changes are real preferences — cache + sync them. Signed-out the
    // appearance is forced (Pop/light), so we neither cache it nor push it to the
    // server (also avoids a 401→refresh→reload loop on the login screen).
    if (useAuthStore.getState().isAuthenticated) {
      localStorage.setItem(THEME_KEY, theme)
      localStorage.setItem(DESIGN_KEY, design)
      localStorage.setItem(DISPLAYNAME_KEY, displayName)
      updateSettings({ theme, design, displayName: displayName || null })
    }
  }

  return {
    theme: init.theme,
    design: init.design,
    displayName: initialDisplayName,

    setTheme: (theme) => {
      set({ theme })
      persist()
    },
    setDesign: (design) => {
      set({ design })
      persist()
    },
    setDisplayName: (name) => {
      set({ displayName: name.trim() })
      persist()
    },
    toggleTheme: () => {
      const next: Theme = resolveTheme(get().theme) === 'dark' ? 'light' : 'dark'
      set({ theme: next })
      persist()
    },

    // Revert to the forced signed-out appearance (e.g. on logout). Does not touch the
    // cached preferences — they're restored from the server on the next sign-in.
    applyLoggedOut: () => {
      set({ theme: LOGGED_OUT_THEME, design: LOGGED_OUT_DESIGN })
      applyToDom(LOGGED_OUT_THEME, LOGGED_OUT_DESIGN)
    },

    loadFromServer: async () => {
      const s = await getSettings()
      if (!s) return
      const theme = (THEMES as string[]).includes(s.theme) ? (s.theme as Theme) : 'system'
      const design = (DESIGNS as string[]).includes(s.design) ? (s.design as Design) : 'standard'
      const displayName = s.displayName ?? ''
      localStorage.setItem(THEME_KEY, theme)
      localStorage.setItem(DESIGN_KEY, design)
      localStorage.setItem(DISPLAYNAME_KEY, displayName)
      applyToDom(theme, design)
      set({ theme, design, displayName })
    },
  }
})
