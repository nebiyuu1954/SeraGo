import { useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { clearStoredAuthTokens, getStoredAuthTokens, signOut } from '../api'
import { setUserId } from '../lib/analytics'

/**
 * Signs the current user out: revokes the refresh token server-side
 * (best-effort), clears the local token pair, and navigates to the standalone
 * login page so the app chrome (and the signed-in header) remounts fresh.
 */
export function useSignOut(): () => void {
  const navigate = useNavigate()

  return useCallback(() => {
    const tokens = getStoredAuthTokens()
    if (tokens) {
      // Best-effort: revoke the refresh token server-side, then clear locally
      // regardless of the outcome.
      signOut(tokens.accessToken).catch(() => {
        // Offline / already-expired token — local sign-out still proceeds.
      })
    }
    clearStoredAuthTokens()
    setUserId(undefined)
    navigate('/login', { replace: true })
  }, [navigate])
}
