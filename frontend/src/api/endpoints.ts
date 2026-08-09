/**
 * Central registry of SeraGo API endpoint paths.
 * Add new endpoints here as the backend grows — never scatter paths
 * across feature code.
 */
export const API_ENDPOINTS = {
  health: '/health',
  // auth: { login: '/auth/login', register: '/auth/register' },
  // trips: '/trips',
} as const

export type ApiEndpoint = (typeof API_ENDPOINTS)[keyof typeof API_ENDPOINTS]
