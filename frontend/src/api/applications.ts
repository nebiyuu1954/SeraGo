import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type {
  ApplicationListData,
  ApplicationResponse,
  ApplyRequest,
  RecruiterJobStats,
  UpdateApplicationStatusRequest,
} from '../types'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/** POST /api/applications — apply to a Serago job. */
export function applyToJob(
  payload: ApplyRequest,
  accessToken: string,
): Promise<ApplicationListData['items'][0]> {
  return request(API_ENDPOINTS.applications.create, {
    method: 'POST',
    body: payload,
    headers: auth(accessToken),
  })
}

/** GET /api/applications/{id} — single application detail (talent or recruiter). */
export function fetchApplication(
  id: string,
  accessToken: string,
): Promise<ApplicationResponse> {
  return request(API_ENDPOINTS.applications.detail(id), {
    headers: auth(accessToken),
  })
}

/** GET /api/applications — the talent's own applications. */
export function fetchMyApplications(
  accessToken: string,
  page = 1,
  pageSize = 20,
): Promise<ApplicationListData> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return request(`${API_ENDPOINTS.applications.list}?${params}`, {
    headers: auth(accessToken),
  })
}

/** GET /api/applications/job/{jobId} — recruiter: applications for a specific job. */
export function fetchJobApplications(
  jobId: string,
  accessToken: string,
  page = 1,
  pageSize = 20,
): Promise<ApplicationListData> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return request(`${API_ENDPOINTS.applications.byJob(jobId)}?${params}`, {
    headers: auth(accessToken),
  })
}

/** GET /api/applications/all — recruiter: all applications across their posted jobs. */
export function fetchAllRecruiterApplications(
  accessToken: string,
  page = 1,
  pageSize = 20,
  opts?: { status?: string; sort?: string; search?: string; jobId?: string },
): Promise<ApplicationListData> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (opts?.status) params.set('status', opts.status)
  if (opts?.sort) params.set('sort', opts.sort)
  if (opts?.search) params.set('search', opts.search)
  if (opts?.jobId) params.set('jobId', opts.jobId)
  return request(`${API_ENDPOINTS.applications.all}?${params}`, {
    headers: auth(accessToken),
  })
}

/** GET /api/applications/stats — recruiter: per-job application counts + view counts. */
export function fetchRecruiterJobStats(
  accessToken: string,
): Promise<RecruiterJobStats[]> {
  return request(API_ENDPOINTS.applications.stats, {
    headers: auth(accessToken),
  })
}

/** PATCH /api/applications/{id}/status — recruiter: update application status. */
export function updateApplicationStatus(
  id: string,
  payload: UpdateApplicationStatusRequest,
  accessToken: string,
): Promise<ApplicationListData['items'][0]> {
  return request(API_ENDPOINTS.applications.status(id), {
    method: 'PATCH',
    body: payload,
    headers: auth(accessToken),
  })
}
