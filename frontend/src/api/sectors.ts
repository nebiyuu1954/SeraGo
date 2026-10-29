import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type {
  SectorDetailResponse,
  SectorResponse,
  SyncScrapedJobsResult,
  ClassificationHistoryResponse,
  AuditReportResponse,
  CorrectionRequestItem,
} from '../types'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/** GET /api/sectors — active sectors, for pickers (profile, filters). */
export function fetchSectors(accessToken?: string): Promise<SectorResponse[]> {
  return request<SectorResponse[]>(API_ENDPOINTS.sectors.list, {
    headers: accessToken ? auth(accessToken) : undefined,
  })
}

/** GET /api/admin/sectors — all sectors with aliases + job counts (admin). */
export function fetchAdminSectors(
  accessToken: string,
): Promise<SectorDetailResponse[]> {
  return request<SectorDetailResponse[]>(API_ENDPOINTS.sectors.admin, {
    headers: auth(accessToken),
  })
}

/** POST /api/admin/sectors — create a sector (admin). */
export function createSector(
  payload: { name: string; slug?: string },
  accessToken: string,
): Promise<SectorResponse> {
  return request<SectorResponse>(API_ENDPOINTS.sectors.admin, {
    method: 'POST',
    body: payload,
    headers: auth(accessToken),
  })
}

/** PUT /api/admin/sectors/{id} — rename and/or toggle active (admin). */
export function updateSector(
  id: string,
  payload: { name?: string; isActive?: boolean },
  accessToken: string,
): Promise<SectorResponse> {
  return request<SectorResponse>(`${API_ENDPOINTS.sectors.admin}/${id}`, {
    method: 'PUT',
    body: payload,
    headers: auth(accessToken),
  })
}

/** POST /api/admin/sectors/{id}/aliases — add raw names that map to this sector (admin). */
export function addSectorAliases(
  id: string,
  aliases: string[],
  accessToken: string,
): Promise<{ id: string; alias: string }[]> {
  return request<{ id: string; alias: string }[]>(
    `${API_ENDPOINTS.sectors.admin}/${id}/aliases`,
    { method: 'POST', body: { aliases }, headers: auth(accessToken) },
  )
}

/** DELETE /api/admin/sectors/{sectorId}/aliases/{aliasId} (admin). */
export function removeSectorAlias(
  sectorId: string,
  aliasId: string,
  accessToken: string,
): Promise<void> {
  return request<void>(
    `${API_ENDPOINTS.sectors.admin}/${sectorId}/aliases/${aliasId}`,
    { method: 'DELETE', headers: auth(accessToken) },
  )
}

/** DELETE /api/admin/sectors/{id} — only when no jobs reference it (admin). */
export function deleteSector(id: string, accessToken: string): Promise<void> {
  return request<void>(`${API_ENDPOINTS.sectors.admin}/${id}`, {
    method: 'DELETE',
    headers: auth(accessToken),
  })
}

/** POST /api/admin/sync/scraped-jobs — import + standardize scraped jobs (admin). */
export function syncScrapedJobs(accessToken: string): Promise<SyncScrapedJobsResult> {
  return request<SyncScrapedJobsResult>(API_ENDPOINTS.sectors.syncScrapedJobs, {
    method: 'POST',
    headers: auth(accessToken),
  })
}

export interface ClassificationHistoryParams {
  page?: number
  pageSize?: number
  sectorId?: string
  method?: string
  todayOnly?: boolean
  date?: string
  search?: string
}

/** GET /api/admin/sectors/classification-history — paged job classification history (admin). */
export function fetchClassificationHistory(
  params: ClassificationHistoryParams = {},
  accessToken: string,
): Promise<ClassificationHistoryResponse> {
  const q = new URLSearchParams()
  if (params.page) q.set('page', String(params.page))
  if (params.pageSize) q.set('pageSize', String(params.pageSize))
  if (params.sectorId) q.set('sectorId', params.sectorId)
  if (params.method) q.set('method', params.method)
  if (params.todayOnly) q.set('todayOnly', 'true')
  if (params.date) q.set('date', params.date)
  if (params.search) q.set('search', params.search)

  const url = `${API_ENDPOINTS.sectors.classificationHistory}${q.toString() ? `?${q.toString()}` : ''}`
  return request<ClassificationHistoryResponse>(url, {
    headers: auth(accessToken),
  })
}

/** POST /api/admin/sectors/audit-classification — judge & audit job classifications with AI (admin). */
export function auditJobClassifications(
  payload: { jobIds?: string[]; auditTodayOnly?: boolean; limit?: number },
  accessToken: string,
): Promise<AuditReportResponse> {
  return request<AuditReportResponse>(API_ENDPOINTS.sectors.auditClassification, {
    method: 'POST',
    body: payload,
    headers: auth(accessToken),
  })
}

/** POST /api/admin/sectors/apply-audit-corrections — batch apply AI auditor corrections (admin). */
export function applyAuditCorrections(
  corrections: CorrectionRequestItem[],
  accessToken: string,
): Promise<{ updated: number; message: string }> {
  return request<{ updated: number; message: string }>(
    API_ENDPOINTS.sectors.applyAuditCorrections,
    {
      method: 'POST',
      body: { corrections },
      headers: auth(accessToken),
    },
  )
}

export interface ProviderHealthItem {
  name: string
  tier: string
  status: 'healthy' | 'warning' | 'degraded'
  jobsClassifiedToday: number
  failoversToday: number
  lastFailureReason?: string | null
}

export interface ProviderFallbackEvent {
  id: string
  occurredAt: string
  attemptedTier: string
  failedProvider: string
  httpStatusCode?: number | null
  errorCategory: string
  errorMessage: string
  fallbackTier: string
  jobCount: number
  wasRecovered: boolean
  jobTitlesSnippet?: string | null
}

export interface ProviderFallbacksResponse {
  date: string
  totalClassifiedToday: number
  eventsCount: number
  providers: ProviderHealthItem[]
  events: ProviderFallbackEvent[]
}

/** GET /api/admin/sectors/provider-fallbacks — real-time AI provider health and fallback logs. */
export function fetchProviderFallbacks(
  date?: string,
  accessToken?: string,
): Promise<ProviderFallbacksResponse> {
  const q = new URLSearchParams()
  if (date) q.set('date', date)
  const url = `${API_ENDPOINTS.sectors.providerFallbacks}${q.toString() ? `?${q.toString()}` : ''}`
  return request<ProviderFallbacksResponse>(url, {
    headers: accessToken ? auth(accessToken) : undefined,
  })
}

export interface TierMetric {
  tierName: string
  providerName: string
  jobsClassified: number
  jobsFailed: number
  successfulRuns: number
  failedRuns: number
  status: 'active' | 'degraded' | 'warning' | 'idle'
}

export interface DailyOperationalMetrics {
  totalClassified: number
  totalUnclassified: number
  averageConfidence: number
  totalInsertedOrSynced: number
  successRatePercentage: number
}

export interface ClassificationDashboardStatsResponse {
  date: string
  tier1OpenRouter: TierMetric
  tier2Groq: TierMetric
  tier3Gemini: TierMetric
  tier4FastPass: TierMetric
  tier5SafeFallback: TierMetric
  dailyMetrics: DailyOperationalMetrics
}

/** GET /api/admin/sectors/dashboard-stats — 5x2 dual-row metrics grid for any date. */
export function fetchClassificationDashboardStats(
  date?: string,
  accessToken?: string,
): Promise<ClassificationDashboardStatsResponse> {
  const q = new URLSearchParams()
  if (date) q.set('date', date)
  const url = `${API_ENDPOINTS.sectors.dashboardStats}${q.toString() ? `?${q.toString()}` : ''}`
  return request<ClassificationDashboardStatsResponse>(url, {
    headers: accessToken ? auth(accessToken) : undefined,
  })
}


