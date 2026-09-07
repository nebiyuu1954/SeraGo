import useSWR from 'swr'
import { fetchJobLocations } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'

/**
 * SWR-backed distinct locations list.
 *
 * The set of locations changes slowly (new jobs = new locations), so
 * fetched lists are never auto-refetched on remount/focus; the open-page
 * poll refreshes at most every 5 minutes.
 */
export function useJobLocationsQuery(enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.jobLocations : null

  const { data, error, isLoading } = useSWR<string[]>(
    key,
    () => fetchJobLocations(tokens!.accessToken),
    {
      // Same 5-minute policy as the job feed: no auto-refetch on remount or
      // focus; open pages refresh at most every 5 minutes.
      revalidateIfStale: false,
      revalidateOnFocus: false,
      refreshInterval: 300_000,
      dedupingInterval: 30_000,
    },
  )

  return {
    locations: data ?? [],
    isLoading,
    error: error as Error | undefined,
  }
}
