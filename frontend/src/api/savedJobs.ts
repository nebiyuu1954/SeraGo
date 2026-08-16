import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type { SavedJobListData } from '../types'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/** GET /api/saved-jobs — the caller's saved jobs with lifecycle status + countdown. */
export function fetchSavedJobs(accessToken: string): Promise<SavedJobListData> {
  return request<SavedJobListData>(API_ENDPOINTS.savedJobs.list, {
    headers: auth(accessToken),
  })
}

/** POST /api/saved-jobs/{jobId} — save a visible job (idempotent). */
export function saveJob(jobId: string, accessToken: string): Promise<void> {
  return request<void>(`${API_ENDPOINTS.savedJobs.list}/${jobId}`, {
    method: 'POST',
    headers: auth(accessToken),
  })
}

/** DELETE /api/saved-jobs/{jobId} — unsave. */
export function unsaveJob(jobId: string, accessToken: string): Promise<void> {
  return request<void>(`${API_ENDPOINTS.savedJobs.list}/${jobId}`, {
    method: 'DELETE',
    headers: auth(accessToken),
  })
}
