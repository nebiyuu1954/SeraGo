import { config } from '../config'
import {
  clearStoredAuthTokens,
  getStoredAuthTokens,
  refreshAccessToken,
  storeAuthTokens,
} from './auth'
import type { ApiErrorPayload, ApiResponse } from '../types'

/** Error thrown for non-2xx responses from the SeraGo API. */
export class ApiError extends Error {
  readonly status: number
  readonly payload?: ApiErrorPayload

  constructor(message: string, status: number, payload?: ApiErrorPayload) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.payload = payload
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  headers?: HeadersInit
  /** JSON body — serialized automatically. */
  body?: unknown
  signal?: AbortSignal
  /**
   * Cookie behavior. The Google OAuth flow relies on the backend-set
   * sign-in cookie, so those calls pass `credentials: 'include'`.
   * Defaults to the fetch default ('same-origin').
   */
  credentials?: RequestCredentials
}

/**
 * Single in-flight refresh: when several requests 401 at once (e.g. parallel
 * dashboard calls after the access token expired), they all share one refresh
 * instead of hammering the endpoint.
 */
let refreshInFlight: Promise<boolean> | null = null

async function refreshTokensOnce(): Promise<boolean> {
  // Already signed out (e.g. another tab) — nothing to refresh; a stale Bearer
  // header shouldn't trigger pointless round-trips.
  if (!getStoredAuthTokens()) return false
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null
  })
  return refreshInFlight
}

async function doRefresh(): Promise<boolean> {
  try {
    // The refresh token lives in the httpOnly Aufy.RefreshToken cookie (never
    // in localStorage), so `credentials: 'include'` is all that's needed.
    const tokens = await refreshAccessToken()
    storeAuthTokens(tokens)
    return true
  } catch {
    // Refresh failed (cookie expired/revoked) — the session is over.
    clearStoredAuthTokens()
    return false
  }
}

async function readResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  if (!text) {
    // Some endpoints (e.g. password forgot/reset) return 200 with an EMPTY
    // body (pre-envelope) — nothing to unwrap.
    return undefined as T
  }

  let envelope: ApiResponse<unknown>
  try {
    envelope = JSON.parse(text) as ApiResponse<unknown>
  } catch {
    // Non-JSON body — surface as an error for non-2xx, pass through otherwise.
    if (!response.ok) {
      throw new ApiError(`Request failed with status ${response.status}`, response.status)
    }
    return text as T
  }

  // Every API response carries the envelope; a non-2xx status or a "Failed"
  // status header means the call failed. Unwrap `data` on success.
  if (!response.ok || envelope.responseStatus === 'Failed') {
    throw new ApiError(
      envelope.message ?? `Request failed with status ${response.status}`,
      response.status,
      envelope as unknown as ApiErrorPayload,
    )
  }

  return envelope.data as T
}

/**
 * Typed fetch wrapper around the SeraGo API.
 * - Prepends the configured base URL
 * - Serializes JSON bodies
 * - Throws a typed `ApiError` on non-2xx responses
 * - Silently renews expired access tokens (15-min lifetime) via the httpOnly
 *   refresh cookie and retries the request once
 */
export async function request<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { headers: providedHeaders, body, ...rest } = options

  // Merge caller headers with a default JSON content type, tolerating
  // any valid HeadersInit (object, array, or Headers instance).
  const headers = new Headers(providedHeaders)
  if (body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const url = `${config.apiBaseUrl}${path}`
  const fetchOptions = {
    ...rest,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  }

  let response = await fetch(url, fetchOptions)

  // Only calls that carried a Bearer token get refreshed — login/sign-in
  // endpoints return 401 for bad credentials and must never trigger a refresh.
  if (
    response.status === 401 &&
    headers.has('Authorization') &&
    (await refreshTokensOnce())
  ) {
    const freshToken = getStoredAuthTokens()?.accessToken
    if (freshToken) {
      headers.set('Authorization', `Bearer ${freshToken}`)
      response = await fetch(url, { ...fetchOptions, headers })
    }
  }

  return readResponse<T>(response)
}

/**
 * Best-effort human-readable message for any thrown error. Failed responses
 * arrive as the envelope — `message` holds the human-readable text (the
 * middleware flattened ProblemDetails title/detail/field errors into it) and
 * `messageCode` is the stable code fallback.
 */
export function getApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const payload = error.payload
    return payload?.message ?? payload?.messageCode ?? error.message
  }
  if (error instanceof Error) return error.message
  return 'Something went wrong. Please try again.'
}
