import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { IconUsers, IconX } from '@tabler/icons-react'
import { getInvitePreview, acceptInvite } from '../../api/invites'
import { useUiStore } from '../../store/uiStore'
import { useT } from '../../i18n'

/**
 * Shown when the app is opened via a share link (#invite=<token>) while signed
 * in. Previews the space, then lets the user join (or open it if already a member).
 */
export default function InviteAcceptSheet({ token }: { token: string }) {
  const qc = useQueryClient()
  const t = useT()
  const { goSpace, setPendingInvite } = useUiStore()

  const { data: preview, isLoading, isError } = useQuery({
    queryKey: ['invite-preview', token],
    queryFn: () => getInvitePreview(token),
    retry: false,
  })

  const acceptMut = useMutation({
    mutationFn: () => acceptInvite(token),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: ['spaces'] })
      close()
      goSpace(result.spaceId)
    },
  })

  function close() {
    setPendingInvite(null)
    // Drop the token from the URL so a refresh doesn't re-open this.
    history.replaceState(null, '', window.location.pathname + window.location.search)
  }

  return (
    <div
      style={{
        position: 'fixed', inset: 0, zIndex: 70,
        background: 'rgba(16,18,22,0.34)', backdropFilter: 'blur(3px)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16,
      }}
      onClick={close}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: 'var(--surface)', maxWidth: 400, width: '100%',
          borderRadius: 'var(--r-xl)', boxShadow: 'var(--shadow-modal)', padding: 24,
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 14 }}>
          <div
            className="icon-tile icon-tile-accent-tint"
            style={{ width: 44, height: 44, borderRadius: 'var(--r-md)' }}
          >
            <IconUsers size={22} />
          </div>
          <button onClick={close} aria-label={t('invite.cancel')} style={{ color: 'var(--text-4)', display: 'flex' }}>
            <IconX size={18} />
          </button>
        </div>

        <div style={{ fontSize: 18, fontWeight: 600, marginBottom: 6 }}>{t('invite.joinTitle')}</div>

        {isLoading ? (
          <div style={{ fontSize: 14, color: 'var(--text-3)' }}>{t('common.loading')}</div>
        ) : isError || !preview ? (
          <>
            <div style={{ fontSize: 14, color: 'var(--text-2)', marginBottom: 20 }}>
              {t('invite.invalid')}
            </div>
            <button className="btn btn-outline" style={{ width: '100%' }} onClick={close}>
              {t('invite.cancel')}
            </button>
          </>
        ) : (
          <>
            <div style={{ fontSize: 14, color: 'var(--text-2)', marginBottom: 4 }}>
              {t('invite.joinBody', { name: preview.spaceName })}
            </div>
            {preview.alreadyMember && (
              <div style={{ fontSize: 13, color: 'var(--text-4)', marginBottom: 16 }}>
                {t('invite.alreadyMember')}
              </div>
            )}
            <div style={{ display: 'flex', gap: 10, marginTop: 18 }}>
              <button className="btn btn-outline" style={{ flex: 1 }} onClick={close}>
                {t('invite.cancel')}
              </button>
              <button
                className="btn btn-accent"
                style={{ flex: 1 }}
                onClick={() => acceptMut.mutate()}
                disabled={acceptMut.isPending}
              >
                {preview.alreadyMember ? t('invite.open') : t('invite.join')}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
