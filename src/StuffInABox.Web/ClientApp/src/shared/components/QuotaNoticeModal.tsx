import { useUiStore } from '../../store/uiStore'
import { useT, type MessageKey } from '../../i18n'

// Shown when the server rejects an action for exceeding the plan's limit (quota_exceeded).
// Offers a one-tap route to the subscription block in Settings.
export default function QuotaNoticeModal() {
  const notice = useUiStore((s) => s.quotaNotice)
  const dismiss = useUiStore((s) => s.dismissQuotaNotice)
  const goSettings = useUiStore((s) => s.goSettings)
  const t = useT()
  if (!notice) return null

  const key = `quota.${notice.quota}` as MessageKey
  const specific = t(key, { limit: notice.limit })
  const body = specific === key ? t('quota.generic', { limit: notice.limit }) : specific

  return (
    <div
      onClick={dismiss}
      style={{
        position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', zIndex: 1000,
        display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16,
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: 'var(--surface)', borderRadius: 14, border: 'var(--bw) solid var(--border)',
          padding: 24, maxWidth: 380, width: '100%',
        }}
      >
        <div style={{ fontSize: 17, fontWeight: 600, marginBottom: 8 }}>{t('quota.title')}</div>
        <div style={{ fontSize: 14, color: 'var(--text-2)', marginBottom: 20, lineHeight: 1.5 }}>{body}</div>
        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button
            className="btn btn-sm"
            onClick={dismiss}
            style={{ background: 'var(--surface)', border: '1.5px solid var(--border-2)' }}
          >
            {t('quota.dismiss')}
          </button>
          <button
            className="btn btn-sm"
            onClick={() => { dismiss(); goSettings() }}
            style={{ background: 'var(--accent)', color: '#fff', border: 'none' }}
          >
            {t('subscription.upgrade')}
          </button>
        </div>
      </div>
    </div>
  )
}
