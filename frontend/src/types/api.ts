/** Standard envelope returned by the SeraGo API. */
export interface ApiResponse<T> {
  data: T
  message?: string
}

/** Shape of an error payload returned by the SeraGo API. */
export interface ApiErrorPayload {
  message?: string
  statusCode?: number
}

/** Example health-check response. */
export interface HealthResponse {
  status: 'ok' | 'degraded'
  timestamp: string
}
