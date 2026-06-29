import { useState, useEffect, useRef } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { IconX, IconCamera, IconLoader2, IconCheck, IconAlertTriangle, IconRefresh } from '@tabler/icons-react'
import { getSpaces } from '../../api/spaces'
import { getBoxesBySpace, getBoxDetail } from '../../api/boxes'
import { createItemFromPhoto } from '../../api/items'
import { useUiStore } from '../../store/uiStore'
import { useT } from '../../i18n'

// Cap concurrent uploads so a big batch of photos can't overload the server (and the
// background recognition that follows). The server enforces its own cap too.
const MAX_CONCURRENT = 3

type UploadStatus = 'queued' | 'uploading' | 'done' | 'error'

interface UploadTask {
  id: string
  file: File
  previewUrl: string
  status: UploadStatus
  // Destination captured when the photo was picked, so changing the selectors
  // afterwards doesn't redirect in-flight uploads.
  boxNumber: number
  spaceId: string
}

export default function AddItemSheet() {
  const qc = useQueryClient()
  const { closeAdd, boxNum, spaceId: navSpaceId } = useUiStore()
  const t = useT()

  const [spaceId, setSpaceId] = useState<string>('')
  const [selectedBox, setSelectedBox] = useState<number | null>(boxNum)
  const [tasks, setTasks] = useState<UploadTask[]>([])
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: spaces = [] } = useQuery({ queryKey: ['spaces'], queryFn: getSpaces })

  // Look up the opened box's own space. Pass the navigation space context so a
  // box in a *shared* space (owned by someone else) resolves to that space and
  // not to the current user's own box with the same number. Same query key as
  // BoxView so the lookup is served from cache. navSpaceId can still be absent
  // (search results, QR deep links) — then the server resolves the own box.
  const boxDetail = useQuery({
    queryKey: ['box', boxNum, navSpaceId],
    queryFn: () => getBoxDetail(boxNum!, navSpaceId ?? undefined),
    enabled: !!boxNum,
  })

  // Default the space to the opened box's space, falling back to the current
  // navigation context, then the first space.
  useEffect(() => {
    if (spaceId || spaces.length === 0) return
    if (boxNum && boxDetail.isPending) return
    setSpaceId(boxDetail.data?.spaceId ?? navSpaceId ?? spaces[0].id)
  }, [spaces, spaceId, boxNum, boxDetail.isPending, boxDetail.data, navSpaceId])

  const { data: boxes = [] } = useQuery({
    queryKey: ['boxes', spaceId],
    queryFn: () => getBoxesBySpace(spaceId),
    enabled: !!spaceId,
  })

  // When the space's boxes load, ensure a valid box is selected
  useEffect(() => {
    if (boxes.length === 0) return
    if (selectedBox && boxes.some((b) => b.number === selectedBox)) return
    setSelectedBox(boxes[0].number)
  }, [boxes, selectedBox])

  // Revoke object URLs on unmount
  useEffect(() => {
    return () => { tasks.forEach((task) => URL.revokeObjectURL(task.previewUrl)) }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Drive the upload queue: whenever there's spare capacity and a queued task,
  // start the next one. Flipping its status re-runs this effect, so the queue
  // fills up to MAX_CONCURRENT and then drains as uploads finish.
  useEffect(() => {
    const active = tasks.filter((task) => task.status === 'uploading').length
    if (active >= MAX_CONCURRENT) return
    const next = tasks.find((task) => task.status === 'queued')
    if (!next) return

    setTasks((ts) => ts.map((t) => (t.id === next.id ? { ...t, status: 'uploading' } : t)))
    void runUpload(next)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tasks])

  async function runUpload(task: UploadTask) {
    try {
      await createItemFromPhoto(task.boxNumber, task.spaceId, task.file)
      setTasks((ts) => ts.map((t) => (t.id === task.id ? { ...t, status: 'done' } : t)))
      // The new (placeholder) item shows up immediately; recognition fills it in later.
      qc.invalidateQueries({ queryKey: ['items', task.boxNumber] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
    } catch {
      setTasks((ts) => ts.map((t) => (t.id === task.id ? { ...t, status: 'error' } : t)))
    }
  }

  function onFilesSelected(files: FileList | null) {
    if (!files || !canUpload) return
    const picked = Array.from(files).map<UploadTask>((file) => ({
      id: crypto.randomUUID(),
      file,
      previewUrl: URL.createObjectURL(file),
      status: 'queued',
      boxNumber: selectedBox!,
      spaceId,
    }))
    setTasks((ts) => [...ts, ...picked])
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  function retry(id: string) {
    setTasks((ts) => ts.map((t) => (t.id === id ? { ...t, status: 'queued' } : t)))
  }

  const canUpload = !!spaceId && !!selectedBox
  const hasTasks = tasks.length > 0

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 60,
        background: 'rgba(16,18,22,0.34)',
        backdropFilter: 'blur(3px)',
        display: 'flex',
        alignItems: 'flex-start',
        justifyContent: 'center',
        padding: '5vh 16px 24px',
        overflowY: 'auto',
      }}
      onClick={closeAdd}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: 'var(--surface)',
          maxWidth: 460,
          width: '100%',
          borderRadius: 'var(--r-xl)',
          boxShadow: 'var(--shadow-modal)',
          maxHeight: '90vh',
          overflowY: 'auto',
          animation: 'sheetUp 0.24s cubic-bezier(.2,.8,.2,1)',
        }}
      >
        <style>{`@keyframes sheetUp { from { opacity:0; transform:translateY(10px) } to { opacity:1; transform:none } }`}</style>

        {/* Sticky header */}
        <div
          style={{
            position: 'sticky',
            top: 0,
            background: 'var(--surface)',
            padding: '18px 20px 14px',
            display: 'flex',
            alignItems: 'flex-start',
            justifyContent: 'space-between',
            borderBottom: 'var(--bw) solid var(--border)',
            zIndex: 1,
          }}
        >
          <div>
            <div style={{ fontSize: 17, fontWeight: 600 }}>{t('addItem.title')}</div>
            <div className="mono" style={{ fontSize: 11.5, color: 'var(--text-4)', marginTop: 2 }}>
              {t('addItem.subtitle')}
            </div>
          </div>
          <button
            onClick={closeAdd}
            className="icon-tile icon-tile-neutral"
            style={{ width: 34, height: 34, borderRadius: 'var(--r-sm)', cursor: 'pointer' }}
          >
            <IconX size={18} />
          </button>
        </div>

        <div style={{ padding: 20 }}>
          {/* Destination — chosen first so picked photos land in the right box */}
          <div style={{ marginBottom: 18 }}>
            <div className="field-label">{t('addItem.destination')}</div>
            <div className="stack-mobile" style={{ display: 'flex', gap: 8 }}>
              <select
                className="select"
                style={{ flex: 1 }}
                value={spaceId}
                onChange={(e) => {
                  setSpaceId(e.target.value)
                  setSelectedBox(null)
                }}
              >
                {spaces.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
              <select
                className="select"
                style={{ width: 150 }}
                value={selectedBox ?? ''}
                onChange={(e) => setSelectedBox(Number(e.target.value))}
              >
                {boxes.length === 0 && <option value="">{t('addItem.noBox')}</option>}
                {boxes.map((b) => (
                  <option key={b.number} value={b.number}>
                    {t('addItem.boxOption', { number: b.number, label: b.label })}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Hidden multi-file input — the photo zone triggers it */}
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            multiple
            style={{ display: 'none' }}
            onChange={(e) => onFilesSelected(e.target.files)}
          />

          {/* Photo drop zone */}
          <div
            className="dashed-tile"
            style={{
              minHeight: 120,
              borderRadius: 'var(--r-lg)',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 6,
              padding: 16,
              cursor: canUpload ? 'pointer' : 'not-allowed',
              opacity: canUpload ? 1 : 0.55,
              marginBottom: hasTasks ? 16 : 4,
            }}
            onClick={() => { if (canUpload) fileInputRef.current?.click() }}
          >
            <IconCamera size={30} style={{ color: 'var(--text-3)' }} />
            <div style={{ fontSize: 14.5, fontWeight: 500 }}>
              {hasTasks ? t('addItem.addMore') : t('addItem.takePhotos')}
            </div>
            <div style={{ fontSize: 12.5, color: 'var(--text-4)', textAlign: 'center', maxWidth: 320 }}>
              {canUpload ? t('addItem.bulkHint') : t('addItem.pickDestinationFirst')}
            </div>
          </div>

          {/* Upload tasks */}
          {hasTasks && (
            <div>
              <div className="mono" style={{ fontSize: 10, color: 'var(--text-4)', letterSpacing: '0.08em', marginBottom: 8 }}>
                {t('addItem.photosHeading')}
              </div>
              <style>{`@keyframes spin { to { transform: rotate(360deg) } }`}</style>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {tasks.map((task) => (
                  <UploadRow key={task.id} task={task} onRetry={() => retry(task.id)} />
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Sticky footer */}
        <div
          style={{
            position: 'sticky',
            bottom: 0,
            background: 'var(--surface)',
            padding: '14px 20px',
            borderTop: 'var(--bw) solid var(--border)',
            display: 'flex',
            gap: 10,
          }}
        >
          <button className="btn btn-accent" style={{ flex: 1 }} onClick={closeAdd}>
            {t('addItem.done')}
          </button>
        </div>
      </div>
    </div>
  )
}

function UploadRow({ task, onRetry }: { task: UploadTask; onRetry: () => void }) {
  const t = useT()

  const statusLabel: Record<UploadStatus, string> = {
    queued: t('addItem.statusQueued'),
    uploading: t('addItem.statusUploading'),
    done: t('addItem.statusDone'),
    error: t('addItem.statusError'),
  }

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 10,
        padding: 8,
        border: 'var(--bw) solid var(--border)',
        borderRadius: 'var(--r-md)',
        background: task.status === 'done' ? 'color-mix(in srgb, var(--success-bg) 55%, var(--surface))' : 'var(--surface)',
      }}
    >
      <img
        src={task.previewUrl}
        alt=""
        style={{ width: 40, height: 40, borderRadius: 'var(--r-sm)', objectFit: 'cover', flexShrink: 0 }}
      />
      <div style={{ flex: 1, minWidth: 0, fontSize: 13, color: 'var(--text-2)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {task.file.name}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0, fontSize: 12.5 }}>
        {task.status === 'uploading' && (
          <IconLoader2 size={16} style={{ color: 'var(--accent)', animation: 'spin 1s linear infinite' }} />
        )}
        {task.status === 'done' && <IconCheck size={16} style={{ color: 'var(--success-text)' }} />}
        {task.status === 'error' && <IconAlertTriangle size={16} style={{ color: 'var(--danger-text, #c0392b)' }} />}
        <span
          style={{
            color:
              task.status === 'done'
                ? 'var(--success-text)'
                : task.status === 'error'
                  ? 'var(--danger-text, #c0392b)'
                  : 'var(--text-4)',
          }}
        >
          {statusLabel[task.status]}
        </span>
        {task.status === 'error' && (
          <button onClick={onRetry} title={t('addItem.retry')} style={{ display: 'flex', color: 'var(--accent)' }}>
            <IconRefresh size={15} />
          </button>
        )}
      </div>
    </div>
  )
}
