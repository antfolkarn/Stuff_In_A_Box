import { api } from './client'

export interface PlanOption {
  tier: string
  priceSek: number
  maxSpaces: number
  maxItems: number
  maxMembers: number
  aiPhotosPerMonth: number
  storageMb: number
  claudeEnrichment: boolean
  priorityQueue: boolean
  allThemes: boolean
  current: boolean
}

export interface Subscription {
  tier: string
  priceSek: number
  usage: {
    spaces: number
    maxSpaces: number
    items: number
    maxItems: number
  }
  plans: PlanOption[]
}

export const getSubscription = (): Promise<Subscription | null> =>
  api.get<Subscription>('/subscription').then((r) => r.data).catch(() => null)
