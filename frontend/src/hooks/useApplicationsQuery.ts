import useSWR from 'swr'
import { fetchMyApplications } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { ApplicationListData } from '../types'

/**
 * SWR-backed query for the talent's own applications.
 */
export function useMyApplicationsQuery(
  enabled = true,
  page = 1,
  pageSize = 20,
  opts?: { status?: string; sort?: string; search?: string },
) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens
    ? queryKeys.applications.my(page, pageSize, opts?.status ?? '', opts?.sort ?? '', opts?.search ?? '')
    : null

  const { data, error, isLoading, mutate } = useSWR<ApplicationListData>(
    key,
    () => fetchMyApplications(tokens!.accessToken, page, pageSize, opts),
    {
      revalidateOnFocus: true,
      shouldRetryOnError: false,
      dedupingInterval: 5_000,
    },
  )

  return {
    applications: data?.items ?? [],
    pagination: data
      ? { page: data.page, pageSize: data.pageSize, totalCount: data.totalCount, totalPages: data.totalPages, hasNextPage: data.hasNextPage }
      : null,
    isLoading,
    error: error as Error | undefined,
    refresh: () => mutate(),
  }
}
