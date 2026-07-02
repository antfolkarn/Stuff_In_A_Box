import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { IconArrowLeft, IconCheck, IconDeviceLaptop, IconSun, IconMoon, IconDownload, IconTrash, IconLock } from '@tabler/icons-react'
import { useUiStore } from '../../store/uiStore'
import { useAuthStore } from '../../store/authStore'
import { useSettingsStore, THEMES, DESIGNS, type Theme, type Design } from '../../store/settingsStore'
import { exportData, deleteAccount } from '../../api/account'
import { getSubscription } from '../../api/subscription'
import { useT, useI18nStore, LANGS, type MessageKey } from '../../i18n'

// Interim contact for upgrades — plans are switched manually by an admin until Stripe
// lands. TODO: point this at a real support inbox.
const SUPPORT_EMAIL = 'hej@stuffinabox.se'

// Accent swatch shown per design (mirrors the CSS in index.css)
const DESIGN_ACCENT: Record<Design, string> = {
  standard: '#2F63E6',
  atelier: '#2C5347',
  pop: '#6C4CF1',
  nord: '#9C4A2F',
  console: '#5FE3B3',
  ledger: '#A8332A',
}

// Designs available on every plan; the rest need a plan with allThemes. Mirrors
// SettingsOptions.FreeDesigns on the server (which enforces it).
const FREE_DESIGNS: Design[] = ['standard', 'pop']

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
  // Shares the ['subscription'] cache with SubscriptionSection. Until it loads we assume
  // the free plan (locks premium designs) — the server enforces the real rule regardless.
  const { data: sub } = useQuery({ queryKey: ['subscription'], queryFn: getSubscription })
  const allThemes = sub?.plans.find((p) => p.current)?.allThemes ?? false
  const displayName = useSettingsStore((s) => s.displayName)
  const setDisplayName = useSettingsStore((s) => s.setDisplayName)
  const lang = useI18nStore((s) => s.lang)
  const setLang = useI18nStore((s) => s.setLang)
  const [nameDraft, setNameDraft] = useState(displayName)
  const [exporting, setExporting] = useState(false)
  const [deleting, setDeleting] = useState(false)

  // Commit the nickname when the field loses focus (avoids a save per keystroke).
  function commitName() {
    if (nameDraft.trim() !== displayName) setDisplayName(nameDraft)
  }

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

      {/* Nickname */}
      <section style={{ marginBottom: 32 }}>
        <div className="field-label" style={{ marginBottom: 10 }}>{t('settings.displayName')}</div>
        <input
          className="input"
          value={nameDraft}
          maxLength={40}
          placeholder={t('settings.displayNamePlaceholder')}
          onChange={(e) => setNameDraft(e.target.value)}
          onBlur={commitName}
          style={{ maxWidth: 320 }}
        />
        <div style={{ fontSize: 12.5, color: 'var(--text-4)', marginTop: 8 }}>
          {t('settings.displayNameNote')}
        </div>
      </section>

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
            // Premium designs are locked without allThemes — but never lock the one that's
            // currently active (grandfathered), so it still reads as selected.
            const locked = !allThemes && !FREE_DESIGNS.includes(id) && !active
            return (
              <button
                key={id}
                onClick={() => { if (!locked) setDesign(id) }}
                aria-disabled={locked}
                title={locked ? t('settings.designLocked') : undefined}
                style={{
                  ...cardStyle(active),
                  flexDirection: 'column',
                  alignItems: 'flex-start',
                  gap: 8,
                  cursor: locked ? 'not-allowed' : 'pointer',
                  opacity: locked ? 0.55 : 1,
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
                  {locked && <IconLock size={14} style={{ marginLeft: 'auto', color: 'var(--text-4)' }} />}
                </div>
                <span style={{ fontSize: 12.5, color: 'var(--text-3)' }}>{t(`design.${id}.desc`)}</span>
              </button>
            )
          })}
        </div>
        <div style={{ fontSize: 12.5, color: 'var(--text-4)', marginTop: 12 }}>
          {allThemes ? t('settings.designNote') : t('settings.designLockedNote')}
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

      {/* Subscription */}
      <SubscriptionSection />

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

// The user's plan, usage against its limits, and the tiers to compare/upgrade to.
// Read-only in Fas A: spaces/items meters only (AI + storage meters follow usage tracking).
function SubscriptionSection() {
  const t = useT()
  const { data } = useQuery({ queryKey: ['subscription'], queryFn: getSubscription })
  if (!data) return null

  const planLabel = (tier: string) => {
    const key = `plan.${tier}.label` as MessageKey
    const label = t(key)
    return label === key ? tier : label // fall back to the raw key for unknown tiers
  }
  const price = (sek: number) =>
    sek <= 0 ? t('subscription.free0') : t('subscription.perMonth', { price: sek })
  const lim = (n: number) => (n < 0 ? t('subscription.unlimited') : String(n))
  const storage = (mb: number) => (mb >= 1024 ? `${Math.round(mb / 1024)} GB` : `${mb} MB`)

  return (
    <section style={{ marginTop: 32, borderTop: 'var(--bw) solid var(--border)', paddingTop: 24 }}>
      <div className="field-label" style={{ marginBottom: 12 }}>{t('subscription.title')}</div>

      {/* Current plan + usage meters — accent-framed hero, but text stays readable */}
      <div
        style={{
          display: 'flex', flexDirection: 'column', alignItems: 'stretch', gap: 14,
          padding: '16px', borderRadius: 'var(--r-md)',
          border: '1.5px solid var(--accent)', background: 'var(--accent-9)',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 12 }}>
          <span style={{ fontSize: 16, fontWeight: 600, color: 'var(--text)' }}>{planLabel(data.tier)}</span>
          <span style={{ fontSize: 14, color: 'var(--accent)', fontWeight: 600 }}>{price(data.priceSek)}</span>
        </div>
        <div style={{ display: 'grid', gap: 10 }}>
          <UsageBar label={t('subscription.spaces')} used={data.usage.spaces} max={data.usage.maxSpaces} unlimited={t('subscription.unlimited')} />
          <UsageBar label={t('subscription.items')} used={data.usage.items} max={data.usage.maxItems} unlimited={t('subscription.unlimited')} />
          <UsageBar label={t('subscription.aiPhotos')} used={data.usage.aiPhotos} max={data.usage.aiPhotosLimit} unlimited={t('subscription.unlimited')} />
          <UsageBar label={t('subscription.storage')} used={data.usage.storageMb} max={data.usage.storageLimitMb} unlimited={t('subscription.unlimited')} fmt={storage} />
        </div>
      </div>

      {/* Compare tiers */}
      <div className="field-label" style={{ margin: '20px 0 10px' }}>{t('subscription.comparePlans')}</div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 10 }}>
        {data.plans.map((p) => (
          <div key={p.tier} style={{ ...cardStyle(p.current), flexDirection: 'column', alignItems: 'stretch', gap: 8, cursor: 'default' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span style={{ fontSize: 15, fontWeight: 600, color: 'var(--text)' }}>{planLabel(p.tier)}</span>
              {p.current && (
                <span style={{ marginLeft: 'auto', fontSize: 11, fontWeight: 600, color: 'var(--accent)', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                  {t('subscription.currentBadge')}
                </span>
              )}
            </div>
            <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--accent)' }}>{price(p.priceSek)}</div>
            <ul style={{ margin: 0, padding: 0, listStyle: 'none', display: 'grid', gap: 5, fontSize: 12.5, color: 'var(--text-2)' }}>
              <Feat text={t('subscription.limitSpaces', { n: lim(p.maxSpaces) })} />
              <Feat text={t('subscription.limitItems', { n: lim(p.maxItems) })} />
              <Feat text={t('subscription.limitAi', { n: p.aiPhotosPerMonth })} />
              <Feat text={t('subscription.limitStorage', { size: storage(p.storageMb) })} />
              <Feat text={t('subscription.limitMembers', { n: lim(p.maxMembers) })} />
              {p.claudeEnrichment && <Feat text={t('subscription.featClaude')} />}
              {p.priorityQueue && <Feat text={t('subscription.featPriority')} />}
              {p.allThemes && <Feat text={t('subscription.featThemes')} />}
            </ul>
            {!p.current && (
              <a
                className="btn btn-outline btn-sm"
                href={`mailto:${SUPPORT_EMAIL}?subject=${encodeURIComponent(`StuffInABox – ${t('subscription.upgrade')}: ${p.tier}`)}`}
                style={{ marginTop: 'auto', textDecoration: 'none', justifyContent: 'center' }}
              >
                {t('subscription.upgrade')}
              </a>
            )}
          </div>
        ))}
      </div>
      <div style={{ fontSize: 12.5, color: 'var(--text-4)', marginTop: 10 }}>{t('subscription.upgradeNote')}</div>
    </section>
  )
}

function UsageBar({ label, used, max, unlimited, fmt }: { label: string; used: number; max: number; unlimited: string; fmt?: (n: number) => string }) {
  const pct = max <= 0 ? 0 : Math.min(100, Math.round((used / max) * 100))
  // Being *at* your allowance is normal (amber nudge); only going *over* is a problem (red).
  const barColor = max > 0 && used > max ? '#B91C1C' : max > 0 && used >= max ? '#B7791F' : 'var(--accent)'
  const show = fmt ?? ((n: number) => String(n))
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, marginBottom: 5 }}>
        <span style={{ color: 'var(--text-2)' }}>{label}</span>
        <span style={{ fontWeight: 500 }}>{show(used)} / {max < 0 ? unlimited : show(max)}</span>
      </div>
      {max >= 0 && (
        <div style={{ height: 6, borderRadius: 999, background: 'var(--border-2)', overflow: 'hidden' }}>
          <div style={{ height: '100%', width: `${pct}%`, background: barColor, transition: 'width 0.2s' }} />
        </div>
      )}
    </div>
  )
}

function Feat({ text }: { text: string }) {
  return (
    <li style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
      <IconCheck size={13} style={{ color: 'var(--accent)', flexShrink: 0 }} />
      {text}
    </li>
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
