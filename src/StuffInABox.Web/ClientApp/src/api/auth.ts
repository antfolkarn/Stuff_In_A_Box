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

// Always resolves (the server returns 200 regardless, to avoid leaking which
// addresses are registered).
export async function forgotPassword(email: string): Promise<void> {
  await api.post('/auth/forgot-password', { email })
}

export async function resetPassword(token: string, password: string): Promise<void> {
  await api.post('/auth/reset-password', { token, password })
}

export interface MeDto {
  provider: string
  email: string | null
  emailVerified: boolean
}

export async function getMe(): Promise<MeDto> {
  const res = await api.get<MeDto>('/auth/me')
  return res.data
}

// Confirms an email address from the #verify=<token> deep link.
export async function verifyEmail(token: string): Promise<void> {
  await api.post('/auth/verify-email', { token })
}

// Re-sends the verification email to the signed-in user. Always resolves.
export async function resendVerification(): Promise<void> {
  await api.post('/auth/resend-verification')
}
