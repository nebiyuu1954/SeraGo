import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type { StatsTopResponse } from '../types'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/**
 * GET /api/admin/stats/top — top sectors + top websites for a period
 * (day | week | month | year), read from the scraper's persistent stats.
 * Admin only.
 */
export function fetchTopStats(
  accessToken: string,
  period: 'day' | 'week' | 'month' | 'year' = 'week',
  limit = 10,
): Promise<StatsTopResponse> {
  const params = new URLSearchParams({ period, limit: String(limit) })
  return request<StatsTopResponse>(`${API_ENDPOINTS.adminStats.top}?${params}`, {
    headers: auth(accessToken),
  })
}
