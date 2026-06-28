import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { IconMailExclamation, IconCheck } from '@tabler/icons-react'
import { resendVerification } from '../../api/auth'
import { useVerification } from './useVerification'
import { useT } from '../../i18n'

/** Shown to email accounts that haven't verified yet. OAuth accounts never see it. */
export default function VerifyEmailBanner() {
  const t = useT()
  const { needsVerification, me } = useVerification()
  const [sent, setSent] = useState(false)

  const resend = useMutation({
    mutationFn: resendVerification,
    onSuccess: () => setSent(true),
  })

  if (!needsVerification) return null

  return (
    <div
      role="status"
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        flexWrap: 'wrap',
        marginBottom: 20,
        padding: '12px 16px',
        borderRadius: 'var(--r-md)',
        background: 'color-mix(in srgb, #E8A23D 16%, var(--surface))',
        border: 'var(--bw) solid color-mix(in srgb, #E8A23D 45%, var(--border))',
        fontSize: 13.5,
      }}
    >
      <IconMailExclamation size={18} style={{ color: '#B9791C', flexShrink: 0 }} />
      <span style={{ flex: 1, minWidth: 200 }}>
        {t('verify.bannerText', { email: me?.email ?? '' })}
      </span>
      {sent ? (
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, color: 'var(--success-text)', fontWeight: 500 }}>
          <IconCheck size={15} />
          {t('verify.bannerSent')}
        </span>
      ) : (
        <button
          className="btn btn-outline btn-sm"
          onClick={() => resend.mutate()}
          disabled={resend.isPending}
          style={{ flexShrink: 0 }}
        >
          {resend.isPending ? t('login.waiting') : t('verify.resend')}
        </button>
      )}
    </div>
  )
}
