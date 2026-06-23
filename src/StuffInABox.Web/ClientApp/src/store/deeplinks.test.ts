import { describe, it, expect } from 'vitest'
import { parsePendingBox, parsePendingInvite, parsePendingReset } from './uiStore'

describe('deep-link parsing', () => {
  it('reads a box number from #box=', () => {
    expect(parsePendingBox('#box=42')).toBe(42)
    expect(parsePendingBox('#foo&box=7')).toBe(7)
  })

  it('returns null when no box link is present', () => {
    expect(parsePendingBox('')).toBeNull()
    expect(parsePendingBox('#invite=abc')).toBeNull()
  })

  it('reads an invite token from #invite=', () => {
    expect(parsePendingInvite('#invite=Ab9_-token')).toBe('Ab9_-token')
  })

  it('reads a reset token from #reset=', () => {
    expect(parsePendingReset('#reset=zZ0-_tok')).toBe('zZ0-_tok')
  })

  it('only accepts URL-safe token characters', () => {
    // A space (or other junk) ends the captured token rather than being swallowed.
    expect(parsePendingInvite('#invite=good bad')).toBe('good')
    expect(parsePendingReset('#reset=')).toBeNull()
  })

  it('does not confuse one token type for another', () => {
    expect(parsePendingReset('#invite=abc')).toBeNull()
    expect(parsePendingInvite('#reset=abc')).toBeNull()
  })
})
