import { api } from './client'

export interface Settings {
  theme: string
  design: string
}

export const getSettings = (): Promise<Settings | null> =>
  api.get<Settings>('/settings').then((r) => r.data).catch(() => null)

export const updateSettings = (s: Settings): Promise<Settings | null> =>
  api.put<Settings>('/settings', s).then((r) => r.data).catch(() => null)
