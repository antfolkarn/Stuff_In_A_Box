import { create } from 'zustand'

interface LightboxState {
  url: string | null
  open: (url: string) => void
  close: () => void
}

// Holds the currently enlarged image (or null). A single <ImageLightbox> at the
// app root renders it, so any view can open a photo full-screen.
export const useLightbox = create<LightboxState>((set) => ({
  url: null,
  open: (url) => set({ url }),
  close: () => set({ url: null }),
}))
