import { describe, it, expect } from 'vitest'
import { memberLabel, memberInitials } from './memberDisplay'
import type { MemberDto } from '../../api/types'

function member(displayName: string | null): MemberDto {
  return { userId: 'abc123de-0000-0000-0000-000000000000', joinedAt: '', displayName }
}

describe('memberLabel', () => {
  it('uses the display name (nickname or email) when present', () => {
    expect(memberLabel(member('Stina'), 'Member')).toBe('Stina')
    expect(memberLabel(member('a@b.se'), 'Member')).toBe('a@b.se')
  })

  it('falls back to the generic label when there is none', () => {
    expect(memberLabel(member(null), 'Member')).toBe('Member')
    expect(memberLabel(member('   '), 'Member')).toBe('Member')
  })
})

describe('memberInitials', () => {
  it('derives initials from the display name', () => {
    expect(memberInitials(member('Stina'))).toBe('ST')
  })

  it('falls back to the user id when there is no display name', () => {
    expect(memberInitials(member(null))).toBe('AB')
  })
})
