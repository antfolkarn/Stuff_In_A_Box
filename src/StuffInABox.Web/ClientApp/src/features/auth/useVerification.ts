import { useQuery } from '@tanstack/react-query'
import { getMe } from '../../api/auth'
import { useAuthStore } from '../../store/authStore'

/**
 * Current account verification status (from /auth/me). `needsVerification` is true only
 * for email accounts that haven't verified yet — OAuth accounts are always verified.
 */
export function useVerification() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const me = useQuery({
    queryKey: ['me'],
    queryFn: getMe,
    enabled: isAuthenticated,
    staleTime: 60_000,
  })

  const needsVerification = !!me.data && me.data.provider === 'email' && !me.data.emailVerified
  return { me: me.data, needsVerification }
}
