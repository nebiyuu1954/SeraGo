import type { JobListParams } from '../types'

/**
 * Centralized SWR cache key factory.
 *
 * Every hook returns its key from here so:
 *  - Identical requests are deduped automatically
 *  - Mutations can invalidate related keys precisely
 *  - The devtools (if ever added) show a readable key hierarchy
 */
export const queryKeys = {
  /** All job-list queries, keyed by serialised filter params. */
  jobs: {
    /** Base key — mutate this to refetch all job lists. */
    all: ['jobs'] as const,
    /** Specific paginated/filtered list. */
    list: (params: JobListParams) =>
      ['jobs', 'list', params] as const,
  },

  /** Single job detail by id. */
  job: (id: string) => ['job', id] as const,

  /** The canonical sector vocabulary (changes rarely). */
  sectors: ['sectors'] as const,

  /** Distinct locations of the live feed. */
  jobLocations: ['jobLocations'] as const,

  /** Current user's profile (includes talent preferences). */
  profile: ['profile'] as const,

  /** Saved jobs list. */
  savedJobs: {
    all: ['savedJobs'] as const,
    list: ['savedJobs', 'list'] as const,
  },

  /** Job applications. */
  applications: {
    all: ['applications'] as const,
    /** Talent's own applications. */
    my: (page: number, pageSize: number, status: string, sort: string, search: string) =>
      ['applications', 'my', page, pageSize, status, sort, search] as const,
  },
} as const
