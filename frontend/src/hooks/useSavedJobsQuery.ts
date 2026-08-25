import useSWR from 'swr'
import { fetchSavedJobs } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { SavedJobListData } from '../types'

/**
 * SWR-backed saved jobs list.
 *
 * Used by SavedJobsPage (shows full cards) and useSavedJobs hook (only
 * needs the id set). Both share the same cache entry — no duplicate requests.
 *
 * Mutate after save/unsave so the list and the bookmark buttons stay in sync.
 */
export function useSavedJobsQuery(enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.savedJobs.list : null

  const { data, error, isLoading, mutate } = useSWR<SavedJobListData>(
    key,
    () => fetchSavedJobs(tokens!.accessToken),
    {
      // Fetch on first mount (needed for bookmark buttons on login).
      // Serve from cache on subsequent mounts.
      revalidateOnFocus: true,
      refreshInterval: 30_000,
      shouldRetryOnError: false,
      dedupingInterval: 5_000,
    },
  )

  return {
    data: data ?? null,
    /** Quick Set of saved job ids — for the bookmark button on cards. */
    savedIds: new Set(
      (data?.items ?? [])
        .filter((item) => item.jobId !== null)
        .map((item) => item.jobId as string),
    ),
    isLoading,
    error: error as Error | undefined,
    /** Call after save/unsave to revalidate. */
    refresh: () => mutate(),
  }
}
