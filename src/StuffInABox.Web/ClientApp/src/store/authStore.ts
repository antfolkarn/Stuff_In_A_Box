import { create } from 'zustand'
import { refresh as apiRefresh, logout as apiLogout } from '../api/auth'

interface AuthState {
  token: string | null
  isAuthenticated: boolean
  ready: boolean // false until the initial refresh-cookie check completes
  setToken: (token: string) => void
  bootstrap: () => Promise<void>
  logout: () => Promise<void>
}

export const useAuthStore = create<AuthState>((set, get) => ({
  token: sessionStorage.getItem('sib_token'),
  isAuthenticated: !!sessionStorage.getItem('sib_token'),
  ready: false,

  setToken: (token) => {
    sessionStorage.setItem('sib_token', token)
    set({ token, isAuthenticated: true })
  },

  bootstrap: async () => {
    // OAuth redirect lands back with the access token in the URL fragment
    const tokenMatch = window.location.hash.match(/[#&]token=([^&]+)/)
    if (tokenMatch) {
      const token = decodeURIComponent(tokenMatch[1])
      sessionStorage.setItem('sib_token', token)
      // Strip the token from the URL so it isn't left in history
      const cleaned = window.location.hash.replace(/([#&])token=[^&]+/, '$1')
      history.replaceState(null, '', window.location.pathname + window.location.search +
        (cleaned === '#' || cleaned === '#&' ? '' : cleaned))
      set({ token, isAuthenticated: true, ready: true })
      return
    }

    if (get().isAuthenticated) {
      set({ ready: true })
      return
    }
    // No access token in this tab — try to restore from the HttpOnly refresh cookie
    const token = await apiRefresh()
    if (token) {
      sessionStorage.setItem('sib_token', token)
      set({ token, isAuthenticated: true })
    }
    set({ ready: true })
  },

  logout: async () => {
    await apiLogout()
    sessionStorage.removeItem('sib_token')
    set({ token: null, isAuthenticated: false })
  },
}))
