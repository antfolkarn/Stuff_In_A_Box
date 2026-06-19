import { create } from 'zustand'

type View = 'home' | 'space' | 'box' | 'labels'

interface LabelFilter {
  spaceId?: string
  boxNumber?: number
}

interface UiState {
  view: View
  spaceId: string | null
  boxNum: number | null
  query: string
  addOpen: boolean
  labelFilter: LabelFilter
  pendingBox: number | null

  goHome: () => void
  goSpace: (spaceId: string) => void
  goBox: (boxNum: number) => void
  goLabels: (filter?: LabelFilter) => void
  setQuery: (q: string) => void
  openAdd: (boxNum?: number) => void
  closeAdd: () => void
  setPendingBox: (n: number | null) => void
}

// Parse QR deep link on load
function parsePendingBox(): number | null {
  const hash = window.location.hash
  const match = hash.match(/[#&]box=(\d+)/)
  return match ? parseInt(match[1], 10) : null
}

export const useUiStore = create<UiState>((set) => ({
  view: 'home',
  spaceId: null,
  boxNum: null,
  query: '',
  addOpen: false,
  labelFilter: {},
  pendingBox: parsePendingBox(),

  goHome: () => set({ view: 'home', spaceId: null, boxNum: null, query: '' }),

  goSpace: (spaceId) => set({ view: 'space', spaceId, boxNum: null, query: '' }),

  goBox: (boxNum) => set({ view: 'box', boxNum, query: '' }),

  goLabels: (filter = {}) => set({ view: 'labels', labelFilter: filter, query: '' }),

  setQuery: (q) => set({ query: q }),

  openAdd: (boxNum) =>
    set((s) => ({ addOpen: true, boxNum: boxNum ?? s.boxNum })),

  closeAdd: () => set({ addOpen: false }),

  setPendingBox: (n) => set({ pendingBox: n }),
}))
