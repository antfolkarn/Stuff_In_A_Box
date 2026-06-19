import { api } from './client'
import type { BoxDto, BoxDetailDto, CreateBoxResult } from './types'

export const getBoxesBySpace = (spaceId: string) =>
  api.get<BoxDto[]>(`/boxes/space/${spaceId}`).then((r) => r.data)

export const getBoxDetail = (number: number) =>
  api.get<BoxDetailDto>(`/boxes/${number}`).then((r) => r.data)

export const createBox = (spaceId: string, label: string) =>
  api.post<CreateBoxResult>('/boxes', { spaceId, label }).then((r) => r.data)

export const moveBox = (number: number, spaceId: string) =>
  api.patch(`/boxes/${number}/space`, { spaceId })

export const updateBoxLabel = (number: number, label: string) =>
  api.patch(`/boxes/${number}/label`, { label })

export const deleteBox = (number: number) =>
  api.delete(`/boxes/${number}`)
