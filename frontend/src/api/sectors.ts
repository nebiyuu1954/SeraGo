import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type { SectorDetailResponse, SectorResponse, SyncScrapedJobsResult } from '../types'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/** GET /api/sectors — active sectors, for pickers (profile, filters). */
export function fetchSectors(accessToken: string): Promise<SectorResponse[]> {
  return request<SectorResponse[]>(API_ENDPOINTS.sectors.list, {
    headers: auth(accessToken),
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
