import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchWhoAmI, getStoredAuthTokens } from '../api'
import type { WhoAmIResponse } from '../types'
import { setUserId } from '../lib/analytics'

export type AuthUserState =
  | { status: 'loading' }
  | { status: 'unauthenticated' }
  | { status: 'authenticated'; user: WhoAmIResponse }

export type RequiredRole = 'Talent' | 'Admin'
export type RequiredRoleSet = RequiredRole[]

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
        if (!cancelled) {
          setState({ status: 'authenticated', user })
          setUserId(user.id)
        }
      })
      .catch(() => {
        // Expired or invalid token — treat as signed out.
        if (!cancelled) {
          setState({ status: 'unauthenticated' })
          setUserId(undefined)
        }
      })
    return () => {
      cancelled = true
    }
  }, [])

  return state
}

function preserveTargetJobIfPresent() {
  try {
    const pathname = window.location.pathname
    const match = pathname.match(/\/dashboard\/talent\/jobs\/([^/?#]+)/)
    if (match && match[1]) {
      sessionStorage.setItem('serago_pending_target_job_id', match[1])
      localStorage.setItem('serago_pending_target_job_id', match[1])
      const search = new URLSearchParams(window.location.search)
      const title = search.get('title')
      if (title) {
        sessionStorage.setItem('serago_pending_target_job_title', title)
        localStorage.setItem('serago_pending_target_job_title', title)
      }
      const sectorId = search.get('sectorId')
      if (sectorId) {
        sessionStorage.setItem('serago_pending_target_job_sector_id', sectorId)
        localStorage.setItem('serago_pending_target_job_sector_id', sectorId)
      }
      const sector = search.get('sector')
      if (sector) {
        sessionStorage.setItem('serago_pending_target_job_sector_name', sector)
        localStorage.setItem('serago_pending_target_job_sector_name', sector)
      }
    }
  } catch {
    // Ignore storage errors in restricted contexts
  }
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
      preserveTargetJobIfPresent()
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

/**
 * Role guard that accepts any of the given roles. Redirects:
 * - signed-out users → /login
 * - signed-in users with an unconfirmed email → /confirm-email
 * - signed-in users without any of the allowed roles → /dashboard
 */
export function useRequireRoleAny(allowedRoles: RequiredRoleSet): AuthUserState {
  const auth = useAuthUser()
  const navigate = useNavigate()

  useEffect(() => {
    if (auth.status === 'unauthenticated') {
      preserveTargetJobIfPresent()
      navigate('/login', { replace: true })
    } else if (auth.status === 'authenticated' && !auth.user.emailConfirmed) {
      navigate('/confirm-email?required=1', { replace: true })
    } else if (
      auth.status === 'authenticated' &&
      !allowedRoles.some((r) => auth.user.roles.includes(r))
    ) {
      navigate('/dashboard', { replace: true })
    }
  }, [auth, allowedRoles, navigate])

  return auth
}
