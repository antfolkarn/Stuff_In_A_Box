import { useState } from 'react'
import { IconSun, IconMoon, IconStack2Filled, IconBrandGoogle, IconBrandAppleFilled } from '@tabler/icons-react'
import { useAuthStore } from '../../store/authStore'
import { useUiStore } from '../../store/uiStore'
import { useSettingsStore, resolveTheme } from '../../store/settingsStore'
import { login, register, forgotPassword } from '../../api/auth'
import { useT, type MessageKey } from '../../i18n'

type Mode = 'login' | 'signup' | 'forgot'

// Maps the OAuth callback error code to a message key; resolved to text at render.
const OAUTH_ERRORS: Record<string, MessageKey> = {
  oauth_not_configured: 'login.oauthNotConfigured',
  oauth_failed: 'login.oauthFailed',
  oauth_state: 'login.oauthState',
  oauth_exchange: 'login.oauthExchange',
}

function initialOAuthError(): MessageKey | '' {
  const m = window.location.hash.match(/[#&]error=([^&]+)/)
  return m ? OAUTH_ERRORS[m[1]] ?? 'login.oauthGeneric' : ''
}

export default function LoginView() {
  const [mode, setMode] = useState<Mode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<MessageKey | ''>(initialOAuthError)
  const [loading, setLoading] = useState(false)
  const [forgotSent, setForgotSent] = useState(false)
  const { setToken } = useAuthStore()
  const { pendingBox, goBox, setPendingBox, setLegal } = useUiStore()
  const t = useT()
  const themeMode = useSettingsStore((s) => s.theme)
  const toggleTheme = useSettingsStore((s) => s.toggleTheme)
  const isDark = resolveTheme(themeMode) === 'dark'

  function switchMode(next: Mode) {
    setMode(next)
    setError('')
    setForgotSent(false)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      if (mode === 'forgot') {
        await forgotPassword(email)
        setForgotSent(true) // always succeeds; server never reveals if the address exists
        return
      }
      const token = mode === 'login'
        ? await login(email, password)
        : await register(email, password)
      setToken(token)
      if (pendingBox) {
        setPendingBox(null)
        goBox(pendingBox)
      }
    } catch {
      setError(mode === 'login' ? 'login.errLogin' : 'login.errSignup')
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
        title={isDark ? t('header.lightMode') : t('header.darkMode')}
        aria-label={t('header.toggleTheme')}
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
              {t('header.eyebrow')}
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
              {mode === 'login' ? t('login.loginTitle') : mode === 'signup' ? t('login.signupTitle') : t('login.forgotTitle')}
            </div>
            <div style={{ fontSize: 13.5, color: 'var(--text-2)', marginTop: 4 }}>
              {mode === 'login' ? t('login.loginSubtitle') : mode === 'signup' ? t('login.signupSubtitle') : t('login.forgotSubtitle')}
            </div>
          </div>

          {/* OAuth buttons (not for password reset) */}
          {mode !== 'forgot' && (
          <>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 20 }}>
            <button
              className="btn btn-outline"
              style={{ width: '100%', height: 46, borderRadius: 'var(--r-md)' }}
              onClick={() => { window.location.href = '/api/v1/auth/google/start' }}
            >
              <IconBrandGoogle size={18} />
              {t('login.continueGoogle')}
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
              onClick={() => { window.location.href = '/api/v1/auth/apple/start' }}
            >
              <IconBrandAppleFilled size={18} />
              {t('login.continueApple')}
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
            <span style={{ fontSize: 12, color: 'var(--text-4)' }}>{t('login.orEmail')}</span>
            <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
          </div>
          </>
          )}

          {/* Email/(password) form */}
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
                <label style={{ fontSize: 13.5, fontWeight: 500 }}>{t('login.email')}</label>
              </div>
              <input
                className="input"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t('login.emailPlaceholder')}
                required
              />
            </div>

            {mode !== 'forgot' && (
            <div style={{ marginBottom: 20 }}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: 6,
                }}
              >
                <label style={{ fontSize: 13.5, fontWeight: 500 }}>{t('login.password')}</label>
                {mode === 'login' && (
                  <a href="#" style={{ fontSize: 12.5 }} onClick={(e) => { e.preventDefault(); switchMode('forgot') }}>
                    {t('login.forgot')}
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
            )}

            {mode === 'forgot' && forgotSent && (
              <div
                style={{
                  marginBottom: 14,
                  padding: '10px 14px',
                  background: 'var(--success-bg)',
                  borderRadius: 'var(--r-sm)',
                  fontSize: 13.5,
                  color: 'var(--success-text)',
                }}
              >
                {t('login.forgotSent')}
              </div>
            )}

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
                {t(error)}
              </div>
            )}

            <button
              type="submit"
              className="btn btn-accent"
              disabled={loading}
              style={{ width: '100%', height: 48, borderRadius: 'var(--r-md)', fontSize: 15.5 }}
            >
              {loading
                ? t('login.waiting')
                : mode === 'login' ? t('login.loginTitle')
                : mode === 'signup' ? t('login.signupTitle')
                : t('login.sendResetLink')}
            </button>
          </form>

          {/* Mode switch */}
          <div style={{ marginTop: 18, fontSize: 13.5, textAlign: 'center', color: 'var(--text-2)' }}>
            {mode === 'login' ? (
              <>
                {t('login.noAccount')}{' '}
                <a href="#" onClick={(e) => { e.preventDefault(); switchMode('signup') }}>
                  {t('login.signupTitle')}
                </a>
              </>
            ) : mode === 'signup' ? (
              <>
                {t('login.haveAccount')}{' '}
                <a href="#" onClick={(e) => { e.preventDefault(); switchMode('login') }}>
                  {t('login.loginTitle')}
                </a>
              </>
            ) : (
              <a href="#" onClick={(e) => { e.preventDefault(); switchMode('login') }}>
                {t('login.backToLogin')}
              </a>
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
          {t('login.legalBefore')}
          <a href="#" onClick={(e) => { e.preventDefault(); setLegal('terms') }}>{t('legal.terms')}</a>
          {t('login.legalBetween')}
          <a href="#" onClick={(e) => { e.preventDefault(); setLegal('privacy') }}>{t('legal.privacy')}</a>
          {t('login.legalAfter')}
        </p>
      </div>
    </div>
  )
}
