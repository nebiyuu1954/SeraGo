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

  return (await response.json()) as T
}
