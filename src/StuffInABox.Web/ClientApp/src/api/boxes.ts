import { api } from './client'
import type { BoxDto, BoxDetailDto, CreateBoxResult } from './types'

export const getBoxesBySpace = (spaceId: string) =>
  api.get<BoxDto[]>(`/boxes/space/${spaceId}`).then((r) => r.data)

// spaceId is optional: omit it for your own boxes (e.g. a QR deep link); pass it
// when navigating inside a (possibly shared) space so the right owner resolves.
export const getBoxDetail = (number: number, spaceId?: string) =>
  api.get<BoxDetailDto>(`/boxes/${number}`, { params: spaceId ? { spaceId } : undefined }).then((r) => r.data)

export const createBox = (spaceId: string, label: string) =>
  api.post<CreateBoxResult>('/boxes', { spaceId, label }).then((r) => r.data)

export const moveBox = (number: number, spaceId: string) =>
  api.patch(`/boxes/${number}/space`, { spaceId })

export const updateBoxLabel = (number: number, spaceId: string, label: string) =>
  api.patch(`/boxes/${number}/label`, { spaceId, label })

export const deleteBox = (number: number, spaceId: string) =>
  api.delete(`/boxes/${number}`, { params: { spaceId } })
