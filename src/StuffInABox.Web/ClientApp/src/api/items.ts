import { api } from './client'
import type { ItemDto, AddItemResult, CreateItemFromPhotoResult } from './types'

// spaceId identifies which space (and thus owner) the box belongs to — required
// so invited members reach the shared space's content, not their own box #n.
export const getItemsByBox = (boxNumber: number, spaceId: string) =>
  api.get<ItemDto[]>(`/boxes/${boxNumber}/items`, { params: { spaceId } }).then((r) => r.data)

export const addItem = (boxNumber: number, spaceId: string, name: string, tags?: string[]) =>
  api.post<AddItemResult>(`/boxes/${boxNumber}/items`, { spaceId, name, tags }).then((r) => r.data)

export const updateItem = (
  boxNumber: number,
  itemId: string,
  payload: { name?: string; tags?: string[] },
) => api.patch(`/boxes/${boxNumber}/items/${itemId}`, payload)

export const deleteItem = (boxNumber: number, itemId: string) =>
  api.delete(`/boxes/${boxNumber}/items/${itemId}`)

// Run AI recognition on a photo item on demand (e.g. one created without AI when the
// monthly quota was spent). 202 Accepted; the worker fills in name + tags in the background.
export const recognizeItem = (boxNumber: number, itemId: string) =>
  api.post(`/boxes/${boxNumber}/items/${itemId}/recognize`)

export const uploadItemPhoto = (boxNumber: number, itemId: string, file: File) => {
  const form = new FormData()
  form.append('file', file)
  return api
    .post<{ photoUrl: string }>(`/boxes/${boxNumber}/items/${itemId}/photo`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((r) => r.data)
}

// Fast bulk add: uploads a photo and creates the item immediately with a placeholder
// name. The server fills in the real name + tags via background recognition.
export const createItemFromPhoto = (boxNumber: number, spaceId: string, file: File) => {
  const form = new FormData()
  form.append('file', file)
  form.append('spaceId', spaceId)
  return api
    .post<CreateItemFromPhotoResult>(`/boxes/${boxNumber}/items/photo`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((r) => r.data)
}
