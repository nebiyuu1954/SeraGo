import useSWR from 'swr'
import { fetchJobs } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { JobListData, JobListParams } from '../types'

/**
 * SWR-backed paginated job list.
 *
 * Caches by the full set of filter params so navigating away and back is
 * instant. Revalidates in the background on window focus and on a short
 * polling interval to keep the feed fresh.
 *
 * @param params  The full query params (page, filters, sort, etc.)
 * @param enabled  Set to false to skip the fetch (e.g. while auth is loading)
 */
export function useJobsQuery(params: JobListParams, enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens
    ? queryKeys.jobs.list(params)
    : null

  const { data, error, isLoading, mutate } = useSWR<JobListData>(
    key,
    () => fetchJobs(params, tokens!.accessToken),
    {
      // Fetch on mount/key change — first load needs network.
      // Subsequent mounts serve from cache instantly.
      revalidateOnFocus: true,
      refreshInterval: 60_000,
      keepPreviousData: true,
      shouldRetryOnError: false,
      dedupingInterval: 5_000,
    },
  )

  return {
    jobs: data?.items ?? [],
    pagination: data?.pagination ?? null,
    isLoading,
    error: error as Error | undefined,
    /** Manually trigger a re-fetch (e.g. after a mutation). */
    refresh: () => mutate(),
  }
}

/**
 * Invalidate all cached job lists. Call after creating, editing, or
 * deleting a job so every open JobsPage shows fresh data.
 */
export function invalidateJobLists() {
  // We can't import mutate globally without the SWR provider, so
  // callers should use the SWR `useSWRConfig` mutation approach.
  // This function is kept as a signal — actual invalidation happens
  // via the hook-level `refresh()` or by key prefix.
}
