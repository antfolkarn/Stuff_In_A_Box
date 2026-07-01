import { create } from 'zustand'

type View = 'home' | 'space' | 'box' | 'labels' | 'settings'
export type LegalPage = 'terms' | 'privacy'

/// Surfaced when the server rejects an action for exceeding the plan's limit.
export interface QuotaNotice {
  quota: string   // "spaces" | "items" | "members"
  limit: number
  plan: string
}

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
  pendingVerify: string | null
  legal: LegalPage | null
  quotaNotice: QuotaNotice | null

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
  setPendingVerify: (token: string | null) => void
  setLegal: (page: LegalPage | null) => void
  showQuotaNotice: (notice: QuotaNotice) => void
  dismissQuotaNotice: () => void
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

// Parse email-verification deep link on load (#verify=<token>)
export function parsePendingVerify(hash: string = window.location.hash): string | null {
  const match = hash.match(/[#&]verify=([A-Za-z0-9_-]+)/)
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
  pendingVerify: parsePendingVerify(),
  legal: null,
  quotaNotice: null,

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

  setPendingVerify: (token) => set({ pendingVerify: token }),

  setLegal: (page) => set({ legal: page }),

  showQuotaNotice: (notice) => set({ quotaNotice: notice }),

  dismissQuotaNotice: () => set({ quotaNotice: null }),
}))
