import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  IconTag, IconMapPin, IconPrinter, IconPhoto, IconPackage,
  IconPencil, IconTrash, IconCheck, IconX, IconArrowLeft, IconCameraPlus,
} from '@tabler/icons-react'
import { getBoxDetail, moveBox, updateBoxLabel, deleteBox } from '../../api/boxes'
import { getItemsByBox, deleteItem, updateItem } from '../../api/items'
import { getSpaces } from '../../api/spaces'
import { useUiStore } from '../../store/uiStore'
import type { ItemDto } from '../../api/types'

export default function BoxView() {
  const qc = useQueryClient()
  const { boxNum, goSpace, goLabels, openAdd } = useUiStore()
  const [editingLabel, setEditingLabel] = useState(false)
  const [labelDraft, setLabelDraft] = useState('')

  const { data: box } = useQuery({
    queryKey: ['box', boxNum],
    queryFn: () => getBoxDetail(boxNum!),
    enabled: !!boxNum,
  })

  const { data: items = [] } = useQuery({
    queryKey: ['items', boxNum],
    queryFn: () => getItemsByBox(boxNum!),
    enabled: !!boxNum,
  })

  const { data: spaces = [] } = useQuery({ queryKey: ['spaces'], queryFn: getSpaces })

  const moveMut = useMutation({
    mutationFn: (spaceId: string) => moveBox(boxNum!, spaceId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['box', boxNum] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
    },
  })

  const renameMut = useMutation({
    mutationFn: (label: string) => updateBoxLabel(boxNum!, label),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['box', boxNum] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      setEditingLabel(false)
    },
  })

  const deleteBoxMut = useMutation({
    mutationFn: () => deleteBox(boxNum!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
      if (currentSpace) goSpace(currentSpace.id)
    },
  })

  const currentSpace = spaces.find((s) => s.id === box?.spaceId)

  if (!boxNum || !box) {
    return <div style={{ color: 'var(--text-3)', padding: 40 }}>Laddar låda…</div>
  }

  function startRename() {
    setLabelDraft(box!.label)
    setEditingLabel(true)
  }

  function confirmDeleteBox() {
    if (window.confirm(`Ta bort låda #${box!.number} och alla dess föremål?`)) {
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
            width: 62, height: 62, background: 'var(--accent)', borderRadius: 16,
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
                style={{ width: 34, height: 34, borderRadius: 9 }}
                onClick={() => labelDraft.trim() && renameMut.mutate(labelDraft.trim())}
              >
                <IconCheck size={17} />
              </button>
              <button
                className="icon-tile icon-tile-neutral"
                style={{ width: 34, height: 34, borderRadius: 9 }}
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
                title="Byt namn"
                style={{ color: 'var(--text-4)', display: 'flex', padding: 4 }}
              >
                <IconPencil size={16} />
              </button>
              <button
                onClick={confirmDeleteBox}
                title="Ta bort lådan"
                style={{ color: 'var(--text-4)', display: 'flex', padding: 4 }}
              >
                <IconTrash size={16} />
              </button>
            </div>
          )}
          <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 3 }}>
            {items.length} föremål
          </div>
        </div>

        {/* Print label pill */}
        <button
          className="btn btn-outline"
          style={{ border: '1.5px dashed var(--border-2)', gap: 8, fontSize: 13.5 }}
          onClick={() => goLabels({ boxNumber: boxNum })}
        >
          <IconTag size={15} />
          Märk lådan:
          <span className="mono" style={{ fontSize: 15, fontWeight: 600 }}>#{box.number}</span>
        </button>
      </div>

      {/* Location row */}
      <div
        style={{
          background: 'var(--surface)', border: '1px solid var(--border)', borderRadius: 12,
          padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20, flexWrap: 'wrap',
        }}
      >
        <IconMapPin size={17} style={{ color: 'var(--text-4)', flexShrink: 0 }} />
        <span style={{ fontSize: 14, fontWeight: 500 }}>Plats</span>
        <select
          className="select"
          value={box.spaceId}
          onChange={(e) => moveMut.mutate(e.target.value)}
          style={{ flex: 1, height: 36, minWidth: 140 }}
        >
          {spaces.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <span style={{ fontSize: 12, color: 'var(--text-4)', flexShrink: 0 }}>
          Numret #{box.number} följer lådan om du flyttar den.
        </span>
        <button
          className="btn btn-outline btn-sm"
          onClick={() => goLabels({ boxNumber: boxNum })}
          style={{ marginLeft: 'auto', flexShrink: 0 }}
        >
          <IconPrinter size={15} />
          Etikett för denna låda
        </button>
      </div>

      {/* Items */}
      {items.length === 0 ? (
        <div
          style={{
            background: 'var(--surface)', border: '1px solid var(--border)', borderRadius: 12,
            padding: '40px 20px', textAlign: 'center', marginBottom: 16,
          }}
        >
          <IconPackage size={36} style={{ color: 'var(--text-5)', marginBottom: 10 }} />
          <div style={{ fontSize: 15, fontWeight: 500, color: 'var(--text-2)' }}>Tom låda</div>
          <div style={{ fontSize: 13.5, color: 'var(--text-4)', marginTop: 4 }}>
            Registrera det första du lägger i.
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
        style={{ width: '100%', height: 46, borderRadius: 12, fontSize: 15 }}
        onClick={() => openAdd(boxNum)}
      >
        <IconCameraPlus size={18} />
        Lägg till en sak i lådan
      </button>
    </div>
  )
}

function ItemCard({ item, boxNumber }: { item: ItemDto; boxNumber: number }) {
  const qc = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(item.name)

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
        background: 'var(--surface)', border: '1px solid var(--border)', borderRadius: 12,
        padding: 11, display: 'flex', gap: 10, alignItems: 'flex-start',
      }}
    >
      {/* Photo */}
      <div
        className="hatch-bg"
        style={{
          width: 46, height: 46, borderRadius: 9, flexShrink: 0,
          display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden',
        }}
      >
        {item.photoUrl ? (
          <img src={item.photoUrl} alt={item.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        ) : (
          <IconPhoto size={18} style={{ color: 'var(--text-5)' }} />
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
            <span style={{ fontSize: 14.5, fontWeight: 500 }}>{item.name}</span>
            <div style={{ display: 'flex', gap: 2, flexShrink: 0 }}>
              <button onClick={() => { setDraft(item.name); setEditing(true) }} title="Byt namn" style={{ color: 'var(--text-5)', display: 'flex', padding: 2 }}>
                <IconPencil size={14} />
              </button>
              <button
                onClick={() => { if (window.confirm(`Ta bort "${item.name}"?`)) deleteMut.mutate() }}
                title="Ta bort"
                style={{ color: 'var(--text-5)', display: 'flex', padding: 2 }}
              >
                <IconTrash size={14} />
              </button>
            </div>
          </div>
        )}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
          {item.tags.slice(0, 4).map((tag) => (
            <span key={tag} className="tag-chip">{tag}</span>
          ))}
        </div>
      </div>
    </div>
  )
}
