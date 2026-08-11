import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type { ProfileResponse, SetPasswordRequest } from '../types'

/** Returns the current user's profile, including whether they have a password. */
export function fetchProfile(accessToken: string): Promise<ProfileResponse> {
  return request<ProfileResponse>(API_ENDPOINTS.account.profile, {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * Sets a first-time password on an account that has none (e.g. a Google
 * sign-up). Once set, the user can sign in with email + password anywhere.
 *
 * POST /api/account/password/set (Bearer auth)
 */
export function setPassword(
  accessToken: string,
  payload: SetPasswordRequest,
): Promise<void> {
  return request<void>(API_ENDPOINTS.account.passwordSet, {
    method: 'POST',
    body: payload,
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}
