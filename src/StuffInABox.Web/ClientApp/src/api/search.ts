import { api } from './client'
import type { SearchResultDto } from './types'

export const search = (q: string) =>
  api.get<SearchResultDto>('/search', { params: { q } }).then((r) => r.data)
