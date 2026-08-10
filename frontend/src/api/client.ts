import { config } from '../config'
import type { ApiErrorPayload } from '../types'

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
 * Typed fetch wrapper around the SeraGo API.
 * - Prepends the configured base URL
 * - Serializes JSON bodies
 * - Throws a typed `ApiError` on non-2xx responses
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

  const response = await fetch(`${config.apiBaseUrl}${path}`, {
    ...rest,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    let payload: ApiErrorPayload | undefined
    try {
      payload = (await response.json()) as ApiErrorPayload
    } catch {
      // Non-JSON error body — leave payload undefined.
    }
    throw new ApiError(
      payload?.message ?? `Request failed with status ${response.status}`,
      response.status,
      payload,
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  // Some endpoints (e.g. password forgot/reset) return 200 with an EMPTY body.
  const text = await response.text()
  if (!text) {
    return undefined as T
  }
  return JSON.parse(text) as T
}

/**
 * Best-effort human-readable message for any thrown error — especially
 * Aufy's RFC 7807 ProblemDetails (`detail` / `title` / field `errors`).
 */
export function getApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const payload = error.payload
    if (payload?.errors) {
      const messages = Object.values(payload.errors).flat()
      if (messages.length > 0) return messages.join(' ')
    }
    return (
      payload?.detail ?? payload?.title ?? payload?.message ?? error.message
    )
  }
  if (error instanceof Error) return error.message
  return 'Something went wrong. Please try again.'
}
