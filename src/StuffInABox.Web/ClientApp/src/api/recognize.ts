import { api } from './client'

export interface RecognitionResult {
  name: string | null
  tags: string[]
}

// Sends a photo to the server's recognition service. Returns a suggested Swedish
// name and tags, or { name: null, tags: [] } when no provider is configured / it failed.
export const recognizeImage = (file: File): Promise<RecognitionResult> => {
  const form = new FormData()
  form.append('file', file)
  return api
    .post<RecognitionResult>('/recognize', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((r) => r.data)
    .catch(() => ({ name: null, tags: [] }))
}
