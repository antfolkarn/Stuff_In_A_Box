import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { IconArrowLeft, IconCheck, IconDeviceLaptop, IconSun, IconMoon, IconDownload, IconTrash } from '@tabler/icons-react'
import { useUiStore } from '../../store/uiStore'
import { useAuthStore } from '../../store/authStore'
import { useSettingsStore, THEMES, DESIGNS, type Theme, type Design } from '../../store/settingsStore'
import { exportData, deleteAccount } from '../../api/account'
import { useT, useI18nStore, LANGS } from '../../i18n'

// Accent swatch shown per design (mirrors the CSS in index.css)
const DESIGN_ACCENT: Record<Design, string> = {
  standard: '#2F63E6',
  atelier: '#2C5347',
  pop: '#6C4CF1',
}

const THEME_ICON: Record<Theme, typeof IconSun> = {
  light: IconSun,
  dark: IconMoon,
  system: IconDeviceLaptop,
}

export default function SettingsView() {
  const { goHome, setLegal } = useUiStore()
  const t = useT()
  const qc = useQueryClient()
  const logout = useAuthStore((s) => s.logout)
  const theme = useSettingsStore((s) => s.theme)
  const design = useSettingsStore((s) => s.design)
  const setTheme = useSettingsStore((s) => s.setTheme)
  const setDesign = useSettingsStore((s) => s.setDesign)
  const lang = useI18nStore((s) => s.lang)
  const setLang = useI18nStore((s) => s.setLang)
  const [exporting, setExporting] = useState(false)
  const [deleting, setDeleting] = useState(false)

  async function handleExport() {
    setExporting(true)
    try {
      await exportData()
    } finally {
      setExporting(false)
    }
  }

  async function handleDelete() {
    if (!window.confirm(t('account.confirmDelete'))) return
    setDeleting(true)
    try {
      await deleteAccount()
      await logout()
      qc.clear()
      window.location.reload()
    } catch {
      setDeleting(false)
    }
  }

  return (
    <div style={{ maxWidth: 640 }}>
      <button
        onClick={goHome}
        style={{
          fontSize: 13.5, color: 'var(--text-3)', display: 'flex', alignItems: 'center',
          gap: 5, marginBottom: 16, background: 'none', border: 'none', cursor: 'pointer', padding: 0,
        }}
      >
        <IconArrowLeft size={15} />
        {t('settings.back')}
      </button>

      <h1 style={{ fontSize: 24, fontWeight: 600, letterSpacing: '-0.02em', margin: 0 }}>
        {t('settings.title')}
      </h1>
      <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 4, marginBottom: 28 }}>
        {t('settings.subtitle')}
      </div>

      {/* Theme */}
      <section style={{ marginBottom: 32 }}>
        <div className="field-label" style={{ marginBottom: 10 }}>{t('settings.theme')}</div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
          {THEMES.map((id) => {
            const Icon = THEME_ICON[id]
            const active = theme === id
            return (
              <button
                key={id}
                onClick={() => setTheme(id)}
                style={optionStyle(active)}
              >
                <Icon size={18} />
                {t(`theme.${id}`)}
                {active && <IconCheck size={16} style={{ marginLeft: 'auto' }} />}
              </button>
            )
          })}
        </div>
      </section>

      {/* Design */}
      <section style={{ marginBottom: 32 }}>
        <div className="field-label" style={{ marginBottom: 10 }}>{t('settings.design')}</div>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))',
            gap: 10,
          }}
        >
          {DESIGNS.map((id) => {
            const active = design === id
            return (
              <button
                key={id}
                onClick={() => setDesign(id)}
                style={{
                  ...cardStyle(active),
                  flexDirection: 'column',
                  alignItems: 'flex-start',
                  gap: 8,
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, width: '100%' }}>
                  <span
                    style={{
                      width: 22, height: 22, borderRadius: 'var(--r-sm)', flexShrink: 0,
                      background: DESIGN_ACCENT[id],
                      boxShadow: 'inset 0 0 0 1px rgba(0,0,0,0.08)',
                    }}
                  />
                  <span style={{ fontSize: 15, fontWeight: 500 }}>{t(`design.${id}.label`)}</span>
                  {active && <IconCheck size={16} style={{ marginLeft: 'auto', color: 'var(--accent)' }} />}
                </div>
                <span style={{ fontSize: 12.5, color: 'var(--text-3)' }}>{t(`design.${id}.desc`)}</span>
              </button>
            )
          })}
        </div>
        <div style={{ fontSize: 12.5, color: 'var(--text-4)', marginTop: 12 }}>
          {t('settings.designNote')}
        </div>
      </section>

      {/* Language */}
      <section>
        <div className="field-label" style={{ marginBottom: 10 }}>{t('settings.language')}</div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
          {LANGS.map((l) => {
            const active = lang === l.id
            return (
              <button
                key={l.id}
                onClick={() => setLang(l.id)}
                style={optionStyle(active)}
              >
                {l.label}
                {active && <IconCheck size={16} style={{ marginLeft: 'auto' }} />}
              </button>
            )
          })}
        </div>
        <div style={{ fontSize: 12.5, color: 'var(--text-4)', marginTop: 12 }}>
          {t('settings.languageNote')}
        </div>
      </section>

      {/* Account & data (GDPR) */}
      <section style={{ marginTop: 32, borderTop: 'var(--bw) solid var(--border)', paddingTop: 24 }}>
        <div className="field-label" style={{ marginBottom: 12 }}>{t('account.title')}</div>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap', marginBottom: 18 }}>
          <div style={{ maxWidth: 380 }}>
            <div style={{ fontSize: 14.5, fontWeight: 500 }}>{t('account.export')}</div>
            <div style={{ fontSize: 12.5, color: 'var(--text-3)', marginTop: 2 }}>{t('account.exportHint')}</div>
          </div>
          <button className="btn btn-outline btn-sm" onClick={handleExport} disabled={exporting}>
            <IconDownload size={16} />
            {t('account.export')}
          </button>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
          <div style={{ maxWidth: 380 }}>
            <div style={{ fontSize: 14.5, fontWeight: 500, color: '#B91C1C' }}>{t('account.delete')}</div>
            <div style={{ fontSize: 12.5, color: 'var(--text-3)', marginTop: 2 }}>{t('account.deleteHint')}</div>
          </div>
          <button
            className="btn btn-sm"
            onClick={handleDelete}
            disabled={deleting}
            style={{ border: '1.5px solid #FCA5A5', color: '#B91C1C', background: 'var(--surface)' }}
          >
            <IconTrash size={16} />
            {deleting ? t('account.deleting') : t('account.delete')}
          </button>
        </div>
      </section>

      {/* Legal footer */}
      <div style={{ marginTop: 32, paddingTop: 16, fontSize: 12.5, color: 'var(--text-4)', display: 'flex', gap: 16 }}>
        <a href="#" onClick={(e) => { e.preventDefault(); setLegal('terms') }} style={{ color: 'var(--text-3)' }}>
          {t('legal.terms')}
        </a>
        <a href="#" onClick={(e) => { e.preventDefault(); setLegal('privacy') }} style={{ color: 'var(--text-3)' }}>
          {t('legal.privacy')}
        </a>
      </div>
    </div>
  )
}

function optionStyle(active: boolean): React.CSSProperties {
  return {
    ...cardStyle(active),
    minWidth: 130,
    gap: 8,
  }
}

function cardStyle(active: boolean): React.CSSProperties {
  return {
    display: 'flex',
    alignItems: 'center',
    padding: '12px 14px',
    borderRadius: 'var(--r-md)',
    border: active ? '1.5px solid var(--accent)' : 'var(--bw) solid var(--border-2)',
    background: active ? 'var(--accent-9)' : 'var(--surface)',
    color: active ? 'var(--accent)' : 'var(--text)',
    fontSize: 14,
    fontWeight: 500,
    cursor: 'pointer',
    fontFamily: 'inherit',
    textAlign: 'left',
    transition: 'border-color 0.15s, background 0.15s',
  }
}
