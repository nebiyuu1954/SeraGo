import useSWR from 'swr'
import { fetchProfile } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { ProfileResponse } from '../types'

/**
 * SWR-backed user profile.
 *
 * The profile is fetched by almost every page (to get the user's name,
 * avatar, preferences, etc.). With SWR it's fetched once and shared.
 * Mutate it after a profile update to keep the UI in sync.
 */
export function useProfileQuery(enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.profile : null

  const { data, error, isLoading, mutate } = useSWR<ProfileResponse>(
    key,
    () => fetchProfile(tokens!.accessToken),
    {
      // Fetch on first mount (needed for preferences on login).
      // Serve from cache on subsequent mounts.
      revalidateOnFocus: true,
      refreshInterval: 0,
      shouldRetryOnError: false,
      dedupingInterval: 10_000,
    },
  )

  return {
    profile: data ?? null,
    isLoading,
    error: error as Error | undefined,
    /** Call after a profile update to refresh the cache. */
    refresh: () => mutate(),
  }
}
