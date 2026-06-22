import { create } from 'zustand'

type View = 'home' | 'space' | 'box' | 'labels' | 'settings'

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
  pendingInvite: string | null
  pendingReset: string | null

  goHome: () => void
  goSpace: (spaceId: string) => void
  goBox: (boxNum: number, spaceId?: string) => void
  goLabels: (filter?: LabelFilter) => void
  goSettings: () => void
  setQuery: (q: string) => void
  openAdd: (boxNum?: number) => void
  closeAdd: () => void
  setPendingBox: (n: number | null) => void
  setPendingInvite: (token: string | null) => void
  setPendingReset: (token: string | null) => void
}

// Parse QR deep link on load
function parsePendingBox(): number | null {
  const hash = window.location.hash
  const match = hash.match(/[#&]box=(\d+)/)
  return match ? parseInt(match[1], 10) : null
}

// Parse share-link deep link on load (#invite=<token>)
function parsePendingInvite(): string | null {
  const match = window.location.hash.match(/[#&]invite=([A-Za-z0-9_-]+)/)
  return match ? match[1] : null
}

// Parse password-reset deep link on load (#reset=<token>)
function parsePendingReset(): string | null {
  const match = window.location.hash.match(/[#&]reset=([A-Za-z0-9_-]+)/)
  return match ? match[1] : null
}

export const useUiStore = create<UiState>((set) => ({
  view: 'home',
  spaceId: null,
  boxNum: null,
  query: '',
  addOpen: false,
  labelFilter: {},
  pendingBox: parsePendingBox(),
  pendingInvite: parsePendingInvite(),
  pendingReset: parsePendingReset(),

  goHome: () => set({ view: 'home', spaceId: null, boxNum: null, query: '' }),

  goSpace: (spaceId) => set({ view: 'space', spaceId, boxNum: null, query: '' }),

  // spaceId carries the space (owner) context so shared boxes resolve correctly.
  goBox: (boxNum, spaceId) => set((s) => ({ view: 'box', boxNum, spaceId: spaceId ?? s.spaceId, query: '' })),

  goLabels: (filter = {}) => set({ view: 'labels', labelFilter: filter, query: '' }),

  goSettings: () => set({ view: 'settings', query: '' }),

  setQuery: (q) => set({ query: q }),

  openAdd: (boxNum) =>
    set((s) => ({ addOpen: true, boxNum: boxNum ?? s.boxNum })),

  closeAdd: () => set({ addOpen: false }),

  setPendingBox: (n) => set({ pendingBox: n }),

  setPendingInvite: (token) => set({ pendingInvite: token }),

  setPendingReset: (token) => set({ pendingReset: token }),
}))
