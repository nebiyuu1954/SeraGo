import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type {
  ForYouMatchResult,
  JobListData,
  JobListParams,
  JobResponse,
  JobWriteRequest,
  JobViewsData,
  JobViewsStatsResponse,
  ScraperWeekStatsList,
  ScraperWeekDetail,
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
    search.set('includeInactive', params.includeInactive ? 'true' : 'false')
  }
  if (params.forMe !== undefined) search.set('forMe', String(params.forMe))
  if (params.sectorId) search.set('sectorId', params.sectorId)
  if (params.uncategorized !== undefined) {
    search.set('uncategorized', String(params.uncategorized))
  }
  if (params.postedBy) search.set('postedBy', params.postedBy)
  if (params.source) search.set('source', params.source)
  if (params.experienceLevel) search.set('experienceLevel', params.experienceLevel)
  if (params.workMode) search.set('workMode', params.workMode)
  if (params.postedWithin !== undefined) {
    search.set('postedWithin', String(params.postedWithin))
  }
  if (params.closingWithin !== undefined) {
    search.set('closingWithin', String(params.closingWithin))
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

/** GET /api/jobs/locations — distinct locations of the live feed (filter dropdown). */
export function fetchJobLocations(accessToken: string): Promise<string[]> {
  return request<string[]>(API_ENDPOINTS.jobs.locations, {
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

/**
 * POST /api/jobs/for-you/match — run AI matching on the caller's For You
 * feed now. The AI service scores each job against the talent's profile and
 * stores the results; re-fetch the feed to display them.
 */
export function runForYouMatching(accessToken: string): Promise<ForYouMatchResult> {
  return request<ForYouMatchResult>(API_ENDPOINTS.jobs.forYouMatch, {
    method: 'POST',
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

/**
 * POST /api/admin/jobs/classify
 */
export function classifyJobs(
  jobIds: string[],
  accessToken: string,
): Promise<any> {
  return request<any>('/api/admin/jobs/classify', {
    method: 'POST',
    body: { jobIds },
    headers: auth(accessToken),
  })
}

/** GET /api/admin/jobs/views — admin paginated list of jobs by view count. */
export function fetchJobViews(
  params: {
    page?: number
    pageSize?: number
    source?: string
    sort?: string
  },
  accessToken: string,
): Promise<JobViewsData> {
  const search = new URLSearchParams()
  if (params.page !== undefined) search.set('page', String(params.page))
  if (params.pageSize !== undefined) search.set('pageSize', String(params.pageSize))
  if (params.source) search.set('source', params.source)
  if (params.sort) search.set('sort', params.sort)
  const qs = search.toString()
  return request<JobViewsData>(API_ENDPOINTS.adminJobs.views + (qs ? `?${qs}` : ''), {
    headers: auth(accessToken),
  })
}

/** GET /api/admin/jobs/views/stats — aggregate view analytics (admin). */
export function fetchJobViewsStats(accessToken: string): Promise<JobViewsStatsResponse> {
  return request<JobViewsStatsResponse>(API_ENDPOINTS.adminJobs.viewsStats, {
    headers: auth(accessToken),
  })
}

/** GET /api/admin/scraper/week-stats — list of scraper weeks. */
export function fetchScraperWeekStats(accessToken: string): Promise<ScraperWeekStatsList> {
  return request<ScraperWeekStatsList>(API_ENDPOINTS.scraper.weekStats, {
    headers: auth(accessToken),
  })
}

/** GET /api/admin/scraper/week/{periodStart} — full scraper week detail. */
export function fetchScraperWeekDetail(
  periodStart: string,
  accessToken: string,
): Promise<ScraperWeekDetail> {
  return request<ScraperWeekDetail>(API_ENDPOINTS.scraper.weekDetail(periodStart), {
    headers: auth(accessToken),
  })
}
