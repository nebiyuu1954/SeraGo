import useSWR from 'swr'
import { fetchMyApplications, fetchRecruiterJobStats, fetchAllRecruiterApplications, fetchJobApplications } from '../api'
import { queryKeys } from '../lib/queryKeys'
import { getStoredAuthTokens } from '../api'
import type { ApplicationListData } from '../types'

/**
 * SWR-backed query for the talent's own applications.
 */
export function useMyApplicationsQuery(enabled = true, page = 1) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens
    ? queryKeys.applications.my(page)
    : null

  const { data, error, isLoading, mutate } = useSWR<ApplicationListData>(
    key,
    () => fetchMyApplications(tokens!.accessToken, page),
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

/**
 * SWR-backed query for recruiter: per-job application counts + view counts.
 */
export function useRecruiterJobStatsQuery(enabled = true) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens ? queryKeys.recruiterJobStats : null

  const { data, error, isLoading, mutate } = useSWR(
    key,
    () => fetchRecruiterJobStats(tokens!.accessToken),
    {
      revalidateOnFocus: true,
      shouldRetryOnError: false,
      dedupingInterval: 5_000,
    },
  )

  return {
    stats: data ?? [],
    isLoading,
    error: error as Error | undefined,
    refresh: () => mutate(),
  }
}

/**
 * SWR-backed query for recruiter: all applications across their posted jobs.
 */
export function useRecruiterAllApplicationsQuery(
  enabled = true,
  page = 1,
  pageSize = 20,
  opts?: { status?: string; sort?: string; search?: string; jobId?: string },
) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens
    ? queryKeys.applications.recruiterAll(page, pageSize, opts?.status ?? '', opts?.sort ?? '', opts?.search ?? '', opts?.jobId ?? '')
    : null

  const { data, error, isLoading, mutate } = useSWR<ApplicationListData>(
    key,
    () => fetchAllRecruiterApplications(tokens!.accessToken, page, pageSize, opts),
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

/**
 * SWR-backed query for a recruiter's applications for a specific job.
 */
export function useJobApplicationsQuery(jobId: string | null, enabled = true, page = 1) {
  const tokens = getStoredAuthTokens()
  const key = enabled && tokens && jobId
    ? queryKeys.applications.job(jobId, page)
    : null

  const { data, error, isLoading, mutate } = useSWR<ApplicationListData>(
    key,
    () => fetchJobApplications(jobId!, tokens!.accessToken, page),
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
