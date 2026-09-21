import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type {
  ProfileResponse,
  ResumeParseResponse,
  SetPasswordRequest,
  UpdateProfileRequest,
} from '../types'

/** Returns the current user's profile, including whether they have a password. */
export function fetchProfile(accessToken: string): Promise<ProfileResponse> {
  return request<ProfileResponse>(API_ENDPOINTS.account.profile, {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * Full-replace save of the current user's profile (common fields + the
 * section matching their role). Returns the updated profile.
 *
 * PUT /api/account/profile (Bearer auth)
 */
export function updateProfile(
  accessToken: string,
  payload: UpdateProfileRequest,
): Promise<ProfileResponse> {
  return request<ProfileResponse>(API_ENDPOINTS.account.profile, {
    method: 'PUT',
    body: payload,
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * Extracts profile fields from the user's stored resume PDF.
 *
 * Parse-only — nothing is persisted. The caller pre-fills the profile form
 * with the result and the user saves it through `updateProfile`, which stays
 * the single validated write path.
 *
 * POST /api/account/profile/parse-resume (Bearer auth)
 *
 * @param resumeUrl Whatever `TalentProfile.resumeUrl` holds — an R2 object KEY
 *   (`resumes/{userId}/…`), not a public URL. Resumes live in a private bucket,
 *   so the API mints a short-lived presigned GET for the extractor.
 */
export function parseResume(
  accessToken: string,
  resumeUrl: string,
): Promise<ResumeParseResponse> {
  return request<ResumeParseResponse>(API_ENDPOINTS.account.profileResumeParse, {
    method: 'POST',
    body: { resumeUrl },
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
