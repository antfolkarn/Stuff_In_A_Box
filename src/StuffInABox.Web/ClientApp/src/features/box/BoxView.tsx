import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  IconTag, IconMapPin, IconPrinter, IconPhoto, IconPackage,
  IconPencil, IconTrash, IconCheck, IconX, IconArrowLeft, IconCameraPlus, IconLoader2,
} from '@tabler/icons-react'
import { getBoxDetail, moveBox, updateBoxLabel, deleteBox } from '../../api/boxes'
import { getItemsByBox, deleteItem, updateItem } from '../../api/items'
import { getSpaces } from '../../api/spaces'
import { useUiStore } from '../../store/uiStore'
import { useLightbox } from '../../store/lightboxStore'
import { useT } from '../../i18n'
import type { ItemDto } from '../../api/types'

export default function BoxView() {
  const qc = useQueryClient()
  const { boxNum, spaceId: navSpaceId, goSpace, goLabels, openAdd } = useUiStore()
  const t = useT()
  const [editingLabel, setEditingLabel] = useState(false)
  const [labelDraft, setLabelDraft] = useState('')

  const { data: box } = useQuery({
    queryKey: ['box', boxNum, navSpaceId],
    queryFn: () => getBoxDetail(boxNum!, navSpaceId ?? undefined),
    enabled: !!boxNum,
  })

  // The box's own space is authoritative for all follow-up calls (handles shared spaces).
  const spaceId = box?.spaceId

  const { data: items = [] } = useQuery({
    queryKey: ['items', boxNum, spaceId],
    queryFn: () => getItemsByBox(boxNum!, spaceId!),
    enabled: !!boxNum && !!spaceId,
    // While any item is still being recognised in the background, poll so its
    // name, tags and final photo appear without a manual refresh.
    refetchInterval: (query) =>
      (query.state.data as ItemDto[] | undefined)?.some((i) => i.status === 'Pending') ? 2500 : false,
  })

  const { data: spaces = [] } = useQuery({ queryKey: ['spaces'], queryFn: getSpaces })

  const moveMut = useMutation({
    mutationFn: (target: string) => moveBox(boxNum!, target),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['box', boxNum] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
    },
  })

  const renameMut = useMutation({
    mutationFn: (label: string) => updateBoxLabel(boxNum!, spaceId!, label),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['box', boxNum] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      setEditingLabel(false)
    },
  })

  const deleteBoxMut = useMutation({
    mutationFn: () => deleteBox(boxNum!, spaceId!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
      if (currentSpace) goSpace(currentSpace.id)
    },
  })

  const currentSpace = spaces.find((s) => s.id === box?.spaceId)
  const isOwner = currentSpace?.isOwner ?? true

  if (!boxNum || !box) {
    return <div style={{ color: 'var(--text-3)', padding: 40 }}>{t('box.loading')}</div>
  }

  function startRename() {
    setLabelDraft(box!.label)
    setEditingLabel(true)
  }

  function confirmDeleteBox() {
    if (window.confirm(t('box.confirmDelete', { number: box!.number }))) {
      deleteBoxMut.mutate()
    }
  }

  return (
    <div>
      {/* Breadcrumb */}
      {currentSpace && (
        <button
          onClick={() => goSpace(currentSpace.id)}
          style={{
            fontSize: 13.5, color: 'var(--text-3)', display: 'flex', alignItems: 'center',
            gap: 5, marginBottom: 18, background: 'none', border: 'none', cursor: 'pointer', padding: 0,
          }}
        >
          <IconArrowLeft size={15} />
          {currentSpace.name}
        </button>
      )}

      {/* Header row */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 16, flexWrap: 'wrap', marginBottom: 20 }}>
        {/* Number tile */}
        <div
          style={{
            width: 62, height: 62, background: 'var(--accent)', borderRadius: 'var(--r-lg)',
            display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
            boxShadow: '0 3px 10px color-mix(in srgb, var(--accent) 35%, transparent)',
          }}
        >
          <span className="mono" style={{ fontSize: 30, fontWeight: 600, color: '#fff' }}>
            {box.number}
          </span>
        </div>

        <div style={{ flex: 1, minWidth: 200 }}>
          {editingLabel ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                className="input"
                style={{ height: 40, fontSize: 18, fontWeight: 600, maxWidth: 320 }}
                value={labelDraft}
                onChange={(e) => setLabelDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && labelDraft.trim()) renameMut.mutate(labelDraft.trim())
                  if (e.key === 'Escape') setEditingLabel(false)
                }}
                autoFocus
              />
              <button
                className="icon-tile icon-tile-accent"
                style={{ width: 34, height: 34, borderRadius: 'var(--r-sm)' }}
                onClick={() => labelDraft.trim() && renameMut.mutate(labelDraft.trim())}
              >
                <IconCheck size={17} />
              </button>
              <button
                className="icon-tile icon-tile-neutral"
                style={{ width: 34, height: 34, borderRadius: 'var(--r-sm)' }}
                onClick={() => setEditingLabel(false)}
              >
                <IconX size={17} />
              </button>
            </div>
          ) : (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <h1 style={{ fontSize: 23, fontWeight: 600, margin: 0 }}>{box.label}</h1>
              <button
                onClick={startRename}
                title={t('box.rename')}
                style={{ color: 'var(--text-4)', display: 'flex', padding: 4 }}
              >
                <IconPencil size={16} />
              </button>
              <button
                onClick={confirmDeleteBox}
                title={t('box.deleteBox')}
                style={{ color: 'var(--text-4)', display: 'flex', padding: 4 }}
              >
                <IconTrash size={16} />
              </button>
            </div>
          )}
          <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 3 }}>
            {t('box.items', { count: items.length })}
          </div>
        </div>

        {/* Print label pill */}
        <button
          className="btn btn-outline"
          style={{ border: 'var(--bw) dashed var(--border-2)', gap: 8, fontSize: 13.5 }}
          onClick={() => goLabels({ boxNumber: boxNum })}
        >
          <IconTag size={15} />
          {t('box.markBox')}
          <span className="mono" style={{ fontSize: 15, fontWeight: 600 }}>#{box.number}</span>
        </button>
      </div>

      {/* Location row */}
      <div
        className="location-row"
        style={{
          background: 'var(--surface)', border: 'var(--bw) solid var(--border)', borderRadius: 'var(--r-md)',
          padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20, flexWrap: 'wrap',
        }}
      >
        <IconMapPin size={17} style={{ color: 'var(--text-4)', flexShrink: 0 }} />
        <span style={{ fontSize: 14, fontWeight: 500 }}>{t('box.location')}</span>
        {isOwner ? (
          <select
            className="select"
            value={box.spaceId}
            onChange={(e) => moveMut.mutate(e.target.value)}
            style={{ flex: 1, height: 36, minWidth: 140 }}
          >
            {spaces.filter((s) => s.isOwner).map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        ) : (
          <span style={{ flex: 1, fontSize: 14, minWidth: 140 }}>{currentSpace?.name}</span>
        )}
        <span style={{ fontSize: 12, color: 'var(--text-4)', flexShrink: 0 }}>
          {t('box.numberFollows', { number: box.number })}
        </span>
        <button
          className="btn btn-outline btn-sm location-print"
          onClick={() => goLabels({ boxNumber: boxNum })}
          style={{ marginLeft: 'auto', flexShrink: 0 }}
        >
          <IconPrinter size={15} />
          {t('box.labelForThis')}
        </button>
      </div>

      {/* Items */}
      {items.length === 0 ? (
        <div
          style={{
            background: 'var(--surface)', border: 'var(--bw) solid var(--border)', borderRadius: 'var(--r-md)',
            padding: '40px 20px', textAlign: 'center', marginBottom: 16,
          }}
        >
          <IconPackage size={36} style={{ color: 'var(--text-5)', marginBottom: 10 }} />
          <div style={{ fontSize: 15, fontWeight: 500, color: 'var(--text-2)' }}>{t('box.emptyTitle')}</div>
          <div style={{ fontSize: 13.5, color: 'var(--text-4)', marginTop: 4 }}>
            {t('box.emptyBody')}
          </div>
        </div>
      ) : (
        <div
          style={{
            display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(244px, 1fr))',
            gap: 10, marginBottom: 16,
          }}
        >
          {items.map((item) => (
            <ItemCard key={item.id} item={item} boxNumber={boxNum} />
          ))}
        </div>
      )}

      {/* Add item button */}
      <button
        className="btn btn-accent"
        style={{ width: '100%', height: 46, borderRadius: 'var(--r-md)', fontSize: 15 }}
        onClick={() => openAdd(boxNum)}
      >
        <IconCameraPlus size={18} />
        {t('box.addItem')}
      </button>
    </div>
  )
}

function ItemCard({ item, boxNumber }: { item: ItemDto; boxNumber: number }) {
  const qc = useQueryClient()
  const t = useT()
  const openLightbox = useLightbox((s) => s.open)
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(item.name)
  const [imgBroken, setImgBroken] = useState(false)

  const hasPhoto = !!item.photoUrl && !imgBroken
  // Still being recognised in the background — show a placeholder until name + tags arrive.
  const isPending = item.status === 'Pending'

  const renameMut = useMutation({
    mutationFn: (name: string) => updateItem(boxNumber, item.id, { name }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['items', boxNumber] })
      setEditing(false)
    },
  })

  const deleteMut = useMutation({
    mutationFn: () => deleteItem(boxNumber, item.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['items', boxNumber] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
    },
  })

  return (
    <div
      style={{
        background: 'var(--surface)', border: 'var(--bw) solid var(--border)', borderRadius: 'var(--r-md)',
        padding: 11, display: 'flex', gap: 10, alignItems: 'flex-start',
      }}
    >
      {/* Photo — click to enlarge when present (disabled while still analysing) */}
      <div
        className="hatch-bg"
        onClick={() => !isPending && hasPhoto && openLightbox(item.photoUrl!)}
        title={!isPending && hasPhoto ? t('box.viewLarger') : undefined}
        style={{
          width: 54, height: 54, borderRadius: 'var(--r-sm)', flexShrink: 0, position: 'relative',
          display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden',
          cursor: !isPending && hasPhoto ? 'zoom-in' : 'default',
        }}
      >
        {hasPhoto ? (
          <img
            src={item.photoUrl!}
            alt={item.name}
            onError={() => setImgBroken(true)}
            style={{ width: '100%', height: '100%', objectFit: 'cover', opacity: isPending ? 0.4 : 1 }}
          />
        ) : (
          <IconPhoto size={18} style={{ color: 'var(--text-5)' }} />
        )}
        {isPending && (
          <span style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <IconLoader2 size={18} style={{ color: 'var(--accent)', animation: 'spin 1s linear infinite' }} />
            <style>{`@keyframes spin { to { transform: rotate(360deg) } }`}</style>
          </span>
        )}
      </div>

      <div style={{ flex: 1, minWidth: 0 }}>
        {editing ? (
          <div style={{ display: 'flex', gap: 6, marginBottom: 5 }}>
            <input
              className="input"
              style={{ height: 32, fontSize: 14 }}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && draft.trim()) renameMut.mutate(draft.trim())
                if (e.key === 'Escape') { setEditing(false); setDraft(item.name) }
              }}
              autoFocus
            />
            <button
              onClick={() => draft.trim() && renameMut.mutate(draft.trim())}
              style={{ color: 'var(--accent)', display: 'flex' }}
            >
              <IconCheck size={16} />
            </button>
          </div>
        ) : (
          <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 6, marginBottom: 5 }}>
            <span style={{ fontSize: 14.5, fontWeight: 500, color: isPending ? 'var(--text-4)' : undefined, fontStyle: isPending ? 'italic' : undefined }}>
              {isPending ? t('box.analyzing') : item.name}
            </span>
            <div style={{ display: 'flex', gap: 2, flexShrink: 0 }}>
              {!isPending && (
                <button onClick={() => { setDraft(item.name); setEditing(true) }} title={t('box.rename')} style={{ color: 'var(--text-5)', display: 'flex', padding: 2 }}>
                  <IconPencil size={14} />
                </button>
              )}
              <button
                onClick={() => { if (window.confirm(t('box.confirmDeleteItem', { name: item.name }))) deleteMut.mutate() }}
                title={t('box.itemRemove')}
                style={{ color: 'var(--text-5)', display: 'flex', padding: 2 }}
              >
                <IconTrash size={14} />
              </button>
            </div>
          </div>
        )}
        {!isPending && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
            {item.tags.slice(0, 4).map((tag) => (
              <span key={tag} className="tag-chip">{tag}</span>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
