import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { IconPlus, IconPrinter, IconX, IconBox, IconUsers } from '@tabler/icons-react'
import { getSpaces, createSpace } from '../../api/spaces'
import { useUiStore } from '../../store/uiStore'
import { Icon } from '../../shared/components/Icon'
import SpaceIconPicker from '../../shared/components/SpaceIconPicker'
import { useT } from '../../i18n'
import type { SpaceDto } from '../../api/types'

export default function HomeView() {
  const qc = useQueryClient()
  const { goSpace, goLabels } = useUiStore()
  const t = useT()
  const [addingSpace, setAddingSpace] = useState(false)
  const [newName, setNewName] = useState('')
  const [newIcon, setNewIcon] = useState('ti-box')

  const { data: spaces = [], isLoading } = useQuery({
    queryKey: ['spaces'],
    queryFn: getSpaces,
  })

  const createMut = useMutation({
    mutationFn: () => createSpace(newName.trim(), newIcon),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['spaces'] })
      setAddingSpace(false)
      setNewName('')
      setNewIcon('ti-box')
    },
  })

  const totalBoxes = spaces.reduce((s, sp) => s + sp.boxCount, 0)
  const totalItems = spaces.reduce((s, sp) => s + sp.itemCount, 0)

  return (
    <div>
      {/* Page header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 12,
          marginBottom: 24,
        }}
      >
        <div>
          <h1 style={{ fontSize: 24, fontWeight: 600, letterSpacing: '-0.02em', margin: 0 }}>
            {t('home.title')}
          </h1>
          <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 4 }}>
            {t('home.summary', { spaces: spaces.length, boxes: totalBoxes, items: totalItems })}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            className="btn btn-outline btn-sm"
            onClick={() => goLabels()}
          >
            <IconPrinter size={16} />
            {t('home.labels')}
          </button>
          <button
            className="btn btn-outline btn-sm"
            onClick={() => setAddingSpace((v) => !v)}
          >
            <IconPlus size={16} />
            {t('home.newSpace')}
          </button>
        </div>
      </div>

      {/* Add space form */}
      {addingSpace && (
        <div
          style={{
            background: 'var(--surface)',
            border: `1.5px solid var(--accent)`,
            borderRadius: 'var(--r-lg)',
            padding: 14,
            marginBottom: 20,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 14 }}>
            <div
              className="icon-tile icon-tile-accent-tint"
              style={{ width: 38, height: 38, borderRadius: 'var(--r-sm)', flexShrink: 0 }}
            >
              <Icon name={newIcon} size={20} color="var(--accent)" />
            </div>
            <input
              className="input"
              style={{ flex: 1 }}
              placeholder={t('home.namePlaceholder')}
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && newName.trim()) createMut.mutate()
              }}
              autoFocus
            />
            <button
              className="btn btn-accent"
              onClick={() => createMut.mutate()}
              disabled={!newName.trim() || createMut.isPending}
            >
              {t('home.save')}
            </button>
            <button
              style={{ color: 'var(--text-3)', display: 'flex' }}
              onClick={() => setAddingSpace(false)}
            >
              <IconX size={18} />
            </button>
          </div>
          <div style={{ borderTop: 'var(--bw) solid var(--border)', paddingTop: 12 }}>
            <SpaceIconPicker value={newIcon} onChange={setNewIcon} label={t('home.chooseIcon')} />
          </div>
        </div>
      )}

      {/* Space grid */}
      {isLoading ? (
        <div style={{ color: 'var(--text-3)', padding: '40px 0', textAlign: 'center' }}>
          {t('common.loading')}
        </div>
      ) : (
        <div
          className="space-grid"
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(252px, 1fr))',
            gap: 14,
          }}
        >
          {spaces.map((space) => (
            <SpaceCard
              key={space.id}
              space={space}
              onClick={() => goSpace(space.id)}
            />
          ))}
        </div>
      )}

      {!isLoading && spaces.length === 0 && !addingSpace && (
        <div style={{ textAlign: 'center', padding: '60px 0', color: 'var(--text-3)' }}>
          <IconBox size={40} style={{ display: 'block', margin: '0 auto 12px' }} />
          <div style={{ fontSize: 16, fontWeight: 500 }}>{t('home.emptyTitle')}</div>
          <div style={{ fontSize: 14, marginTop: 6 }}>
            {t('home.emptyBody')}
          </div>
        </div>
      )}
    </div>
  )
}

function SpaceCard({
  space,
  onClick,
}: {
  space: SpaceDto
  onClick: () => void
}) {
  const t = useT()
  return (
    <div
      className="card space-card"
      style={{ padding: 18, cursor: 'pointer', borderRadius: 'var(--r-lg)' }}
      onClick={onClick}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div
          className="icon-tile icon-tile-neutral space-card-icon"
          style={{ width: 42, height: 42, borderRadius: 'var(--r-md)', fontSize: 21 }}
        >
          <Icon name={space.icon} size={21} color="currentColor" />
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          {!space.isOwner && (
            <span
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 4,
                fontSize: 10.5,
                fontWeight: 600,
                color: 'var(--accent)',
                background: 'var(--accent-9)',
                padding: '3px 8px',
                borderRadius: 'var(--r-chip)',
              }}
            >
              <IconUsers size={12} />
              {t('home.sharedBadge')}
            </span>
          )}
          <span
            className="mono space-card-code"
            style={{
              fontSize: 11,
              letterSpacing: '0.08em',
              padding: '3px 8px',
              borderRadius: 'var(--r-sm)',
            }}
          >
            {space.code}
          </span>
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <div style={{ fontSize: 17, fontWeight: 600 }}>{space.name}</div>
        <div
          className="mono"
          style={{ fontSize: 12, color: 'var(--text-3)', marginTop: 3 }}
        >
          {t('space.boxesItems', { boxes: space.boxCount, items: space.itemCount })}
        </div>
      </div>
    </div>
  )
}
