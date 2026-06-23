import { create } from 'zustand'

type View = 'home' | 'space' | 'box' | 'labels' | 'settings'
export type LegalPage = 'terms' | 'privacy'

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
  legal: LegalPage | null

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
  setLegal: (page: LegalPage | null) => void
}

// Parse QR deep link on load (#box=<n>). Hash is injectable for testing.
export function parsePendingBox(hash: string = window.location.hash): number | null {
  const match = hash.match(/[#&]box=(\d+)/)
  return match ? parseInt(match[1], 10) : null
}

// Parse share-link deep link on load (#invite=<token>)
export function parsePendingInvite(hash: string = window.location.hash): string | null {
  const match = hash.match(/[#&]invite=([A-Za-z0-9_-]+)/)
  return match ? match[1] : null
}

// Parse password-reset deep link on load (#reset=<token>)
export function parsePendingReset(hash: string = window.location.hash): string | null {
  const match = hash.match(/[#&]reset=([A-Za-z0-9_-]+)/)
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
  legal: null,

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

  setLegal: (page) => set({ legal: page }),
}))
