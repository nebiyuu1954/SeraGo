import useSWR, { mutate } from 'swr'
import { fetchJob } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { JobResponse } from '../types'

/**
 * SWR-backed single job detail.
 *
 * Caches each job by id so clicking into a detail page and back to the list
 * is instant. The cache is shared across all components that request the
 * same job id.
 */
export function useJobDetailQuery(jobId: string | undefined, enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens && jobId
    ? queryKeys.job(jobId)
    : null

  const { data, error, isLoading, mutate } = useSWR<JobResponse>(
    key,
    () => fetchJob(jobId!, tokens!.accessToken),
    {
      // Serve from cache instantly — zero network requests unless
      // the user explicitly retries or the prefetch on hover fires.
      // Job details rarely change, so don't refetch on mount or focus.
      revalidateOnMount: false,
      revalidateOnFocus: false,
      refreshInterval: 0,
      shouldRetryOnError: false,
      dedupingInterval: 30_000,
    },
  )

  return {
    job: data ?? null,
    isLoading,
    error: error as Error | undefined,
    refresh: () => mutate(),
  }
}

/**
 * Prefetch a job detail into the SWR cache. Call this on hover/focus of a
 * job card so the data is ready by the time the user clicks through.
 * If the data is already cached, this is a no-op (SWR dedupes).
 */
export function prefetchJob(jobId: string) {
  const tokens = getStoredAuthTokens()
  if (!tokens) return
  const key = queryKeys.job(jobId)
  // Global mutate — fetches and stores in cache, but doesn't revalidate
  // if data already exists (SWR dedupes identical in-flight requests).
  mutate(key, fetchJob(jobId, tokens.accessToken), { revalidate: false })
}
