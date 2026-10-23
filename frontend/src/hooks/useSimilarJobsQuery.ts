import useSWR from 'swr'
import { fetchSimilarJobs, getStoredAuthTokens } from '../api'
import { queryKeys } from '../lib/queryKeys'
import type { JobResponse } from '../types'

/**
 * SWR-backed hook for retrieving top 6 similar jobs in the same sector.
 *
 * Utilizes the backend's high-water mark vector similarity algorithm with
 * client-side deduplication and caching.
 */
export function useSimilarJobsQuery(jobId: string | undefined, enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && jobId ? queryKeys.similarJobs(jobId) : null

  const { data, error, isLoading, mutate } = useSWR<JobResponse[]>(
    key,
    () => fetchSimilarJobs(jobId!, tokens?.accessToken),
    {
      revalidateOnFocus: false,
      revalidateOnMount: true,
      refreshInterval: 0,
      dedupingInterval: 30_000,
      shouldRetryOnError: false,
    },
  )

  return {
    similarJobs: data ?? [],
    isLoading,
    error: error as Error | undefined,
    refresh: () => mutate(),
  }
}
