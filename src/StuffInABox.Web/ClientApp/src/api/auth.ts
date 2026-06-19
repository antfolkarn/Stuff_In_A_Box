import { api } from './client'

export async function register(email: string, password: string): Promise<string> {
  const res = await api.post<{ token: string }>('/auth/register', { email, password })
  return res.data.token
}

export async function login(email: string, password: string): Promise<string> {
  const res = await api.post<{ token: string }>('/auth/login', { email, password })
  return res.data.token
}

// Restores a session from the HttpOnly refresh cookie on app load. Returns a
// fresh access token, or null if there is no valid refresh session.
export async function refresh(): Promise<string | null> {
  try {
    const res = await api.post<{ token: string }>('/auth/refresh')
    return res.data.token
  } catch {
    return null
  }
}

export async function logout(): Promise<void> {
  try {
    await api.post('/auth/logout')
  } catch {
    // ignore — we clear local state regardless
  }
}
