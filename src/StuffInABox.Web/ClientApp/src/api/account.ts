import { api } from './client'

// Downloads the user's full data export as a JSON file (GDPR data portability).
export async function exportData(): Promise<void> {
  const res = await api.get('/account/export', { responseType: 'blob' })
  const url = URL.createObjectURL(res.data as Blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `stuffinabox-export-${new Date().toISOString().slice(0, 10)}.json`
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

// Permanently deletes the account and all data (GDPR right to erasure).
export async function deleteAccount(): Promise<void> {
  await api.delete('/account')
}
