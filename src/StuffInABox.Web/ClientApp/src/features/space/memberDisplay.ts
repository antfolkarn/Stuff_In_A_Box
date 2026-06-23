import type { MemberDto } from '../../api/types'

/** Primary label for a member: their nickname/email, else a generic fallback. */
export function memberLabel(m: MemberDto, fallback: string): string {
  return m.displayName?.trim() || fallback
}

/** Two-letter avatar initials, derived from the display name when present. */
export function memberInitials(m: MemberDto): string {
  const base = m.displayName?.trim() || m.userId
  return base.slice(0, 2).toUpperCase()
}
