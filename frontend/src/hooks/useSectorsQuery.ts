import useSWR from 'swr'
import { fetchSectors } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { SectorResponse } from '../types'

/**
 * SWR-backed sector vocabulary.
 *
 * Sectors are admin-managed and change very rarely, so we cache aggressively:
 * 5-minute revalidation + long stale time. Multiple components (talent
 * JobsPage, admin SectorsPage, profile pages) share the same cache entry.
 */
export function useSectorsQuery(enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.sectors : null

  const { data, error, isLoading } = useSWR<SectorResponse[]>(
    key,
    () => fetchSectors(tokens!.accessToken),
    {
      // Sectors barely change — fetch on first mount, cache after that.
      revalidateOnFocus: false,
      refreshInterval: 300_000,
      dedupingInterval: 60_000,
    },
  )

  return {
    sectors: data ?? [],
    isLoading,
    error: error as Error | undefined,
  }
}
