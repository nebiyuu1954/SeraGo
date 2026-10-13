import useSWR from 'swr'
import { fetchJobTypes } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { JobType, JobTypeCountResponse } from '../types'

import { FORM_JOB_TYPE_OPTIONS } from '../components/dashboard/jobOptions.ts'

/**
 * SWR-backed job type counts for filter dropdowns.
 *
 * Scoped by `forMe` so "For You" (candidate's followed sectors) and
 * "All jobs" (platform-wide) each get their own cached counts.
 */
export function useJobTypesQuery(forMe?: boolean, enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.jobTypes(forMe) : null

  const { data, error, isLoading } = useSWR<JobTypeCountResponse[]>(
    key,
    () => fetchJobTypes(forMe, tokens!.accessToken),
    {
      revalidateIfStale: false,
      revalidateOnFocus: false,
      refreshInterval: 300_000,
      dedupingInterval: 30_000,
    },
  )

  const counts: Record<JobType, number> | null = data
    ? FORM_JOB_TYPE_OPTIONS.reduce((acc, type) => {
        acc[type] = 0
        return acc
      }, {} as Record<JobType, number>)
    : null

  if (data && counts) {
    for (const item of data) {
      counts[item.jobType] = item.count
    }
  }

  return {
    counts,
    raw: data ?? [],
    isLoading,
    error: error as Error | undefined,
  }
}
