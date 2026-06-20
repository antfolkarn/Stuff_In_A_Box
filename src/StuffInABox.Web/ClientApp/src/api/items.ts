import { api } from './client'
import type { ItemDto, AddItemResult } from './types'

export const getItemsByBox = (boxNumber: number) =>
  api.get<ItemDto[]>(`/boxes/${boxNumber}/items`).then((r) => r.data)

export const addItem = (boxNumber: number, name: string, tags?: string[]) =>
  api.post<AddItemResult>(`/boxes/${boxNumber}/items`, { name, tags }).then((r) => r.data)

export const updateItem = (
  boxNumber: number,
  itemId: string,
  payload: { name?: string; tags?: string[] },
) => api.patch(`/boxes/${boxNumber}/items/${itemId}`, payload)

export const deleteItem = (boxNumber: number, itemId: string) =>
  api.delete(`/boxes/${boxNumber}/items/${itemId}`)

export const uploadItemPhoto = (boxNumber: number, itemId: string, file: File) => {
  const form = new FormData()
  form.append('file', file)
  return api
    .post<{ photoUrl: string }>(`/boxes/${boxNumber}/items/${itemId}/photo`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((r) => r.data)
}
