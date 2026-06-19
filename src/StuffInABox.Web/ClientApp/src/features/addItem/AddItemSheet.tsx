import { useState, useEffect, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { IconX, IconCamera, IconLoader2, IconCheck, IconSparkles } from '@tabler/icons-react'
import { getSpaces } from '../../api/spaces'
import { getBoxesBySpace } from '../../api/boxes'
import { addItem, uploadItemPhoto } from '../../api/items'
import { useUiStore } from '../../store/uiStore'

type PhotoState = 'idle' | 'analyzing' | 'done'

// Plausible Swedish guesses, mirrors the prototype's simulated recognition
const GUESSES = ['Skruvdragare', 'Vinterjacka', 'Julpynt', 'Tältpinnar', 'Filtar', 'Kabelnystan']

export default function AddItemSheet() {
  const qc = useQueryClient()
  const { closeAdd, boxNum } = useUiStore()

  const [photo, setPhoto] = useState<PhotoState>('idle')
  const [guess, setGuess] = useState('')
  const [name, setName] = useState('')
  const [spaceId, setSpaceId] = useState<string>('')
  const [selectedBox, setSelectedBox] = useState<number | null>(boxNum)
  const [recent, setRecent] = useState<string[]>([])
  const [photoFile, setPhotoFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: spaces = [] } = useQuery({ queryKey: ['spaces'], queryFn: getSpaces })

  // Default the space from the active box if available, else first space
  useEffect(() => {
    if (spaceId || spaces.length === 0) return
    setSpaceId(spaces[0].id)
  }, [spaces, spaceId])

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

  // Revoke the object URL when it changes or the sheet closes
  useEffect(() => {
    return () => { if (previewUrl) URL.revokeObjectURL(previewUrl) }
  }, [previewUrl])

  const saveMut = useMutation({
    mutationFn: async () => {
      const result = await addItem(selectedBox!, name.trim())
      // Attach the photo (if any) to the freshly created item
      if (photoFile) {
        try {
          await uploadItemPhoto(selectedBox!, result.itemId, photoFile)
        } catch {
          // Photo upload failure shouldn't block the item being saved
        }
      }
      return result
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['items', selectedBox] })
      qc.invalidateQueries({ queryKey: ['boxes'] })
      qc.invalidateQueries({ queryKey: ['spaces'] })
    },
  })

  function onFileSelected(file: File | undefined) {
    if (!file) return
    setPhotoFile(file)
    setPreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return URL.createObjectURL(file)
    })
    // Simulated recognition (a real image-recognition service would go here)
    setPhoto('analyzing')
    const picked = GUESSES[Math.floor(Math.random() * GUESSES.length)]
    setTimeout(() => {
      setGuess(picked)
      setPhoto('done')
      setName((n) => n || picked)
    }, 1050)
  }

  function resetItemFields() {
    setName('')
    setPhoto('idle')
    setGuess('')
    setPhotoFile(null)
    setPreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev)
      return null
    })
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  async function handleSaveNext() {
    if (!canSave) return
    const saved = name.trim()
    await saveMut.mutateAsync()
    setRecent((r) => [saved, ...r])
    resetItemFields()
  }

  async function handleDone() {
    if (!canSave) return
    await saveMut.mutateAsync()
    closeAdd()
  }

  const canSave = !!name.trim() && !!spaceId && !!selectedBox

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
          borderRadius: 18,
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
            borderBottom: '1px solid var(--border)',
            zIndex: 1,
          }}
        >
          <div>
            <div style={{ fontSize: 17, fontWeight: 600 }}>Lägg till en sak</div>
            <div className="mono" style={{ fontSize: 11.5, color: 'var(--text-4)', marginTop: 2 }}>
              Snabbare än att strunta i det
            </div>
          </div>
          <button
            onClick={closeAdd}
            className="icon-tile icon-tile-neutral"
            style={{ width: 34, height: 34, borderRadius: 9, cursor: 'pointer' }}
          >
            <IconX size={18} />
          </button>
        </div>

        <div style={{ padding: 20 }}>
          {/* Hidden file input — the photo zone triggers it */}
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            style={{ display: 'none' }}
            onChange={(e) => onFileSelected(e.target.files?.[0])}
          />

          {/* Photo zone */}
          <div
            className={photo === 'idle' ? 'dashed-tile' : 'hatch-bg'}
            style={{
              height: 148,
              borderRadius: 14,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 6,
              position: 'relative',
              overflow: 'hidden',
              cursor: 'pointer',
              background: photo === 'done' ? 'color-mix(in srgb, var(--success-bg) 70%, #fff)' : undefined,
              marginBottom: 18,
            }}
            onClick={() => { if (photo !== 'analyzing') fileInputRef.current?.click() }}
          >
            {/* Preview thumbnail behind the overlay once a file is chosen */}
            {previewUrl && photo !== 'idle' && (
              <img
                src={previewUrl}
                alt="Förhandsvisning"
                style={{
                  position: 'absolute',
                  inset: 0,
                  width: '100%',
                  height: '100%',
                  objectFit: 'cover',
                  opacity: photo === 'done' ? 0.35 : 0.5,
                }}
              />
            )}
            {photo === 'idle' && (
              <>
                <IconCamera size={30} style={{ color: 'var(--text-3)' }} />
                <div style={{ fontSize: 14.5, fontWeight: 500 }}>Ta ett foto</div>
                <div style={{ fontSize: 12.5, color: 'var(--text-4)' }}>
                  Vi känner igen vad det är åt dig
                </div>
              </>
            )}
            {photo === 'analyzing' && (
              <>
                <div className="scan-line" />
                <IconLoader2
                  size={26}
                  style={{ color: 'var(--accent)', animation: 'spin 1s linear infinite', position: 'relative' }}
                />
                <style>{`@keyframes spin { to { transform: rotate(360deg) } }`}</style>
                <div style={{ fontSize: 14, fontWeight: 500, position: 'relative' }}>Analyserar bild…</div>
              </>
            )}
            {photo === 'done' && (
              <>
                <div
                  style={{
                    width: 40,
                    height: 40,
                    borderRadius: '50%',
                    background: 'var(--success-text)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    position: 'relative',
                  }}
                >
                  <IconCheck size={22} color="#fff" />
                </div>
                <div style={{ fontSize: 14, color: 'var(--success-text)', position: 'relative' }}>
                  Igenkänt: <strong>{guess}</strong>
                </div>
                <button
                  onClick={(e) => { e.stopPropagation(); fileInputRef.current?.click() }}
                  style={{ fontSize: 12.5, color: 'var(--accent)', position: 'relative' }}
                >
                  Ta om
                </button>
              </>
            )}
          </div>

          {/* Name field */}
          <div style={{ marginBottom: 18 }}>
            <div className="field-label">VAD ÄR DET?</div>
            <input
              className="input"
              style={{ height: 46, fontWeight: 500 }}
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="t.ex. Vinterjacka"
            />
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 5,
                marginTop: 7,
                fontSize: 12.5,
                color: 'var(--text-4)',
              }}
            >
              <IconSparkles size={14} />
              Taggas automatiskt med relaterade ord så den blir lätt att hitta
            </div>
          </div>

          {/* Destination */}
          <div style={{ marginBottom: 18 }}>
            <div className="field-label">VAR LÄGGER DU DEN?</div>
            <div style={{ display: 'flex', gap: 8 }}>
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
                {boxes.length === 0 && <option value="">Ingen låda</option>}
                {boxes.map((b) => (
                  <option key={b.number} value={b.number}>
                    Box #{b.number} · {b.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Recents */}
          {recent.length > 0 && (
            <div style={{ marginBottom: 18 }}>
              <div className="field-label">TILLAGDA NU</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {recent.map((r, i) => (
                  <span
                    key={i}
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: 4,
                      padding: '4px 10px',
                      borderRadius: 999,
                      background: 'var(--success-bg)',
                      color: 'var(--success-text)',
                      fontSize: 12.5,
                      fontWeight: 500,
                    }}
                  >
                    <IconCheck size={13} />
                    {r}
                  </span>
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
            borderTop: '1px solid var(--border)',
            display: 'flex',
            gap: 10,
          }}
        >
          <button
            className="btn btn-outline"
            style={{ flex: 1 }}
            onClick={handleSaveNext}
            disabled={!canSave || saveMut.isPending}
          >
            Spara & nästa
          </button>
          <button
            className="btn btn-accent"
            style={{ flex: 1 }}
            onClick={handleDone}
            disabled={!canSave || saveMut.isPending}
          >
            Klart
          </button>
        </div>
      </div>
    </div>
  )
}
