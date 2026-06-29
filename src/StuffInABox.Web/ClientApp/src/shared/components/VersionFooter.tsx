import { useQuery } from '@tanstack/react-query'
import { getVersion } from '../../api/version'

// Discreet footer showing which build is running — so a stale deploy is visible at a
// glance instead of being mistaken for a behaviour bug. Silent on failure (e.g. old
// build without the /version endpoint): renders nothing.
export default function VersionFooter() {
  const { data } = useQuery({
    queryKey: ['version'],
    queryFn: getVersion,
    staleTime: Infinity,
    retry: false,
  })

  if (!data) return null

  const commit = data.commit && data.commit !== 'unknown' ? data.commit.slice(0, 7) : null
  const built = data.buildTimeUtc ? new Date(data.buildTimeUtc).toISOString().slice(0, 16).replace('T', ' ') : null
  const parts = [data.version, commit, built].filter(Boolean)

  return (
    <div
      className="mono"
      style={{
        textAlign: 'center',
        fontSize: 10.5,
        color: 'var(--text-5)',
        padding: '20px 0 12px',
        letterSpacing: '0.03em',
      }}
    >
      {parts.join(' · ')}
    </div>
  )
}
