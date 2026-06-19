import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'

export const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true, // send/receive the HttpOnly refresh cookie
})

// Inject JWT on every request
api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('sib_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Single in-flight refresh shared across concurrent 401s
let refreshPromise: Promise<string | null> | null = null

function doRefresh(): Promise<string | null> {
  if (!refreshPromise) {
    refreshPromise = axios
      .post<{ token: string }>('/api/auth/refresh', null, { withCredentials: true })
      .then((r) => {
        sessionStorage.setItem('sib_token', r.data.token)
        return r.data.token
      })
      .catch(() => null)
      .finally(() => { refreshPromise = null })
  }
  return refreshPromise
}

// On 401, try a one-time refresh then retry the original request
api.interceptors.response.use(
  (res) => res,
  async (err: AxiosError) => {
    const original = err.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined
    const url = original?.url ?? ''
    const isAuthCall = url.includes('/auth/refresh') || url.includes('/auth/login') || url.includes('/auth/register')

    if (err.response?.status === 401 && original && !original._retried && !isAuthCall) {
      original._retried = true
      const token = await doRefresh()
      if (token) {
        original.headers.Authorization = `Bearer ${token}`
        return api(original)
      }
      // Refresh failed — drop session and reload to the login gate
      sessionStorage.removeItem('sib_token')
      window.location.reload()
    }
    return Promise.reject(err)
  },
)
