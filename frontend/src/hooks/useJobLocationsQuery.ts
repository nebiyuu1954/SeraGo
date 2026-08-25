import useSWR from 'swr'
import { fetchJobLocations } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'

/**
 * SWR-backed distinct locations list.
 *
 * The set of locations changes slowly (new jobs = new locations), so a
 * 2-minute poll + focus revalidation keeps it fresh without waste.
 */
export function useJobLocationsQuery(enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.jobLocations : null

  const { data, error, isLoading } = useSWR<string[]>(
    key,
    () => fetchJobLocations(tokens!.accessToken),
    {
      // Fetch on first mount (needed for location filter on login).
      revalidateOnFocus: true,
      refreshInterval: 120_000,
      dedupingInterval: 30_000,
    },
  )

  return {
    locations: data ?? [],
    isLoading,
    error: error as Error | undefined,
  }
}
