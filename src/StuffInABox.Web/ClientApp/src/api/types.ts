export interface SpaceDto {
  id: string
  name: string
  code: string
  icon: string
  boxCount: number
  itemCount: number
  isOwner: boolean
  memberCount: number
}

export interface InviteDto {
  token: string
}

export interface InvitePreviewDto {
  spaceId: string
  spaceName: string
  alreadyMember: boolean
  isOwner: boolean
}

export interface AcceptInviteResult {
  spaceId: string
  spaceName: string
}

export interface MemberDto {
  userId: string
  joinedAt: string
  // Nickname, else email, else null (UI falls back to a generic label).
  displayName: string | null
}

export interface BoxDto {
  number: number
  label: string
  spaceId: string
  itemCount: number
}

export interface BoxDetailDto {
  number: number
  label: string
  spaceId: string
}

export type ItemEnrichmentStatus = 'Pending' | 'Completed'

export interface ItemDto {
  id: string
  name: string
  tags: string[]
  photoUrl: string | null
  // 'Pending' while background photo recognition (name + tags) is still running.
  status: ItemEnrichmentStatus
}

export interface CreateItemFromPhotoResult {
  itemId: string
  name: string
  photoUrl: string | null
  status: ItemEnrichmentStatus
}

export interface CreateSpaceResult {
  spaceId: string
  name: string
  code: string
  icon: string
}

export interface CreateBoxResult {
  boxNumber: number
}

export interface AddItemResult {
  itemId: string
  name: string
  tags: string[]
}

export interface SearchResultDto {
  spaces: SpaceSearchResult[]
  boxes: BoxSearchResult[]
  items: ItemSearchResult[]
}

export interface SpaceSearchResult {
  id: string
  name: string
  icon: string
  boxCount: number
}

export interface BoxSearchResult {
  number: number
  spaceId: string
  label: string
  spaceName: string
  matchReason: string | null
}

export interface ItemSearchResult {
  id: string
  name: string
  boxNumber: number
  spaceId: string
  spaceName: string
  matchedTag: string | null
}

export interface LabelDto {
  boxNumber: number
  boxLabel: string
  spaceName: string
  itemNames: string[]
}
