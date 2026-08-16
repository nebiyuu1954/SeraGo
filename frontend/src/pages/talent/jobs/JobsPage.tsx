import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import {
  fetchJobs,
  fetchProfile,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole, useSavedJobs } from '../../../hooks'
import type { JobResponse, JobType, PaginationResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import SaveJobButton from '../../../components/dashboard/SaveJobButton.tsx'
import {
  JOB_TYPE_LABELS,
  JOB_TYPE_OPTIONS,
  SORT_OPTIONS,
} from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'
import { formatDate, timeAgo } from '../../../lib/date.ts'
import { initialsOf } from '../../../lib/initials.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'

const PAGE_SIZE = 10

function jobTypeLabel(job: JobResponse): string {
  return JOB_TYPE_LABELS[job.jobType as JobType] ?? 'Other'
}

/** The talent's detail page for a job. */
function jobDetailPath(job: JobResponse): string {
  return `/dashboard/talent/jobs/${job.id}`
}

/**
 * Talent's post-login landing: browse every published job on the platform
 * with search, filters, sorting, and pagination — wired to the jobs API.
 */
export default function JobsPage() {
  const auth = useRequireRole('Talent')

  // View toggle, persisted in the URL: "For you" (preference-filtered feed,
  // the default) vs. "All jobs" (the classic browse).
  const [searchParams, setSearchParams] = useSearchParams()
  const view = searchParams.get('view') === 'all' ? 'all' : 'forYou'

  const [searchInput, setSearchInput] = useState('')
  const [appliedQ, setAppliedQ] = useState('')
  const [jobType, setJobType] = useState<JobType | ''>('')
  const [locationFilter, setLocationFilter] = useState('')
  const [sectorFilter, setSectorFilter] = useState('')
  const [sort, setSort] = useState('newest')
  const [filterOpen, setFilterOpen] = useState(false)
  // Draft copies of the filters edited inside the popover — nothing is
  // applied (or refetched) until the user hits Apply.
  const [draftJobType, setDraftJobType] = useState<JobType | ''>('')
  const [draftLocation, setDraftLocation] = useState('')
  const [draftSector, setDraftSector] = useState('')

  // The talent's preferred sector ids + the sector vocabulary (for the
  // All-jobs filter and the "set your preferences" prompt).
  const [prefSectorIds, setPrefSectorIds] = useState<string[]>([])
  const [sectors, setSectors] = useState<{ id: string; name: string }[]>([])

  const [page, setPage] = useState(1)
  const [refreshKey, setRefreshKey] = useState(0)

  const [jobs, setJobs] = useState<JobResponse[]>([])
  const [pagination, setPagination] = useState<PaginationResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { isSaved, toggleSaved } = useSavedJobs(auth.status === 'authenticated')

  // Load the talent's preferences + the sector vocabulary once.
  useEffect(() => {
    if (auth.status !== 'authenticated') return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    fetchProfile(tokens.accessToken)
      .then((p) => {
        if (!cancelled) setPrefSectorIds(p.talent?.preferredSectorIds ?? [])
      })
      .catch(() => {
        /* Non-fatal — the page still works, the prompt just won't know. */
      })
    fetchSectors(tokens.accessToken)
      .then((list) => {
        if (!cancelled) setSectors(list)
      })
      .catch(() => {
        /* Non-fatal — the All-jobs sector filter just stays empty. */
      })
    return () => {
      cancelled = true
    }
  }, [auth.status])

  const filtersDirty =
    appliedQ !== '' ||
    jobType !== '' ||
    locationFilter.trim() !== '' ||
    (view === 'all' && sectorFilter !== '')

  // Whether any filter (not search/sort) is active — drives the button dot.
  const filterActive =
    jobType !== '' ||
    locationFilter.trim() !== '' ||
    (view === 'all' && sectorFilter !== '')

  // Debounced search input → applied query. A new query always starts at
  // page 1, so both are updated together inside the debounce callback.
  useEffect(() => {
    const t = setTimeout(() => {
      setAppliedQ(searchInput.trim())
      setPage(1)
    }, 350)
    return () => clearTimeout(t)
  }, [searchInput])

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens || auth.status !== 'authenticated') return
    let cancelled = false
    const load = async () => {
      // "For you" with no preferences yet → show the setup prompt, no fetch.
      if (view === 'forYou' && prefSectorIds.length === 0) {
        if (!cancelled) {
          setJobs([])
          setPagination(null)
          setLoading(false)
        }
        return
      }
      setLoading(true)
      setError(null)
      try {
        // forMe → the API narrows to the caller's preferred sectors.
        const data = await fetchJobs(
          {
            sort,
            page,
            pageSize: PAGE_SIZE,
            q: appliedQ || undefined,
            jobType: jobType || undefined,
            location: locationFilter.trim() || undefined,
            forMe: view === 'forYou' ? true : undefined,
            sectorId: view === 'all' && sectorFilter ? sectorFilter : undefined,
          },
          tokens.accessToken,
        )
        if (!cancelled) {
          setJobs(data.items)
          setPagination(data.pagination)
        }
      } catch (err) {
        if (!cancelled) setError(getApiErrorMessage(err))
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [
    auth.status,
    page,
    appliedQ,
    jobType,
    locationFilter,
    sectorFilter,
    sort,
    refreshKey,
    view,
    prefSectorIds,
  ])

  const setView = (next: 'forYou' | 'all') => {
    setSearchParams(next === 'forYou' ? {} : { view: 'all' }, { replace: true })
    setPage(1)
  }

  const applyFilters = () => {
    setJobType(draftJobType)
    setLocationFilter(draftLocation)
    setSectorFilter(draftSector)
    setPage(1)
    setFilterOpen(false)
  }

  const clearFilters = () => {
    setSearchInput('')
    setAppliedQ('')
    setJobType('')
    setLocationFilter('')
    setSectorFilter('')
    setDraftJobType('')
    setDraftLocation('')
    setDraftSector('')
    setPage(1)
  }

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span
          aria-hidden="true"
          className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
        />
      </div>
    )
  }

  const from =
    pagination && pagination.totalCount > 0
      ? (page - 1) * pagination.pageSize + 1
      : 0
  const to = pagination
    ? Math.min(page * pagination.pageSize, pagination.totalCount)
    : 0

  return (
    <DashboardShell role="Talent" authUser={auth.user}>
      {/* Page header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
            Find jobs
          </h1>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            {view === 'forYou'
              ? 'Jobs matched to your preferred sectors — nothing you’d scroll past.'
              : 'Browse every open position on the platform — search, filter, and apply.'}
          </p>
        </div>
      </div>

      {/* For you / All jobs toggle */}
      <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex rounded-lg border border-surface-variant bg-surface-container-lowest p-1 shadow-sm">
          <button
            type="button"
            onClick={() => setView('forYou')}
            aria-pressed={view === 'forYou'}
            className={cn(
              'flex items-center gap-1.5 rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
              view === 'forYou'
                ? 'bg-primary font-medium text-on-primary'
                : 'text-on-surface-variant hover:text-on-surface',
            )}
          >
            <span className="material-symbols-outlined text-lg">
              auto_awesome
            </span>
            For you
          </button>
          <button
            type="button"
            onClick={() => setView('all')}
            aria-pressed={view === 'all'}
            className={cn(
              'flex items-center gap-1.5 rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
              view === 'all'
                ? 'bg-primary font-medium text-on-primary'
                : 'text-on-surface-variant hover:text-on-surface',
            )}
          >
            <span className="material-symbols-outlined text-lg">
              format_list_bulleted
            </span>
            All jobs
          </button>
        </div>
        {view === 'forYou' && prefSectorIds.length > 0 && (
          <p className="font-label-sm text-label-sm text-on-surface-variant">
            Filtered to your {prefSectorIds.length} preferred sector
            {prefSectorIds.length === 1 ? '' : 's'} —{' '}
            <Link
              to="/dashboard/talent/profile"
              className="text-primary transition-colors hover:text-surface-tint"
            >
              adjust preferences
            </Link>
          </p>
        )}
      </div>

      {/* Error banner */}
      {error && (
        <div
          role="alert"
          className="mt-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          <span className="flex items-center gap-2.5">
            <span className="material-symbols-outlined text-lg">error</span>
            <span>{error}</span>
          </span>
          <button
            type="button"
            onClick={() => setRefreshKey((k) => k + 1)}
            className="rounded-lg border border-on-error-container/30 px-3 py-1.5 font-label-md text-label-md font-medium transition-colors hover:bg-on-error-container/10"
          >
            Retry
          </button>
        </div>
      )}

      {/* Search + filter toolbar */}
      <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center">
        <div className="relative w-full flex-1">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">
            search
          </span>
          <input
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search job titles, companies, or keywords..."
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 pl-10 pr-4 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex items-center gap-3">
          {filtersDirty && (
            <button
              type="button"
              onClick={clearFilters}
              className="font-label-md text-label-md text-on-surface-variant transition-colors hover:text-on-surface"
            >
              Clear
            </button>
          )}
          <select
            value={sort}
            onChange={(e) => {
              setSort(e.target.value)
              setPage(1)
            }}
            aria-label="Sort jobs"
            className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            {SORT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <div className="relative">
            <button
              type="button"
              onClick={() => {
                if (filterOpen) {
                  setFilterOpen(false)
                  return
                }
                // Seed the popover with the currently applied filters.
                setDraftJobType(jobType)
                setDraftLocation(locationFilter)
                setDraftSector(sectorFilter)
                setFilterOpen(true)
              }}
              aria-expanded={filterOpen}
              className={cn(
                'flex items-center gap-2 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md transition-colors hover:bg-surface-container-low',
                (filterOpen || filterActive) && 'border-primary text-primary',
              )}
            >
              <span className="material-symbols-outlined text-lg">
                filter_list
              </span>
              Filter
              {filterActive && (
                <span
                  aria-hidden="true"
                  className="h-2 w-2 rounded-full bg-accent"
                />
              )}
            </button>
            {filterOpen && (
              <>
                <div
                  className="fixed inset-0 z-10"
                  onClick={() => setFilterOpen(false)}
                />
                <div className="absolute right-0 z-20 mt-2 w-72 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-lg">
                  <div className="space-y-4">
                    <div>
                      <label className="font-label-sm text-label-sm font-medium text-on-surface">
                        Job type
                      </label>
                      <select
                        value={draftJobType}
                        onChange={(e) =>
                          setDraftJobType(e.target.value as JobType | '')
                        }
                        className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                      >
                        {JOB_TYPE_OPTIONS.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="font-label-sm text-label-sm font-medium text-on-surface">
                        Location
                      </label>
                      <input
                        type="text"
                        value={draftLocation}
                        onChange={(e) => setDraftLocation(e.target.value)}
                        placeholder="City, region..."
                        className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                      />
                    </div>
                    {view === 'all' && (
                      <div>
                        <label className="font-label-sm text-label-sm font-medium text-on-surface">
                          Sector
                        </label>
                        <select
                          value={draftSector}
                          onChange={(e) => setDraftSector(e.target.value)}
                          className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                        >
                          <option value="">All sectors</option>
                          {sectors.map((sector) => (
                            <option key={sector.id} value={sector.id}>
                              {sector.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                  </div>
                  <div className="mt-4 flex justify-end gap-2">
                    <button
                      type="button"
                      onClick={clearFilters}
                      className="rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                    >
                      Clear
                    </button>
                    <button
                      type="button"
                      onClick={applyFilters}
                      className="rounded-lg bg-primary px-4 py-2 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
                    >
                      Apply
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Result count */}
      {!loading && !error && pagination && (
        <p className="mt-5 font-label-md text-label-md text-on-surface-variant">
          {pagination.totalCount === 0
            ? 'No jobs found'
            : `${pagination.totalCount} ${
                pagination.totalCount === 1 ? 'job' : 'jobs'
              } ${view === 'forYou' ? 'matched to your preferences' : 'available'}`}
        </p>
      )}

      {/* Job cards / states */}
      {view === 'forYou' && prefSectorIds.length === 0 ? (
        <PreferencesPrompt onBrowseAll={() => setView('all')} />
      ) : loading ? (
        <SkeletonGrid />
      ) : error ? null : jobs.length === 0 ? (
        <EmptyState
          hasFilters={filtersDirty}
          onClear={clearFilters}
          forYou={view === 'forYou'}
        />
      ) : (
        <div className="mt-5 grid grid-cols-1 gap-6 xl:grid-cols-2">
          {jobs.map((job) => (
            <JobCard
              key={job.id}
              job={job}
              saved={isSaved(job.id)}
              onToggleSave={() => toggleSaved(job.id)}
            />
          ))}
        </div>
      )}

      {/* Pagination footer */}
      {pagination && pagination.totalCount > 0 && (
        <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <span className="font-label-md text-label-md text-on-surface-variant">
            Showing {from} to {to} of {pagination.totalCount} results
          </span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              aria-label="Previous page"
              className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40"
            >
              <span className="material-symbols-outlined text-lg">
                chevron_left
              </span>
            </button>
            <span className="font-label-md text-label-md text-on-surface-variant">
              Page {pagination.page} of {Math.max(pagination.totalPages, 1)}
            </span>
            <button
              type="button"
              disabled={!pagination.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              aria-label="Next page"
              className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40"
            >
              <span className="material-symbols-outlined text-lg">
                chevron_right
              </span>
            </button>
          </div>
        </div>
      )}
    </DashboardShell>
  )
}

/* --------------------------------------------------------------- Card */

function JobCard({
  job,
  saved,
  onToggleSave,
}: {
  job: JobResponse
  saved: boolean
  onToggleSave: () => void
}) {
  const brandLogo = sourceLogo(job.sourceName)
  const meta = [
    {
      icon: 'location_on',
      label: job.location || 'Location not specified',
    },
    { icon: 'payments', label: job.salary || 'Salary not specified' },
    { icon: 'schedule', label: jobTypeLabel(job) },
    ...(job.experienceLevel
      ? [{ icon: 'work', label: job.experienceLevel }]
      : []),
    ...(job.sectorName ? [{ icon: 'domain', label: job.sectorName }] : []),
    {
      icon: 'history',
      label: `Posted ${timeAgo(job.publishedAt) || 'recently'}`,
    },
    ...(job.deadline
      ? [{ icon: 'event', label: `Closes ${formatDate(job.deadline)}` }]
      : []),
  ]

  return (
    <article className="group flex flex-col overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest transition-all duration-300 hover:border-outline-variant hover:shadow-sm">
      {/* Header: company mark + title (mirrors the landing page cards) */}
      <div className="flex flex-grow flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
        <div className="flex h-12 w-28 shrink-0 items-center justify-center overflow-hidden rounded bg-surface-container-low px-2">
          {brandLogo ? (
            <img
              className="max-h-8 w-auto max-w-full object-contain"
              src={brandLogo}
              alt={job.sourceName ?? ''}
            />
          ) : job.companyLogoUrl ? (
            <img
              className="max-h-8 w-auto max-w-full object-contain"
              src={job.companyLogoUrl}
              alt=""
            />
          ) : job.company ? (
            <span className="font-headline-md text-headline-md font-semibold text-primary">
              {initialsOf(job.company)}
            </span>
          ) : (
            <span className="material-symbols-outlined text-2xl text-on-surface-variant">
              work
            </span>
          )}
        </div>
        <div className="min-w-0">
          <h4 className="mb-1 truncate font-headline-md text-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint">
            {job.title}
          </h4>
          <p className="truncate font-body-md text-body-md text-secondary">
            {job.company || 'Company undisclosed'}
          </p>
          {job.sourceName &&
            job.sourceName.toLowerCase() !== 'serago' &&
            (job.sourceUrl ? (
              <a
                href={job.sourceUrl}
                target="_blank"
                rel="noreferrer"
                className="mt-0.5 inline-flex items-center gap-1 font-label-sm text-label-sm text-primary transition-colors hover:text-surface-tint"
              >
                via {job.sourceName}
                <span className="material-symbols-outlined text-sm">
                  open_in_new
                </span>
              </a>
            ) : (
              <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                via {job.sourceName}
              </p>
            ))}
        </div>
      </div>

      {/* Meta grid + actions */}
      <div className="flex flex-col gap-5 bg-surface/30 p-6">
        <div className="grid grid-cols-2 gap-3 font-label-md text-label-md text-on-surface-variant">
          {meta.map((item) => (
            <div key={item.icon} className="flex items-center gap-2">
              <span className="material-symbols-outlined text-[18px]">
                {item.icon}
              </span>
              <span className="truncate">{item.label}</span>
            </div>
          ))}
        </div>
        <div className="mt-auto flex gap-2">
          {job.url ? (
            <a
              href={job.url}
              target="_blank"
              rel="noreferrer"
              className="flex-1 rounded bg-accent px-4 py-2.5 text-center font-label-md text-label-md text-on-accent transition-colors hover:opacity-90"
            >
              Apply Now
            </a>
          ) : (
            <span
              title="No application link provided"
              className="flex-1 cursor-not-allowed rounded bg-accent px-4 py-2.5 text-center font-label-md text-label-md text-on-accent opacity-60"
            >
              Apply Now
            </span>
          )}
          <Link
            to={jobDetailPath(job)}
            className="flex-1 rounded border border-surface-variant px-4 py-2.5 text-center font-label-md text-label-md text-primary transition-colors hover:bg-surface-container-low"
          >
            See description
          </Link>
          <SaveJobButton saved={saved} onToggle={onToggleSave} />
        </div>
      </div>
    </article>
  )
}

/* ------------------------------------------------------------- States */

function SkeletonGrid() {
  return (
    <div className="mt-5 grid grid-cols-1 gap-6 xl:grid-cols-2">
      {Array.from({ length: PAGE_SIZE }).map((_, i) => (
        <div
          key={i}
          className="overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest"
        >
          <div className="flex flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
            <div className="h-12 w-28 shrink-0 animate-pulse rounded bg-surface-container-low" />
            <div className="flex-1 space-y-2">
              <div className="h-4 w-3/4 animate-pulse rounded bg-surface-container" />
              <div className="h-3 w-1/3 animate-pulse rounded bg-surface-container-low" />
            </div>
          </div>
          <div className="space-y-4 bg-surface/30 p-6">
            <div className="grid grid-cols-2 gap-3">
              {Array.from({ length: 4 }).map((_, j) => (
                <div
                  key={j}
                  className="h-3 animate-pulse rounded bg-surface-container-low"
                />
              ))}
            </div>
            <div className="flex gap-2">
              <div className="h-10 flex-1 animate-pulse rounded bg-surface-container" />
              <div className="h-10 flex-1 animate-pulse rounded bg-surface-container" />
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

function EmptyState({
  hasFilters,
  onClear,
  forYou = false,
}: {
  hasFilters: boolean
  onClear: () => void
  /** Personalized-feed variant — suggests tuning preferences instead of filters. */
  forYou?: boolean
}) {
  return (
    <div className="mt-5 flex flex-col items-center justify-center rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-16 text-center shadow-sm">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
        <span className="material-symbols-outlined text-3xl">
          {forYou ? 'auto_awesome' : hasFilters ? 'filter_alt_off' : 'search_off'}
        </span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        {forYou
          ? 'No jobs match your preferences yet'
          : hasFilters
            ? 'No matching jobs'
            : 'No jobs yet'}
      </h2>
      <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
        {forYou
          ? 'Try widening your preferred sectors, or browse everything.'
          : hasFilters
            ? 'Try adjusting your search or filters.'
            : 'There are no open positions right now — check back soon.'}
      </p>
      {forYou ? (
        <Link
          to="/dashboard/talent/profile"
          className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
        >
          Adjust preferences
        </Link>
      ) : hasFilters ? (
        <button
          type="button"
          onClick={onClear}
          className="mt-6 rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
        >
          Clear filters
        </button>
      ) : null}
    </div>
  )
}

/** Shown on "For you" when the talent hasn't set any preferences yet. */
function PreferencesPrompt({ onBrowseAll }: { onBrowseAll: () => void }) {
  return (
    <div className="mt-5 flex flex-col items-center justify-center rounded-xl border border-primary/20 bg-primary-container/20 px-6 py-16 text-center shadow-sm">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary">
        <span className="material-symbols-outlined text-3xl">auto_awesome</span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        Tailor your feed to what you do
      </h2>
      <p className="mt-1 max-w-md font-body-md text-body-md text-on-surface-variant">
        Tell us the sectors you work in and SeraGo will only show you jobs that
        matter — no more scrolling past roles you’d never apply for.
      </p>
      <div className="mt-6 flex flex-wrap justify-center gap-3">
        <Link
          to="/dashboard/talent/profile"
          className="rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
        >
          Set my preferences
        </Link>
        <button
          type="button"
          onClick={onBrowseAll}
          className="rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
        >
          Browse all jobs
        </button>
      </div>
    </div>
  )
}
