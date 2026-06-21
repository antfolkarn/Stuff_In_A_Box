import { useState } from 'react'
import { IconSun, IconMoon, IconStack2Filled, IconBrandGoogle, IconBrandAppleFilled } from '@tabler/icons-react'
import { useAuthStore } from '../../store/authStore'
import { useUiStore } from '../../store/uiStore'
import { useSettingsStore, resolveTheme } from '../../store/settingsStore'
import { login, register } from '../../api/auth'

type Mode = 'login' | 'signup'

const OAUTH_ERRORS: Record<string, string> = {
  oauth_not_configured: 'Inloggning via leverantör är inte konfigurerad.',
  oauth_failed: 'Inloggningen avbröts.',
  oauth_state: 'Säkerhetskontrollen misslyckades. Försök igen.',
  oauth_exchange: 'Kunde inte verifiera inloggningen. Försök igen.',
}

function initialOAuthError(): string {
  const m = window.location.hash.match(/[#&]error=([^&]+)/)
  return m ? OAUTH_ERRORS[m[1]] ?? 'Inloggningen misslyckades.' : ''
}

export default function LoginView() {
  const [mode, setMode] = useState<Mode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(initialOAuthError)
  const [loading, setLoading] = useState(false)
  const { setToken } = useAuthStore()
  const { pendingBox, goBox, setPendingBox } = useUiStore()
  const themeMode = useSettingsStore((s) => s.theme)
  const toggleTheme = useSettingsStore((s) => s.toggleTheme)
  const isDark = resolveTheme(themeMode) === 'dark'

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const token = mode === 'login'
        ? await login(email, password)
        : await register(email, password)
      setToken(token)
      if (pendingBox) {
        setPendingBox(null)
        goBox(pendingBox)
      }
    } catch {
      setError(
        mode === 'login'
          ? 'Fel e-post eller lösenord.'
          : 'Kunde inte skapa konto. Prova en annan e-post.',
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 24,
        background: 'var(--bg)',
        position: 'relative',
      }}
    >
      {/* Theme toggle (top-right) */}
      <button
        className="btn btn-outline"
        onClick={toggleTheme}
        title={isDark ? 'Ljust läge' : 'Mörkt läge'}
        aria-label="Växla färgtema"
        style={{ position: 'absolute', top: 20, right: 20, width: 42, padding: 0 }}
      >
        {isDark ? <IconSun size={18} /> : <IconMoon size={18} />}
      </button>

      <div style={{ width: '100%', maxWidth: 392 }}>
        {/* Brand lockup */}
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10, marginBottom: 28 }}>
          <div
            className="icon-tile icon-tile-accent"
            style={{ width: 48, height: 48, borderRadius: 'var(--r-lg)', fontSize: 26 }}
          >
            <IconStack2Filled size={26} color="#fff" />
          </div>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: 19, fontWeight: 600 }}>StuffInABox</div>
            <div
              className="mono"
              style={{
                fontSize: 10,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                color: 'var(--text-4)',
                marginTop: 2,
              }}
            >
              INDEX FÖR FYSISK FÖRVARING
            </div>
          </div>
        </div>

        {/* Card */}
        <div
          style={{
            background: 'var(--surface)',
            border: '1px solid rgba(20,24,30,0.10)',
            borderRadius: 'var(--r-xl)',
            boxShadow: '0 8px 30px rgba(20,24,30,0.07)',
            padding: 28,
          }}
        >
          <div style={{ marginBottom: 22 }}>
            <div style={{ fontSize: 20, fontWeight: 600 }}>
              {mode === 'login' ? 'Logga in' : 'Skapa konto'}
            </div>
            <div style={{ fontSize: 13.5, color: 'var(--text-2)', marginTop: 4 }}>
              {mode === 'login'
                ? 'Välkommen tillbaka till ditt register.'
                : 'Börja hålla reda på var allt finns.'}
            </div>
          </div>

          {/* OAuth buttons */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 20 }}>
            <button
              className="btn btn-outline"
              style={{ width: '100%', height: 46, borderRadius: 'var(--r-md)' }}
              onClick={() => { window.location.href = '/api/auth/google/start' }}
            >
              <IconBrandGoogle size={18} />
              Fortsätt med Google
            </button>
            <button
              style={{
                width: '100%',
                height: 46,
                borderRadius: 'var(--r-md)',
                background: 'var(--apple-bg)',
                color: '#fff',
                border: 'none',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 8,
                fontSize: 14,
                fontWeight: 500,
                cursor: 'pointer',
                fontFamily: 'inherit',
              }}
              onClick={() => { window.location.href = '/api/auth/apple/start' }}
            >
              <IconBrandAppleFilled size={18} />
              Fortsätt med Apple
            </button>
          </div>

          {/* Divider */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              marginBottom: 20,
            }}
          >
            <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
            <span style={{ fontSize: 12, color: 'var(--text-4)' }}>eller med e-post</span>
            <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
          </div>

          {/* Email/password form */}
          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: 14 }}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: 6,
                }}
              >
                <label style={{ fontSize: 13.5, fontWeight: 500 }}>E-post</label>
              </div>
              <input
                className="input"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="din@epost.se"
                required
              />
            </div>

            <div style={{ marginBottom: 20 }}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: 6,
                }}
              >
                <label style={{ fontSize: 13.5, fontWeight: 500 }}>Lösenord</label>
                {mode === 'login' && (
                  <a href="#" style={{ fontSize: 12.5 }}>
                    Glömt?
                  </a>
                )}
              </div>
              <input
                className="input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                minLength={6}
              />
            </div>

            {error && (
              <div
                style={{
                  marginBottom: 14,
                  padding: '10px 14px',
                  background: '#FEF2F2',
                  border: '1px solid #FECACA',
                  borderRadius: 'var(--r-sm)',
                  fontSize: 13.5,
                  color: '#991B1B',
                }}
              >
                {error}
              </div>
            )}

            <button
              type="submit"
              className="btn btn-accent"
              disabled={loading}
              style={{ width: '100%', height: 48, borderRadius: 'var(--r-md)', fontSize: 15.5 }}
            >
              {loading ? 'Vänta…' : mode === 'login' ? 'Logga in' : 'Skapa konto'}
            </button>
          </form>

          {/* Mode switch */}
          <div style={{ marginTop: 18, fontSize: 13.5, textAlign: 'center', color: 'var(--text-2)' }}>
            {mode === 'login' ? (
              <>
                Har du inget konto?{' '}
                <a href="#" onClick={(e) => { e.preventDefault(); setMode('signup'); setError('') }}>
                  Skapa konto
                </a>
              </>
            ) : (
              <>
                Har du redan ett konto?{' '}
                <a href="#" onClick={(e) => { e.preventDefault(); setMode('login'); setError('') }}>
                  Logga in
                </a>
              </>
            )}
          </div>
        </div>

        {/* Legal */}
        <p
          style={{
            textAlign: 'center',
            fontSize: 11.5,
            color: 'var(--text-4)',
            marginTop: 16,
          }}
        >
          Genom att fortsätta godkänner du våra villkor och vår integritetspolicy.
        </p>
      </div>
    </div>
  )
}
