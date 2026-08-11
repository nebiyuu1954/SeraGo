import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchWhoAmI, getStoredAuthTokens } from '../api'
import type { WhoAmIResponse } from '../types'

export type AuthUserState =
  | { status: 'loading' }
  | { status: 'unauthenticated' }
  | { status: 'authenticated'; user: WhoAmIResponse }

export type RequiredRole = 'Talent' | 'Recruiter' | 'Admin'

/**
 * Reads the persisted token pair and resolves the current user via
 * GET /api/auth/whoami. A missing, expired, or invalid token resolves to
 * 'unauthenticated' so callers can redirect to the login page.
 *
 * The initial state is derived from localStorage at render time (no
 * state-to-state transition needed when there is nothing to fetch).
 */
export function useAuthUser(): AuthUserState {
  const [state, setState] = useState<AuthUserState>(() =>
    getStoredAuthTokens()
      ? { status: 'loading' }
      : { status: 'unauthenticated' },
  )

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    let cancelled = false
    fetchWhoAmI(tokens.accessToken)
      .then((user) => {
        if (!cancelled) setState({ status: 'authenticated', user })
      })
      .catch(() => {
        // Expired or invalid token — treat as signed out.
        if (!cancelled) setState({ status: 'unauthenticated' })
      })
    return () => {
      cancelled = true
    }
  }, [])

  return state
}

/**
 * Role guard for dashboard pages. Redirects:
 * - signed-out users → /login
 * - signed-in users with an unconfirmed email → /confirm-email (the account is
 *   inactive until they click the emailed link — nothing else is accessible)
 * - signed-in users without `role` → /dashboard (which resolves their own page)
 */
export function useRequireRole(role: RequiredRole): AuthUserState {
  const auth = useAuthUser()
  const navigate = useNavigate()

  useEffect(() => {
    if (auth.status === 'unauthenticated') {
      navigate('/login', { replace: true })
    } else if (auth.status === 'authenticated' && !auth.user.emailConfirmed) {
      navigate('/confirm-email?required=1', { replace: true })
    } else if (
      auth.status === 'authenticated' &&
      !auth.user.roles.includes(role)
    ) {
      navigate('/dashboard', { replace: true })
    }
  }, [auth, role, navigate])

  return auth
}
