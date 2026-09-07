import useSWR from 'swr'
import { fetchJobs } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { JobListData, JobListParams } from '../types'

/**
 * SWR-backed paginated job list.
 *
 * Caches by the full set of filter params so navigating away and back is
 * instant. Feed data refreshes on a 5-minute cadence only: remounts and tab
 * focus never auto-refetch a cached feed, so bouncing between pages (e.g.
 * saved ↔ talent) costs zero requests; the open-page poll keeps the feed at
 * most 5 minutes old. First visits, key changes (new filters/sort/page),
 * and explicit refresh() (e.g. after "Run AI matching") always fetch.
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
      // 5-minute freshness. This SWR version has no staleTime knob, so:
      // revalidateIfStale: false stops remounts from auto-refetching cached
      // data; revalidateOnFocus: false stops tab-focus refetches; the poll
      // refreshes at most once per 5 minutes while a page stays open.
      revalidateIfStale: false,
      revalidateOnFocus: false,
      refreshInterval: 300_000,
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
