import { api } from './client'

// Sends a photo to the server's recognition service. Returns a suggested Swedish
// name, or null when no provider is configured / recognition failed.
export const recognizeImage = (file: File): Promise<string | null> => {
  const form = new FormData()
  form.append('file', file)
  return api
    .post<{ name: string | null }>('/recognize', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((r) => r.data.name)
    .catch(() => null)
}
