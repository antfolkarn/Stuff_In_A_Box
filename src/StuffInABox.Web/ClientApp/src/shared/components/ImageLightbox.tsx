import { useEffect } from 'react'
import { IconX } from '@tabler/icons-react'
import { useLightbox } from '../../store/lightboxStore'

/** Full-screen overlay that shows an item photo at a comfortable size. */
export default function ImageLightbox() {
  const url = useLightbox((s) => s.url)
  const close = useLightbox((s) => s.close)

  useEffect(() => {
    if (!url) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [url, close])

  if (!url) return null

  return (
    <div
      onClick={close}
      role="dialog"
      aria-modal="true"
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 80,
        background: 'rgba(8, 10, 14, 0.85)',
        backdropFilter: 'blur(4px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 20,
      }}
    >
      <button
        onClick={close}
        aria-label="Stäng"
        style={{
          position: 'absolute',
          top: 16,
          right: 16,
          width: 42,
          height: 42,
          borderRadius: 'var(--r-md)',
          background: 'rgba(255,255,255,0.14)',
          color: '#fff',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <IconX size={22} />
      </button>
      <img
        src={url}
        alt="Föremålsbild"
        onClick={(e) => e.stopPropagation()}
        style={{
          maxWidth: '92vw',
          maxHeight: '88vh',
          objectFit: 'contain',
          borderRadius: 'var(--r-md)',
          boxShadow: '0 18px 50px rgba(0,0,0,0.5)',
        }}
      />
    </div>
  )
}
