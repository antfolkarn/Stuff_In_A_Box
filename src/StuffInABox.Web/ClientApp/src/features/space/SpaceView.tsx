import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { IconPlus, IconPrinter, IconPencil, IconArrowLeft, IconUsers, IconLogout } from '@tabler/icons-react'
import { getSpaces, updateSpaceIcon } from '../../api/spaces'
import { getBoxesBySpace, createBox } from '../../api/boxes'
import { leaveSpace } from '../../api/invites'
import { useUiStore } from '../../store/uiStore'
import SpaceIconPicker from '../../shared/components/SpaceIconPicker'
import SharePanel from './SharePanel'
import { Icon } from '../../shared/components/Icon'
import { useT } from '../../i18n'
import type { BoxDto } from '../../api/types'

export default function SpaceView() {
  const qc = useQueryClient()
  const { spaceId, goBox, goHome, goLabels } = useUiStore()
  const t = useT()
  const [editingIcon, setEditingIcon] = useState(false)
  const [showShare, setShowShare] = useState(false)
  const [newBoxLabel, setNewBoxLabel] = useState('')
  const [addingBox, setAddingBox] = useState(false)

  const { data: spaces = [] } = useQuery({ queryKey: ['spaces'], queryFn: getSpaces })
  const space = spaces.find((s) => s.id === spaceId)

  const { data: boxes = [], isLoading } = useQuery({
    queryKey: ['boxes', spaceId],
    queryFn: () => getBoxesBySpace(spaceId!),
    enabled: !!spaceId,
  })

  const iconMut = useMutation({
    mutationFn: (icon: string) => updateSpaceIcon(spaceId!, icon),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['spaces'] })
      setEditingIcon(false)
    },
  })

  const createBoxMut = useMutation({
    mutationFn: () => createBox(spaceId!, newBoxLabel.trim()),
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: ['boxes', spaceId] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
      setAddingBox(false)
      setNewBoxLabel('')
      goBox(result.boxNumber, spaceId!)
    },
  })

  const leaveMut = useMutation({
    mutationFn: () => leaveSpace(spaceId!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['spaces'] })
      goHome()
    },
  })

  const isOwner = space?.isOwner ?? false

  if (!space) {
    return (
      <div>
        <button className="btn btn-outline btn-sm" onClick={goHome}>
          ← {t('space.back')}
        </button>
        <p style={{ color: 'var(--text-3)' }}>{t('space.notFound')}</p>
      </div>
    )
  }

  return (
    <div>
      {/* Breadcrumb */}
      <button
        onClick={goHome}
        style={{
          fontSize: 13.5,
          color: 'var(--text-3)',
          display: 'flex',
          alignItems: 'center',
          gap: 5,
          marginBottom: 18,
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          padding: 0,
        }}
      >
        <IconArrowLeft size={15} />
        {t('space.back')}
      </button>

      {/* Header row */}
      <div
        style={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 12,
          marginBottom: editingIcon ? 0 : 24,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          {/* Editable icon button */}
          <div style={{ position: 'relative' }}>
            <button
              onClick={() => isOwner && setEditingIcon((v) => !v)}
              title={isOwner ? t('space.changeIcon') : undefined}
              style={{
                width: 54,
                height: 54,
                background: 'var(--surface)',
                border: 'var(--bw) solid var(--border)',
                borderRadius: 'var(--r-lg)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: isOwner ? 'pointer' : 'default',
                color: 'var(--text-2)',
              }}
            >
              <Icon name={space.icon} size={26} color="var(--text-2)" />
            </button>
            {isOwner && (
              <div
                style={{
                  position: 'absolute',
                  bottom: -4,
                  right: -4,
                  width: 21,
                  height: 21,
                  background: 'var(--accent)',
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <IconPencil size={11} color="#fff" />
              </div>
            )}
          </div>

          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <h1 style={{ fontSize: 24, fontWeight: 600, letterSpacing: '-0.02em', margin: 0 }}>
                {space.name}
              </h1>
              <span
                className="mono"
                style={{
                  fontSize: 11,
                  color: 'var(--text-4)',
                  background: 'var(--tile)',
                  padding: '3px 8px',
                  borderRadius: 'var(--r-sm)',
                }}
              >
                {space.code}
              </span>
            </div>
            <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 3 }}>
              {t('space.boxesItems', { boxes: space.boxCount, items: space.itemCount })}
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {isOwner ? (
            <button
              className="btn btn-outline btn-sm"
              onClick={() => setShowShare((v) => !v)}
            >
              <IconUsers size={16} />
              {t('space.share')}
              {space.memberCount > 0 && (
                <span className="mono" style={{ opacity: 0.6 }}>{space.memberCount}</span>
              )}
            </button>
          ) : (
            <button
              className="btn btn-outline btn-sm"
              style={{ color: 'var(--text-2)' }}
              onClick={() => {
                if (window.confirm(t('space.confirmLeave'))) leaveMut.mutate()
              }}
            >
              <IconLogout size={16} />
              {t('space.leave')}
            </button>
          )}
          <button
            className="btn btn-outline btn-sm"
            onClick={() => goLabels({ spaceId: space.id })}
          >
            <IconPrinter size={16} />
            {t('space.labels')}
          </button>
        </div>
      </div>

      {/* Share panel (owner) */}
      {isOwner && showShare && <SharePanel spaceId={space.id} />}

      {/* "Shared with you" hint for invited members */}
      {!isOwner && (
        <div
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
            fontSize: 12.5,
            color: 'var(--accent)',
            background: 'var(--accent-9)',
            padding: '5px 11px',
            borderRadius: 'var(--r-chip)',
            marginBottom: 20,
          }}
        >
          <IconUsers size={14} />
          {t('space.sharedWithYou')}
        </div>
      )}

      {/* Icon picker panel */}
      {editingIcon && (
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
          <SpaceIconPicker
            value={space.icon}
            onChange={(icon) => iconMut.mutate(icon)}
            label={t('space.chooseIconFor', { name: space.name.toUpperCase() })}
          />
        </div>
      )}

      {/* Box grid */}
      {isLoading ? (
        <div style={{ color: 'var(--text-3)', padding: '40px 0', textAlign: 'center' }}>
          {t('space.loadingBoxes')}
        </div>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(168px, 1fr))',
            gap: 13,
          }}
        >
          {boxes.map((box) => (
            <BoxCard key={box.number} box={box} onClick={() => goBox(box.number)} />
          ))}

          {/* New box tile */}
          {addingBox ? (
            <div
              style={{
                background: 'var(--surface)',
                border: '1.5px solid var(--accent)',
                borderRadius: 'var(--r-lg)',
                padding: 15,
                display: 'flex',
                flexDirection: 'column',
                gap: 8,
              }}
            >
              <input
                className="input"
                style={{ height: 38, fontSize: 13.5 }}
                placeholder={t('space.boxLabelPlaceholder')}
                value={newBoxLabel}
                onChange={(e) => setNewBoxLabel(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newBoxLabel.trim()) createBoxMut.mutate()
                  if (e.key === 'Escape') { setAddingBox(false); setNewBoxLabel('') }
                }}
                autoFocus
              />
              <div style={{ display: 'flex', gap: 6 }}>
                <button
                  className="btn btn-accent"
                  style={{ flex: 1, height: 34, fontSize: 13 }}
                  onClick={() => createBoxMut.mutate()}
                  disabled={!newBoxLabel.trim() || createBoxMut.isPending}
                >
                  {t('space.create')}
                </button>
                <button
                  className="btn btn-outline"
                  style={{ height: 34, fontSize: 13, padding: '0 10px' }}
                  onClick={() => { setAddingBox(false); setNewBoxLabel('') }}
                >
                  {t('space.cancel')}
                </button>
              </div>
            </div>
          ) : (
            <button
              className="dashed-tile"
              onClick={() => setAddingBox(true)}
              style={{
                background: 'transparent',
                borderRadius: 'var(--r-lg)',
                minHeight: 128,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 6,
                cursor: 'pointer',
                color: 'var(--text-4)',
                transition: 'color 0.15s, border-color 0.15s',
                fontSize: 13.5,
                fontFamily: 'inherit',
              }}
            >
              <IconPlus size={22} />
              {t('space.newBox')}
            </button>
          )}
        </div>
      )}
    </div>
  )
}

function BoxCard({ box, onClick }: { box: BoxDto; onClick: () => void }) {
  const t = useT()
  return (
    <div
      className="card"
      style={{ padding: 15, cursor: 'pointer', borderRadius: 'var(--r-lg)', minHeight: 128 }}
      onClick={onClick}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div
          className="icon-tile icon-tile-accent-tint"
          style={{ width: 38, height: 38, borderRadius: 'var(--r-sm)' }}
        >
          <span
            className="mono"
            style={{ fontSize: 18, fontWeight: 600, color: 'var(--accent)' }}
          >
            {box.number}
          </span>
        </div>
        <span
          className="mono"
          style={{ fontSize: 10, color: 'var(--text-5)', letterSpacing: '0.06em' }}
        >
          BOX-{String(box.number).padStart(3, '0')}
        </span>
      </div>
      <div style={{ marginTop: 'auto', paddingTop: 16 }}>
        <div style={{ fontSize: 14.5, fontWeight: 500 }}>{box.label}</div>
        <div
          className="mono"
          style={{ fontSize: 11.5, color: 'var(--text-4)', marginTop: 3 }}
        >
          {t('space.items', { count: box.itemCount })}
        </div>
      </div>
    </div>
  )
}
