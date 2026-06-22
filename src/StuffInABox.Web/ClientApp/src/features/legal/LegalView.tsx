import { IconArrowLeft } from '@tabler/icons-react'
import { useUiStore, type LegalPage } from '../../store/uiStore'
import { useT, useI18nStore } from '../../i18n'
import { getLegalDoc } from './legalContent'

/** Standalone reading view for the Terms of Service / Privacy Policy. */
export default function LegalView({ page }: { page: LegalPage }) {
  const t = useT()
  const lang = useI18nStore((s) => s.lang)
  const setLegal = useUiStore((s) => s.setLegal)
  const doc = getLegalDoc(lang, page)

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg)', color: 'var(--text)' }}>
      <div style={{ maxWidth: 720, margin: '0 auto', padding: '32px 24px 80px' }}>
        <button
          onClick={() => setLegal(null)}
          style={{
            fontSize: 13.5, color: 'var(--text-3)', display: 'flex', alignItems: 'center',
            gap: 5, marginBottom: 20, background: 'none', border: 'none', cursor: 'pointer', padding: 0,
          }}
        >
          <IconArrowLeft size={15} />
          {t('legal.back')}
        </button>

        <h1 style={{ fontSize: 26, fontWeight: 600, letterSpacing: '-0.02em', margin: 0 }}>{doc.title}</h1>
        <div style={{ fontSize: 12.5, color: 'var(--text-4)', marginTop: 6 }}>
          {t('legal.updated')}: {doc.updated}
        </div>

        <p style={{ fontSize: 14.5, color: 'var(--text-2)', marginTop: 18, lineHeight: 1.6 }}>{doc.intro}</p>

        {doc.sections.map((section) => (
          <section key={section.heading} style={{ marginTop: 26 }}>
            <h2 style={{ fontSize: 17, fontWeight: 600, margin: 0 }}>{section.heading}</h2>
            {section.paragraphs.map((p, i) => (
              <p key={i} style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 10, lineHeight: 1.6 }}>{p}</p>
            ))}
          </section>
        ))}
      </div>
    </div>
  )
}
