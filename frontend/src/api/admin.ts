import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type { AdminUserResponse, AdminUserListData, AdminStatsTopResponse } from '../types'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

// ---- Users ----

/** GET /api/admin/users — list all users (admin). */
export function fetchAdminUsers(
  params: { q?: string; role?: string; isActive?: boolean; page?: number; pageSize?: number },
  accessToken: string,
): Promise<AdminUserListData> {
  const query = new URLSearchParams()
  if (params.q) query.set('q', params.q)
  if (params.role) query.set('role', params.role)
  if (params.isActive !== undefined) query.set('isActive', String(params.isActive))
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  const qs = query.toString()
  return request<AdminUserListData>(
    `${API_ENDPOINTS.adminUsers.list}${qs ? `?${qs}` : ''}`,
    { headers: auth(accessToken) },
  )
}

/** PATCH /api/admin/users/{id}/role — change a user's role (admin). */
export function updateUserRole(
  userId: string,
  role: string,
  accessToken: string,
): Promise<AdminUserResponse> {
  return request<AdminUserResponse>(
    `${API_ENDPOINTS.adminUsers.detail(userId)}/role`,
    { method: 'PATCH', body: { role }, headers: auth(accessToken) },
  )
}

/** PATCH /api/admin/users/{id}/status — activate/deactivate (admin). */
export function updateUserStatus(
  userId: string,
  isActive: boolean,
  accessToken: string,
): Promise<AdminUserResponse> {
  return request<AdminUserResponse>(
    `${API_ENDPOINTS.adminUsers.detail(userId)}/status`,
    { method: 'PATCH', body: { isActive }, headers: auth(accessToken) },
  )
}

// ---- Stats ----

/** GET /api/admin/stats/top — scraper stats (admin). */
export function fetchAdminStats(
  period: string,
  accessToken: string,
): Promise<AdminStatsTopResponse> {
  return request<AdminStatsTopResponse>(
    `${API_ENDPOINTS.adminStats.top}?period=${period}`,
    { headers: auth(accessToken) },
  )
}
