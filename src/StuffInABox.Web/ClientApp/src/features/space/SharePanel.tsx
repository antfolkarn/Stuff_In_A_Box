import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { IconCopy, IconCheck, IconTrash, IconUsers, IconLink } from '@tabler/icons-react'
import {
  getActiveInvite, createInvite, revokeInvite, getMembers, removeMember,
} from '../../api/invites'
import { useT } from '../../i18n'

function inviteUrl(token: string) {
  return `${window.location.origin}/#invite=${token}`
}

/** Owner-only panel: manage the share link and the list of members. */
export default function SharePanel({ spaceId }: { spaceId: string }) {
  const qc = useQueryClient()
  const t = useT()
  const [copied, setCopied] = useState(false)

  const { data: invite } = useQuery({
    queryKey: ['invite', spaceId],
    queryFn: () => getActiveInvite(spaceId),
  })

  const { data: members = [] } = useQuery({
    queryKey: ['members', spaceId],
    queryFn: () => getMembers(spaceId),
  })

  const createMut = useMutation({
    mutationFn: () => createInvite(spaceId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['invite', spaceId] }),
  })

  const revokeMut = useMutation({
    mutationFn: () => revokeInvite(spaceId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['invite', spaceId] }),
  })

  const removeMut = useMutation({
    mutationFn: (userId: string) => removeMember(spaceId, userId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['members', spaceId] }),
  })

  async function copyLink() {
    if (!invite) return
    try {
      await navigator.clipboard.writeText(inviteUrl(invite.token))
      setCopied(true)
      setTimeout(() => setCopied(false), 1600)
    } catch {
      /* clipboard may be unavailable; the field is selectable as a fallback */
    }
  }

  return (
    <div
      style={{
        background: 'var(--surface)',
        border: 'var(--bw) solid var(--border)',
        borderRadius: 'var(--r-lg)',
        padding: 16,
        marginBottom: 24,
        marginTop: 16,
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
        <IconLink size={17} style={{ color: 'var(--accent)' }} />
        <span style={{ fontSize: 15, fontWeight: 600 }}>{t('space.shareTitle')}</span>
      </div>
      <div style={{ fontSize: 13, color: 'var(--text-3)', marginBottom: 14 }}>
        {t('space.shareDesc')}
      </div>

      {invite ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div className="stack-mobile" style={{ display: 'flex', gap: 8 }}>
            <input
              className="input"
              readOnly
              value={inviteUrl(invite.token)}
              onFocus={(e) => e.currentTarget.select()}
              style={{ flex: 1, fontSize: 13 }}
            />
            <button className="btn btn-accent" onClick={copyLink} style={{ flexShrink: 0 }}>
              {copied ? <IconCheck size={16} /> : <IconCopy size={16} />}
              {copied ? t('space.linkCopied') : t('space.copyLink')}
            </button>
          </div>
          <button
            className="btn btn-outline btn-sm"
            style={{ alignSelf: 'flex-start', color: 'var(--text-2)' }}
            onClick={() => {
              if (window.confirm(t('space.confirmRevoke'))) revokeMut.mutate()
            }}
          >
            <IconTrash size={15} />
            {t('space.revokeLink')}
          </button>
        </div>
      ) : (
        <button
          className="btn btn-accent"
          onClick={() => createMut.mutate()}
          disabled={createMut.isPending}
        >
          <IconLink size={16} />
          {t('space.createLink')}
        </button>
      )}

      {/* Members */}
      <div style={{ borderTop: 'var(--bw) solid var(--border)', marginTop: 16, paddingTop: 14 }}>
        <div className="field-label" style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 10 }}>
          <IconUsers size={13} />
          {t('space.members')}
        </div>
        {members.length === 0 ? (
          <div style={{ fontSize: 13, color: 'var(--text-4)' }}>{t('space.noMembers')}</div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {members.map((m) => (
              <div
                key={m.userId}
                style={{ display: 'flex', alignItems: 'center', gap: 10 }}
              >
                <div
                  className="icon-tile icon-tile-accent-tint"
                  style={{ width: 32, height: 32, borderRadius: 'var(--r-sm)', fontSize: 13, fontWeight: 600 }}
                >
                  {m.userId.slice(0, 2).toUpperCase()}
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontSize: 13.5, fontWeight: 500 }}>{t('space.member')}</div>
                  <div className="mono" style={{ fontSize: 11, color: 'var(--text-4)' }}>
                    {m.userId.slice(0, 8)}
                  </div>
                </div>
                <button
                  onClick={() => {
                    if (window.confirm(t('space.confirmRemoveMember'))) removeMut.mutate(m.userId)
                  }}
                  title={t('space.removeMember')}
                  style={{ color: 'var(--text-4)', display: 'flex', padding: 4 }}
                >
                  <IconTrash size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
