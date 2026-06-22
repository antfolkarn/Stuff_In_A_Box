import { useState } from 'react'
import { IconSun, IconMoon, IconStack2Filled, IconCheck } from '@tabler/icons-react'
import { useUiStore } from '../../store/uiStore'
import { useSettingsStore, resolveTheme } from '../../store/settingsStore'
import { resetPassword } from '../../api/auth'
import { useT } from '../../i18n'

/** Full-screen view reached from a password-reset email (#reset=<token>). */
export default function ResetPasswordView({ token }: { token: string }) {
  const t = useT()
  const { setPendingReset } = useUiStore()
  const themeMode = useSettingsStore((s) => s.theme)
  const toggleTheme = useSettingsStore((s) => s.toggleTheme)
  const isDark = resolveTheme(themeMode) === 'dark'

  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState(false)

  function close() {
    setPendingReset(null)
    history.replaceState(null, '', window.location.pathname + window.location.search)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(false)
    setLoading(true)
    try {
      await resetPassword(token, password)
      setDone(true)
    } catch {
      setError(true) // invalid or expired token
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      style={{
        minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: 24, background: 'var(--bg)', position: 'relative',
      }}
    >
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
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10, marginBottom: 28 }}>
          <div className="icon-tile icon-tile-accent" style={{ width: 48, height: 48, borderRadius: 'var(--r-lg)' }}>
            <IconStack2Filled size={26} color="#fff" />
          </div>
          <div style={{ fontSize: 19, fontWeight: 600 }}>StuffInABox</div>
        </div>

        <div
          style={{
            background: 'var(--surface)', border: '1px solid rgba(20,24,30,0.10)',
            borderRadius: 'var(--r-xl)', boxShadow: '0 8px 30px rgba(20,24,30,0.07)', padding: 28,
          }}
        >
          {done ? (
            <div style={{ textAlign: 'center' }}>
              <div
                className="icon-tile"
                style={{ width: 44, height: 44, borderRadius: '50%', background: 'var(--success-text)', margin: '0 auto 14px' }}
              >
                <IconCheck size={24} color="#fff" />
              </div>
              <div style={{ fontSize: 15, fontWeight: 500, marginBottom: 18 }}>{t('reset.success')}</div>
              <button className="btn btn-accent" style={{ width: '100%' }} onClick={close}>
                {t('reset.toLogin')}
              </button>
            </div>
          ) : (
            <>
              <div style={{ marginBottom: 22 }}>
                <div style={{ fontSize: 20, fontWeight: 600 }}>{t('reset.title')}</div>
                <div style={{ fontSize: 13.5, color: 'var(--text-2)', marginTop: 4 }}>{t('reset.subtitle')}</div>
              </div>

              <form onSubmit={handleSubmit}>
                <div style={{ marginBottom: 20 }}>
                  <label style={{ fontSize: 13.5, fontWeight: 500, display: 'block', marginBottom: 6 }}>
                    {t('reset.newPassword')}
                  </label>
                  <input
                    className="input"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="••••••••"
                    required
                    minLength={6}
                    autoFocus
                  />
                </div>

                {error && (
                  <div
                    style={{
                      marginBottom: 14, padding: '10px 14px', background: '#FEF2F2',
                      border: '1px solid #FECACA', borderRadius: 'var(--r-sm)', fontSize: 13.5, color: '#991B1B',
                    }}
                  >
                    {t('reset.invalid')}
                  </div>
                )}

                <button
                  type="submit"
                  className="btn btn-accent"
                  disabled={loading}
                  style={{ width: '100%', height: 48, borderRadius: 'var(--r-md)', fontSize: 15.5 }}
                >
                  {loading ? t('login.waiting') : t('reset.submit')}
                </button>
              </form>

              <div style={{ marginTop: 18, textAlign: 'center', fontSize: 13.5 }}>
                <a href="#" onClick={(e) => { e.preventDefault(); close() }}>{t('login.backToLogin')}</a>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
