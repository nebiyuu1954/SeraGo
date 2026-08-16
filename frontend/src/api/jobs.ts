import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type {
  JobListData,
  JobListParams,
  JobResponse,
  JobWriteRequest,
} from '../types'

/** /api/jobs/{id} or /api/jobs/{id}/{action}. */
function jobPath(
  id: string,
  action?: 'submit' | 'approve' | 'reject' | 'restore' | 'sector',
) {
  return `${API_ENDPOINTS.jobs.list}/${id}${action ? `/${action}` : ''}`
}

function toQuery(params: JobListParams): string {
  const search = new URLSearchParams()
  if (params.q) search.set('q', params.q)
  if (params.jobType) search.set('jobType', params.jobType)
  if (params.location) search.set('location', params.location)
  if (params.sort) search.set('sort', params.sort)
  if (params.page !== undefined) search.set('page', String(params.page))
  if (params.pageSize !== undefined) search.set('pageSize', String(params.pageSize))
  if (params.mine !== undefined) search.set('mine', String(params.mine))
  if (params.status) search.set('status', params.status)
  if (params.includeInactive !== undefined) {
    search.set('includeInactive', String(params.includeInactive))
  }
  if (params.forMe !== undefined) search.set('forMe', String(params.forMe))
  if (params.sectorId) search.set('sectorId', params.sectorId)
  if (params.uncategorized !== undefined) {
    search.set('uncategorized', String(params.uncategorized))
  }
  const qs = search.toString()
  return qs ? `?${qs}` : ''
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/** GET /api/jobs — list with sorting, filtering, search and pagination. */
export function fetchJobs(
  params: JobListParams,
  accessToken: string,
): Promise<JobListData> {
  return request<JobListData>(`${API_ENDPOINTS.jobs.list}${toQuery(params)}`, {
    headers: auth(accessToken),
  })
}

/** GET /api/jobs/{id} — job detail. */
export function fetchJob(id: string, accessToken: string): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id), {
    headers: auth(accessToken),
  })
}

/** POST /api/jobs — create a job (draft by default). */
export function createJob(
  payload: JobWriteRequest,
  accessToken: string,
): Promise<JobResponse> {
  return request<JobResponse>(API_ENDPOINTS.jobs.list, {
    method: 'POST',
    body: payload,
    headers: auth(accessToken),
  })
}

/** PUT /api/jobs/{id} — full update of an editable job. */
export function updateJob(
  id: string,
  payload: JobWriteRequest,
  accessToken: string,
): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id), {
    method: 'PUT',
    body: payload,
    headers: auth(accessToken),
  })
}

/** DELETE /api/jobs/{id} — soft delete; `hard` (admin only) is permanent. */
export function deleteJob(
  id: string,
  accessToken: string,
  hard = false,
): Promise<void> {
  const qs = hard ? '?hard=true' : ''
  return request<void>(`${jobPath(id)}${qs}`, {
    method: 'DELETE',
    headers: auth(accessToken),
  })
}

/** PATCH /api/jobs/{id}/submit — owner sends a draft (or rejected) job for review. */
export function submitJob(id: string, accessToken: string): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id, 'submit'), {
    method: 'PATCH',
    headers: auth(accessToken),
  })
}

/** PATCH /api/jobs/{id}/approve — admin publishes a pending job. */
export function approveJob(id: string, accessToken: string): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id, 'approve'), {
    method: 'PATCH',
    headers: auth(accessToken),
  })
}

/** PATCH /api/jobs/{id}/reject — admin rejects a pending job with a reason. */
export function rejectJob(
  id: string,
  reason: string,
  accessToken: string,
): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id, 'reject'), {
    method: 'PATCH',
    body: { reason },
    headers: auth(accessToken),
  })
}

/** PATCH /api/jobs/{id}/restore — admin unhides a soft-deleted job. */
export function restoreJob(id: string, accessToken: string): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id, 'restore'), {
    method: 'PATCH',
    headers: auth(accessToken),
  })
}

/**
 * PATCH /api/jobs/{id}/sector — admin (re)assigns a job's sector.
 * Pass sectorId: null to clear it (back to the review queue).
 */
export function setJobSector(
  id: string,
  sectorId: string | null,
  accessToken: string,
): Promise<JobResponse> {
  return request<JobResponse>(jobPath(id, 'sector'), {
    method: 'PATCH',
    body: { sectorId },
    headers: auth(accessToken),
  })
}
