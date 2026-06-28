import { useEffect, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { IconStack2Filled, IconCheck, IconX } from '@tabler/icons-react'
import { useUiStore } from '../../store/uiStore'
import { verifyEmail } from '../../api/auth'
import { useT } from '../../i18n'

/** Full-screen view reached from a verification email (#verify=<token>). */
export default function VerifyEmailView({ token }: { token: string }) {
  const t = useT()
  const qc = useQueryClient()
  const { setPendingVerify } = useUiStore()
  const [status, setStatus] = useState<'verifying' | 'done' | 'error'>('verifying')

  useEffect(() => {
    let active = true
    verifyEmail(token)
      .then(() => {
        if (!active) return
        setStatus('done')
        // Clear the banner/gating for an already-signed-in session.
        qc.invalidateQueries({ queryKey: ['me'] })
      })
      .catch(() => active && setStatus('error'))
    return () => { active = false }
  }, [token, qc])

  function close() {
    setPendingVerify(null)
    history.replaceState(null, '', window.location.pathname + window.location.search)
  }

  return (
    <div
      style={{
        minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: 24, background: 'var(--bg)',
      }}
    >
      <div style={{ width: '100%', maxWidth: 392 }}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10, marginBottom: 28 }}>
          <div className="icon-tile icon-tile-accent" style={{ width: 48, height: 48, borderRadius: 'var(--r-lg)' }}>
            <IconStack2Filled size={26} color="#fff" />
          </div>
          <div style={{ fontSize: 19, fontWeight: 600 }}>StuffInABox</div>
        </div>

        <div
          style={{
            background: 'var(--surface)', border: 'var(--bw) solid var(--border)',
            borderRadius: 'var(--r-xl)', boxShadow: 'var(--shadow-card)', padding: 28, textAlign: 'center',
          }}
        >
          {status === 'verifying' && (
            <div style={{ fontSize: 15, color: 'var(--text-2)' }}>{t('verify.verifying')}</div>
          )}

          {status === 'done' && (
            <>
              <div className="icon-tile" style={{ width: 44, height: 44, borderRadius: '50%', background: 'var(--success-text)', margin: '0 auto 14px' }}>
                <IconCheck size={24} color="#fff" />
              </div>
              <div style={{ fontSize: 15, fontWeight: 500, marginBottom: 18 }}>{t('verify.success')}</div>
              <button className="btn btn-accent" style={{ width: '100%' }} onClick={close}>
                {t('verify.continue')}
              </button>
            </>
          )}

          {status === 'error' && (
            <>
              <div className="icon-tile" style={{ width: 44, height: 44, borderRadius: '50%', background: 'var(--text-4)', margin: '0 auto 14px' }}>
                <IconX size={24} color="#fff" />
              </div>
              <div style={{ fontSize: 15, fontWeight: 500, marginBottom: 6 }}>{t('verify.failed')}</div>
              <div style={{ fontSize: 13.5, color: 'var(--text-2)', marginBottom: 18 }}>{t('verify.failedHint')}</div>
              <button className="btn btn-outline" style={{ width: '100%' }} onClick={close}>
                {t('verify.continue')}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
