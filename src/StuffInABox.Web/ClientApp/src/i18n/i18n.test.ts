import { describe, it, expect } from 'vitest'
import { translate } from './index'

describe('translate', () => {
  it('returns the language-specific string for a known key', () => {
    expect(translate('sv', 'common.loading')).toBe('Laddar…')
    // English differs from Swedish for the same key.
    expect(translate('en', 'common.loading')).not.toBe('Laddar…')
  })

  it('falls back to the key itself for an unknown key', () => {
    // @ts-expect-error — deliberately passing a non-existent key
    expect(translate('en', 'does.not.exist')).toBe('does.not.exist')
  })

  it('interpolates {params}', () => {
    // Use a key whose value contains a placeholder by checking round-trip behaviour:
    // an unknown key returns the raw template, so we can assert interpolation directly.
    // @ts-expect-error — using the raw template path on purpose
    expect(translate('en', '{count} items', { count: 3 })).toBe('3 items')
  })

  it('leaves unmatched placeholders untouched', () => {
    // @ts-expect-error — raw template path
    expect(translate('en', 'hi {name}', {})).toBe('hi {name}')
  })
})
