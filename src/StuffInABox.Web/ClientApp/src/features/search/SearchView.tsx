import { useQuery } from '@tanstack/react-query'
import { IconArrowRight, IconPhoto, IconSparkles, IconSearchOff } from '@tabler/icons-react'
import { search } from '../../api/search'
import { useUiStore } from '../../store/uiStore'
import { Icon } from '../../shared/components/Icon'
import { useT } from '../../i18n'

export default function SearchView() {
  const { query, goBox, goSpace } = useUiStore()
  const t = useT()
  const q = query.trim()

  const { data, isLoading } = useQuery({
    queryKey: ['search', q],
    queryFn: () => search(q),
    enabled: q.length > 0,
  })

  const total =
    (data?.spaces.length ?? 0) + (data?.boxes.length ?? 0) + (data?.items.length ?? 0)

  return (
    <div>
      <div style={{ marginBottom: 22 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 10 }}>
          <h1 style={{ fontSize: 21, fontWeight: 600, margin: 0 }}>{t('search.title')}</h1>
          <span className="mono" style={{ fontSize: 13, color: 'var(--text-4)' }}>
            {t('search.hits', { count: total })}
          </span>
        </div>
        <div style={{ fontSize: 14, color: 'var(--text-2)', marginTop: 4 }}>
          {t('search.subtitle')}
        </div>
      </div>

      {isLoading ? (
        <div style={{ color: 'var(--text-3)', padding: '40px 0', textAlign: 'center' }}>{t('search.searching')}</div>
      ) : total === 0 ? (
        <div
          style={{
            background: 'var(--surface)',
            border: 'var(--bw) solid var(--border)',
            borderRadius: 'var(--r-lg)',
            padding: '50px 20px',
            textAlign: 'center',
          }}
        >
          <IconSearchOff size={38} style={{ color: 'var(--text-5)', marginBottom: 12 }} />
          <div style={{ fontSize: 16, fontWeight: 500 }}>{t('search.noHitsTitle')}</div>
          <div style={{ fontSize: 14, color: 'var(--text-4)', marginTop: 6 }}>
            {t('search.noHitsBody', { q })}
          </div>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 22 }}>
          {/* Spaces */}
          {data!.spaces.length > 0 && (
            <ResultGroup label={t('search.groupSpaces')}>
              {data!.spaces.map((s) => (
                <ResultRow key={s.id} onClick={() => goSpace(s.id)}>
                  <div
                    className="icon-tile icon-tile-neutral"
                    style={{ width: 38, height: 38, borderRadius: 'var(--r-sm)' }}
                  >
                    <Icon name={s.icon} size={19} color="var(--text-2)" />
                  </div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 14.5, fontWeight: 500 }}>{s.name}</div>
                    <div className="mono" style={{ fontSize: 12, color: 'var(--text-4)' }}>
                      {t('search.spaceMeta', { count: s.boxCount })}
                    </div>
                  </div>
                  <IconArrowRight size={17} style={{ color: 'var(--text-5)' }} />
                </ResultRow>
              ))}
            </ResultGroup>
          )}

          {/* Boxes */}
          {data!.boxes.length > 0 && (
            <ResultGroup label={t('search.groupBoxes')}>
              {data!.boxes.map((b) => (
                <ResultRow key={`${b.spaceId}-${b.number}`} onClick={() => goBox(b.number, b.spaceId)}>
                  <div
                    className="icon-tile icon-tile-accent-tint"
                    style={{ width: 38, height: 38, borderRadius: 'var(--r-sm)' }}
                  >
                    <span className="mono" style={{ fontSize: 16, fontWeight: 600, color: 'var(--accent)' }}>
                      {b.number}
                    </span>
                  </div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 14.5, fontWeight: 500 }}>{b.label}</div>
                    <div className="mono" style={{ fontSize: 12, color: 'var(--text-4)' }}>
                      {t('search.boxMeta', { space: b.spaceName, number: b.number })}
                    </div>
                    {b.matchReason && (
                      <div style={{ fontSize: 12.5, color: 'var(--accent)', marginTop: 2 }}>
                        {t('search.contains', { reason: b.matchReason })}
                      </div>
                    )}
                  </div>
                  <IconArrowRight size={17} style={{ color: 'var(--text-5)' }} />
                </ResultRow>
              ))}
            </ResultGroup>
          )}

          {/* Items */}
          {data!.items.length > 0 && (
            <ResultGroup label={t('search.groupItems')}>
              {data!.items.map((it) => (
                <ResultRow key={it.id} onClick={() => goBox(it.boxNumber, it.spaceId)}>
                  <div
                    className="icon-tile icon-tile-neutral"
                    style={{ width: 38, height: 38, borderRadius: 'var(--r-sm)' }}
                  >
                    <IconPhoto size={17} style={{ color: 'var(--text-4)' }} />
                  </div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 14.5, fontWeight: 500 }}>{it.name}</div>
                    {it.matchedTag && (
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: 4,
                          marginTop: 3,
                          padding: '2px 8px',
                          borderRadius: 'var(--r-chip)',
                          background: 'var(--accent-9)',
                          color: 'var(--accent)',
                          fontSize: 11.5,
                        }}
                      >
                        <IconSparkles size={12} />
                        {it.matchedTag}
                      </span>
                    )}
                  </div>
                  <div
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      background: 'var(--accent-9)',
                      borderRadius: 'var(--r-sm)',
                      padding: '4px 10px',
                    }}
                  >
                    <span className="mono" style={{ fontSize: 9, color: 'var(--text-4)', letterSpacing: '0.08em' }}>
                      {t('search.boxTag')}
                    </span>
                    <span className="mono" style={{ fontSize: 16, fontWeight: 600, color: 'var(--accent)' }}>
                      {it.boxNumber}
                    </span>
                  </div>
                  <IconArrowRight size={17} style={{ color: 'var(--text-5)' }} />
                </ResultRow>
              ))}
            </ResultGroup>
          )}
        </div>
      )}
    </div>
  )
}

function ResultGroup({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="field-label" style={{ marginBottom: 9 }}>
        {label}
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 9 }}>{children}</div>
    </div>
  )
}

function ResultRow({ children, onClick }: { children: React.ReactNode; onClick: () => void }) {
  return (
    <div
      className="card"
      style={{
        padding: '13px 14px',
        borderRadius: 'var(--r-lg)',
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        cursor: 'pointer',
      }}
      onClick={onClick}
    >
      {children}
    </div>
  )
}
