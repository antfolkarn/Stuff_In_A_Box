import { api } from './client'
import type { InviteDto, InvitePreviewDto, AcceptInviteResult, MemberDto } from './types'

// --- Owner: manage the share link for a space ---
export const createInvite = (spaceId: string) =>
  api.post<InviteDto>(`/spaces/${spaceId}/invite`).then((r) => r.data)

export const getActiveInvite = (spaceId: string) =>
  api.get<InviteDto>(`/spaces/${spaceId}/invite`, { validateStatus: (s) => s === 200 || s === 204 })
    .then((r) => (r.status === 204 ? null : r.data))

export const revokeInvite = (spaceId: string) =>
  api.delete(`/spaces/${spaceId}/invite`)

// --- Owner: members ---
export const getMembers = (spaceId: string) =>
  api.get<MemberDto[]>(`/spaces/${spaceId}/members`).then((r) => r.data)

export const removeMember = (spaceId: string, userId: string) =>
  api.delete(`/spaces/${spaceId}/members/${userId}`)

// --- Member: leave a shared space ---
export const leaveSpace = (spaceId: string) =>
  api.delete(`/spaces/${spaceId}/membership`)

// --- Recipient: preview + accept a share link ---
export const getInvitePreview = (token: string) =>
  api.get<InvitePreviewDto>(`/invites/${token}`).then((r) => r.data)

export const acceptInvite = (token: string) =>
  api.post<AcceptInviteResult>(`/invites/${token}/accept`).then((r) => r.data)
