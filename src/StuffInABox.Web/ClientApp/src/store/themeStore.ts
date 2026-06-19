import { create } from 'zustand'

type Theme = 'light' | 'dark'

const STORAGE_KEY = 'sib_theme'

function getInitialTheme(): Theme {
  const saved = localStorage.getItem(STORAGE_KEY)
  if (saved === 'light' || saved === 'dark') return saved
  // Fall back to the OS preference
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyTheme(theme: Theme) {
  document.documentElement.setAttribute('data-theme', theme)
}

interface ThemeState {
  theme: Theme
  toggle: () => void
  setTheme: (theme: Theme) => void
}

export const useThemeStore = create<ThemeState>((set, get) => {
  // Apply immediately so the first paint matches the chosen theme
  const initial = getInitialTheme()
  applyTheme(initial)

  return {
    theme: initial,
    toggle: () => get().setTheme(get().theme === 'dark' ? 'light' : 'dark'),
    setTheme: (theme) => {
      localStorage.setItem(STORAGE_KEY, theme)
      applyTheme(theme)
      set({ theme })
    },
  }
})
