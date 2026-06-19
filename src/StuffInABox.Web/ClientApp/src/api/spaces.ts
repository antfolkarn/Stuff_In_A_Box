import { api } from './client'
import type { SpaceDto, CreateSpaceResult } from './types'

export const getSpaces = () =>
  api.get<SpaceDto[]>('/spaces').then((r) => r.data)

export const createSpace = (name: string, icon: string) =>
  api.post<CreateSpaceResult>('/spaces', { name, icon }).then((r) => r.data)

export const updateSpaceIcon = (id: string, icon: string) =>
  api.patch(`/spaces/${id}/icon`, { icon })

export const deleteSpace = (id: string) =>
  api.delete(`/spaces/${id}`)
