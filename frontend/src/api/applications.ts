import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type {
  ApplicationListData,
  ApplicationResponse,
  ApplyRequest,
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

/** GET /api/applications/{id} — single application detail (the talent's own). */
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
  opts?: { status?: string; sort?: string; search?: string },
): Promise<ApplicationListData> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (opts?.status) params.set('status', opts.status)
  if (opts?.sort) params.set('sort', opts.sort)
  if (opts?.search) params.set('search', opts.search)
  return request(`${API_ENDPOINTS.applications.list}?${params}`, {
    headers: auth(accessToken),
  })
}
