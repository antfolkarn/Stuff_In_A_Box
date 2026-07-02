import { useRef } from 'react'
import {
  IconStack2Filled,
  IconSearch,
  IconX,
  IconPlus,
  IconLogout,
  IconSun,
  IconMoon,
  IconSettings,
} from '@tabler/icons-react'
import { useAuthStore } from '../../store/authStore'
import { useUiStore } from '../../store/uiStore'
import { useSettingsStore, resolveTheme } from '../../store/settingsStore'
import { useQueryClient } from '@tanstack/react-query'
import { useT } from '../../i18n'

export default function AppHeader() {
  const { logout } = useAuthStore()
  const { query, setQuery, goHome, openAdd, goSettings } = useUiStore()
  const t = useT()
  const theme = useSettingsStore((s) => s.theme)
  const toggleTheme = useSettingsStore((s) => s.toggleTheme)
  const isDark = resolveTheme(theme) === 'dark'
  const qc = useQueryClient()
  const inputRef = useRef<HTMLInputElement>(null)

  async function handleLogout() {
    await logout()
    qc.clear()
  }

  return (
    <header className="app-header no-print">
      <div className="app-header-inner">
        {/* Brand */}
        <button
          className="header-brand"
          onClick={goHome}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            padding: 0,
          }}
        >
          <div
            className="icon-tile icon-tile-accent"
            style={{ width: 32, height: 32, borderRadius: 'var(--r-sm)', flexShrink: 0 }}
          >
            <IconStack2Filled size={18} />
          </div>
          <div style={{ textAlign: 'left' }}>
            <div style={{ fontSize: 15.5, fontWeight: 600, lineHeight: 1.2, color: 'var(--text)' }}>
              StuffInABox
            </div>
            <div
              className="mono brand-eyebrow"
              style={{
                fontSize: 9,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                color: 'var(--text-4)',
                lineHeight: 1,
              }}
            >
              {t('header.eyebrow')}
            </div>
          </div>
        </button>

        {/* Search */}
        <div
          className="header-search"
          style={{
            flex: 1,
            maxWidth: 540,
            position: 'relative',
            display: 'flex',
            alignItems: 'center',
          }}
        >
          <IconSearch
            size={17}
            style={{
              position: 'absolute',
              left: 13,
              color: 'var(--text-4)',
              pointerEvents: 'none',
            }}
          />
          <input
            ref={inputRef}
            className="input"
            style={{ paddingLeft: 38, paddingRight: query ? 36 : 14 }}
            placeholder={t('header.searchPlaceholder')}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          {query && (
            <button
              onClick={() => { setQuery(''); inputRef.current?.focus() }}
              style={{
                position: 'absolute',
                right: 10,
                color: 'var(--text-4)',
                display: 'flex',
              }}
            >
              <IconX size={16} />
            </button>
          )}
        </div>

        {/* Actions — primary action first, then appearance/config, then account */}
        <div className="header-actions" style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {/* Add button (primary) */}
          <button className="btn btn-accent" onClick={() => openAdd()} aria-label={t('header.add')} style={{ flexShrink: 0 }}>
            <IconPlus size={17} />
            <span className="btn-label">{t('header.add')}</span>
          </button>

          {/* Theme toggle — hidden on mobile (colour mode lives in Settings there) */}
          <button
            className="btn btn-outline header-theme-toggle"
            onClick={toggleTheme}
            title={isDark ? t('header.lightMode') : t('header.darkMode')}
            aria-label={t('header.toggleTheme')}
            style={{ width: 42, padding: 0, flexShrink: 0 }}
          >
            {isDark ? <IconSun size={18} /> : <IconMoon size={18} />}
          </button>

          {/* Settings */}
          <button
            className="btn btn-outline"
            onClick={goSettings}
            title={t('header.settings')}
            aria-label={t('header.settings')}
            style={{ width: 42, padding: 0, flexShrink: 0 }}
          >
            <IconSettings size={18} />
          </button>

          {/* Logout (account) */}
          <button
            className="btn btn-outline"
            onClick={handleLogout}
            aria-label={t('header.logout')}
            style={{ gap: 8, paddingLeft: 4, flexShrink: 0 }}
          >
            <div
              className="icon-tile icon-tile-neutral"
              style={{ width: 32, height: 32, borderRadius: 'var(--r-sm)', flexShrink: 0 }}
            >
              <span className="mono" style={{ fontSize: 14, fontWeight: 600 }}>
                A
              </span>
            </div>
            <span className="btn-label">{t('header.logout')}</span>
            <IconLogout size={16} />
          </button>
        </div>
      </div>
    </header>
  )
}
