import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import type { SettingsResponse } from '../types/settings.ts'

/**
 * GET /api/account/settings — fetch the current user's settings blob.
 * Returns { settings: string, version: number }.
 */
export function fetchSettings(accessToken: string): Promise<SettingsResponse> {
  return request<SettingsResponse>(API_ENDPOINTS.account.settings, {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * PUT /api/account/settings — full replace of the settings blob.
 * Requires the current version for optimistic concurrency.
 */
export function updateSettings(
  accessToken: string,
  settings: string,
  version: number,
): Promise<SettingsResponse> {
  return request<SettingsResponse>(API_ENDPOINTS.account.settings, {
    method: 'PUT',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: { settings, version },
  })
}

/**
 * PATCH /api/account/settings/{category} — partial update of a single category.
 * Merges the category value into the existing settings blob.
 */
export function patchSettingsCategory(
  accessToken: string,
  category: string,
  value: string,
): Promise<SettingsResponse> {
  return request<SettingsResponse>(`${API_ENDPOINTS.account.settings}/${category}`, {
    method: 'PATCH',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: { category, value },
  })
}
