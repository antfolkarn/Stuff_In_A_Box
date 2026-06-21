import { useQuery } from '@tanstack/react-query'
import { IconPrinter, IconFilter, IconX, IconQrcode, IconArrowLeft } from '@tabler/icons-react'
import { getLabelData } from '../../api/labels'
import { getSpaces } from '../../api/spaces'
import { useUiStore } from '../../store/uiStore'
import { useQrCache } from '../../shared/components/useQrCode'
import { Icon } from '../../shared/components/Icon'
import { useT } from '../../i18n'
import type { LabelDto } from '../../api/types'

export default function LabelsView() {
  const { labelFilter, goLabels, goHome } = useUiStore()
  const getQr = useQrCache()
  const t = useT()

  const { data: spaces = [] } = useQuery({ queryKey: ['spaces'], queryFn: getSpaces })

  const { data: labels = [], isLoading } = useQuery({
    queryKey: ['labels', labelFilter.spaceId ?? null, labelFilter.boxNumber ?? null],
    queryFn: () => getLabelData(labelFilter.spaceId, labelFilter.boxNumber),
  })

  const totalBoxes = spaces.reduce((s, sp) => s + sp.boxCount, 0)
  const isBoxFilter = labelFilter.boxNumber != null

  return (
    <div>
      {/* Header */}
      <div className="no-print">
        <button
          onClick={goHome}
          style={{
            fontSize: 13.5,
            color: 'var(--text-3)',
            display: 'flex',
            alignItems: 'center',
            gap: 5,
            marginBottom: 16,
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            padding: 0,
          }}
        >
          <IconArrowLeft size={15} />
          {t('labels.back')}
        </button>

        <div
          style={{
            display: 'flex',
            alignItems: 'flex-start',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: 12,
            marginBottom: 18,
          }}
        >
          <div>
            <h1 style={{ fontSize: 24, fontWeight: 600, letterSpacing: '-0.02em', margin: 0 }}>
              {t('labels.title')}
            </h1>
            <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 4, maxWidth: 560 }}>
              {t('labels.subtitle')}
            </div>
          </div>
          <button className="btn btn-accent" onClick={() => window.print()}>
            <IconPrinter size={17} />
            {t('labels.print', { count: labels.length })}
          </button>
        </div>

        {/* Filter row */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 8,
            marginBottom: 22,
          }}
        >
          <span
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 5,
              fontSize: 13,
              color: 'var(--text-3)',
            }}
          >
            <IconFilter size={15} />
            {t('labels.filter')}
          </span>

          <FilterPill
            active={!labelFilter.spaceId && !isBoxFilter}
            onClick={() => goLabels({})}
          >
            {t('labels.all')}
            <span className="mono" style={{ opacity: 0.6, marginLeft: 4 }}>
              {totalBoxes}
            </span>
          </FilterPill>

          {spaces.map((s) => (
            <FilterPill
              key={s.id}
              active={labelFilter.spaceId === s.id && !isBoxFilter}
              onClick={() => goLabels({ spaceId: s.id })}
            >
              <Icon name={s.icon} size={14} />
              {s.name}
              <span className="mono" style={{ opacity: 0.6, marginLeft: 4 }}>
                {s.boxCount}
              </span>
            </FilterPill>
          ))}

          {isBoxFilter && (
            <button
              onClick={() => goLabels({})}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 6,
                padding: '6px 12px',
                borderRadius: 'var(--r-chip)',
                border: `1.5px solid var(--accent)`,
                background: 'var(--accent-9)',
                color: 'var(--accent)',
                fontSize: 13,
                cursor: 'pointer',
                fontFamily: 'inherit',
              }}
            >
              {t('labels.onlyBox', { number: labelFilter.boxNumber ?? '' })}
              <IconX size={14} />
            </button>
          )}
        </div>
      </div>

      {/* Label grid */}
      {isLoading ? (
        <div style={{ color: 'var(--text-3)', padding: '40px 0', textAlign: 'center' }}>
          {t('labels.loading')}
        </div>
      ) : (
        <div
          className="label-grid"
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(248px, 1fr))',
            gap: 14,
          }}
        >
          {labels.map((label) => (
            <LabelCard key={label.boxNumber} label={label} qr={getQr(label.boxNumber)} />
          ))}
        </div>
      )}
    </div>
  )
}

function FilterPill({
  active,
  onClick,
  children,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      onClick={onClick}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
        padding: '6px 12px',
        borderRadius: 'var(--r-chip)',
        border: active ? `1.5px solid var(--accent)` : 'var(--bw) solid var(--border-2)',
        background: active ? 'var(--accent-9)' : 'var(--surface)',
        color: active ? 'var(--accent)' : 'var(--text-2)',
        fontSize: 13,
        cursor: 'pointer',
        fontFamily: 'inherit',
      }}
    >
      {children}
    </button>
  )
}

function LabelCard({ label, qr }: { label: LabelDto; qr: string }) {
  const t = useT()
  const contents =
    label.itemNames.length > 0 ? label.itemNames.slice(0, 6).join(' · ') : t('labels.emptyBox')

  return (
    <div
      className="label-card"
      style={{
        background: 'var(--surface)',
        border: 'var(--bw) dashed var(--border-2)',
        borderRadius: 'var(--r-md)',
        padding: 18,
        minHeight: 188,
        display: 'flex',
        flexDirection: 'column',
        gap: 12,
      }}
    >
      {/* Top row: number tile + QR */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <div
            style={{
              width: 62,
              height: 62,
              background: 'var(--accent)',
              borderRadius: 'var(--r-md)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <span className="mono" style={{ fontSize: 34, fontWeight: 600, color: '#fff' }}>
              {label.boxNumber}
            </span>
          </div>
          <div className="mono" style={{ fontSize: 12, color: 'var(--text-3)', marginTop: 4, textAlign: 'center' }}>
            #{label.boxNumber}
          </div>
        </div>

        <div
          style={{
            width: 74,
            height: 74,
            background: '#fff',
            border: 'var(--bw) solid var(--border)',
            borderRadius: 'var(--r-sm)',
            padding: 5,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          {qr ? (
            <img
              src={qr}
              alt={t('labels.qrAlt', { number: label.boxNumber })}
              width={64}
              height={64}
              style={{ imageRendering: 'pixelated' }}
            />
          ) : (
            <IconQrcode size={40} style={{ color: 'var(--text-5)' }} />
          )}
        </div>
      </div>

      {/* Middle */}
      <div style={{ borderTop: 'var(--bw) solid var(--border)', paddingTop: 10 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 8 }}>
          <span style={{ fontSize: 15, fontWeight: 600 }}>{label.boxLabel}</span>
          <span style={{ fontSize: 13, color: 'var(--text-2)', flexShrink: 0 }}>{label.spaceName}</span>
        </div>
        <div style={{ fontSize: 12, color: 'var(--text-2)', marginTop: 6, lineHeight: 1.4 }}>
          {contents}
        </div>
      </div>

      {/* Footer */}
      <div
        style={{
          marginTop: 'auto',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <span
          className="mono"
          style={{ fontSize: 9, color: 'var(--text-4)', display: 'flex', alignItems: 'center', gap: 4, letterSpacing: '0.06em' }}
        >
          <IconQrcode size={11} />
          {t('labels.qrCaption')}
        </span>
        <span className="mono" style={{ fontSize: 11, color: 'var(--text-4)' }}>
          {t('labels.items', { count: label.itemNames.length })}
        </span>
      </div>
    </div>
  )
}
