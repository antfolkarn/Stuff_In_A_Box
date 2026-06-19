import { useRef } from 'react'
import {
  IconStack2Filled,
  IconSearch,
  IconX,
  IconPlus,
  IconLogout,
  IconSun,
  IconMoon,
} from '@tabler/icons-react'
import { useAuthStore } from '../../store/authStore'
import { useUiStore } from '../../store/uiStore'
import { useThemeStore } from '../../store/themeStore'
import { useQueryClient } from '@tanstack/react-query'

export default function AppHeader() {
  const { logout } = useAuthStore()
  const { query, setQuery, goHome, openAdd } = useUiStore()
  const theme = useThemeStore((s) => s.theme)
  const toggleTheme = useThemeStore((s) => s.toggle)
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
            style={{ width: 32, height: 32, borderRadius: 9, flexShrink: 0 }}
          >
            <IconStack2Filled size={18} />
          </div>
          <div style={{ textAlign: 'left' }}>
            <div style={{ fontSize: 15.5, fontWeight: 600, lineHeight: 1.2, color: 'var(--text)' }}>
              StuffInABox
            </div>
            <div
              className="mono"
              style={{
                fontSize: 9,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                color: 'var(--text-4)',
                lineHeight: 1,
              }}
            >
              INDEX FÖR FYSISK FÖRVARING
            </div>
          </div>
        </button>

        {/* Search */}
        <div
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
            placeholder="Sök – t.ex. täcke, verktyg, vinter…"
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

        {/* Theme toggle */}
        <button
          className="btn btn-outline"
          onClick={toggleTheme}
          title={theme === 'dark' ? 'Ljust läge' : 'Mörkt läge'}
          aria-label="Växla färgtema"
          style={{ width: 42, padding: 0 }}
        >
          {theme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
        </button>

        {/* Add button */}
        <button className="btn btn-accent" onClick={() => openAdd()}>
          <IconPlus size={17} />
          Lägg till
        </button>

        {/* Logout */}
        <button
          className="btn btn-outline"
          onClick={handleLogout}
          style={{ gap: 8, paddingLeft: 4 }}
        >
          <div
            className="icon-tile icon-tile-neutral"
            style={{ width: 32, height: 32, borderRadius: 8, flexShrink: 0 }}
          >
            <span
              className="mono"
              style={{ fontSize: 14, fontWeight: 600 }}
            >
              A
            </span>
          </div>
          <span style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
            Logga ut
            <IconLogout size={16} />
          </span>
        </button>
      </div>
    </header>
  )
}
