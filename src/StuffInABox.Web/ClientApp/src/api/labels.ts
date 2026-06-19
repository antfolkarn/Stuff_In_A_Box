import { api } from './client'
import type { LabelDto } from './types'

export const getLabelData = (spaceId?: string, boxNumber?: number) =>
  api
    .get<LabelDto[]>('/labels', { params: { spaceId, boxNumber } })
    .then((r) => r.data)
