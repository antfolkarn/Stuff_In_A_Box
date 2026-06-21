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

export interface ItemDto {
  id: string
  name: string
  tags: string[]
  photoUrl: string | null
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
