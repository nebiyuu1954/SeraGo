import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  deleteJob,
  fetchJobs,
  getApiErrorMessage,
  getStoredAuthTokens,
  submitJob,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type {
  JobResponse,
  JobStatus,
  JobType,
  PaginationResponse,
} from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import {
  JOB_TYPE_OPTIONS,
  STATUS_OPTIONS,
} from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'

const PAGE_SIZE = 10

interface StatusBadge {
  label: string
  className: string
}

function statusBadge(job: JobResponse): StatusBadge {
  if (job.status === 'published') {
    return job.isActive
      ? {
          label: 'Published',
          className: 'bg-primary-fixed text-on-primary-fixed-variant',
        }
      : {
          label: 'Closed',
          className: 'bg-error-container text-on-error-container',
        }
  }
  switch (job.status) {
    case 'draft':
      return {
        label: 'Draft',
        className: 'bg-surface-container text-on-surface-variant',
      }
    case 'pendingApproval':
      return {
        label: 'Pending review',
        className: 'bg-amber-100 text-amber-900',
      }
    case 'rejected':
      return {
        label: 'Rejected',
        className: 'bg-error-container text-on-error-container',
      }
    default:
      return {
        label: 'Closed',
        className: 'bg-error-container text-on-error-container',
      }
  }
}

/**
 * Recruiter's post-login landing: their own job postings (the "Jobs" table,
 * with a separate Drafts view) with search, filters, delete, and pagination —
 * wired to the jobs API.
 */
export default function JobsPage() {
  const auth = useRequireRole('Recruiter')
  const navigate = useNavigate()
  const location = useLocation()

  const [searchInput, setSearchInput] = useState('')
  const [appliedQ, setAppliedQ] = useState('')
  const [jobType, setJobType] = useState<JobType | ''>('')
  const [status, setStatus] = useState<JobStatus | ''>('')
  const [locationFilter, setLocationFilter] = useState('')
  const [filterOpen, setFilterOpen] = useState(false)
  const [view, setView] = useState<'active' | 'drafts'>('active')
  // Draft copies of the filters edited inside the popover — nothing is
  // applied (or refetched) until the user hits Apply.
  const [draftJobType, setDraftJobType] = useState<JobType | ''>('')
  const [draftStatus, setDraftStatus] = useState<JobStatus | ''>('')
  const [draftLocation, setDraftLocation] = useState('')

  const [page, setPage] = useState(1)
  const [refreshKey, setRefreshKey] = useState(0)

  const [jobs, setJobs] = useState<JobResponse[]>([])
  const [pagination, setPagination] = useState<PaginationResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [toast, setToast] = useState<string | null>(
    (location.state as { toast?: string } | null)?.toast ?? null,
  )

  const filtersDirty =
    appliedQ !== '' ||
    jobType !== '' ||
    status !== '' ||
    locationFilter.trim() !== ''

  // Whether any filter (not search) is active — drives the button indicator.
  const filterActive =
    jobType !== '' || status !== '' || locationFilter.trim() !== ''

  // Auto-dismiss the success toast.
  useEffect(() => {
    if (!toast) return
    const t = setTimeout(() => setToast(null), 4000)
    return () => clearTimeout(t)
  }, [toast])

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
      setLoading(true)
      setError(null)
      try {
        const data = await fetchJobs(
          {
            mine: true,
            sort: 'newest',
            page,
            pageSize: PAGE_SIZE,
            q: appliedQ || undefined,
            jobType: jobType || undefined,
            status: view === 'drafts' ? 'draft' : status || undefined,
            location: locationFilter.trim() || undefined,
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
    view,
    page,
    appliedQ,
    jobType,
    status,
    locationFilter,
    refreshKey,
  ])

  const switchView = (next: 'active' | 'drafts') => {
    if (next === view) return
    setView(next)
    setPage(1)
    // The drafts view owns the status filter (always 'draft'); drop any
    // leftover status filter so the Clear/empty-state UI stays honest.
    if (next === 'drafts') setStatus('')
  }

  /** Soft delete on the active list; permanent (hard) delete for drafts. */
  const handleDelete = (job: JobResponse, hard: boolean) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    const message = hard
      ? `Delete the draft “${job.title}” permanently? This can't be undone.`
      : `Delete “${job.title}”? It will be hidden from the platform.`
    if (!window.confirm(message)) return
    deleteJob(job.id, tokens.accessToken, hard)
      .then(() => {
        setToast(hard ? 'Draft deleted permanently.' : 'Job deleted.')
        setRefreshKey((k) => k + 1)
      })
      .catch((err) => setError(getApiErrorMessage(err)))
  }

  /** Publish a draft — sends it to the admin review queue. */
  const handlePublish = (job: JobResponse) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    submitJob(job.id, tokens.accessToken)
      .then(() => {
        setToast('Job submitted for review.')
        setRefreshKey((k) => k + 1)
      })
      .catch((err) => setError(getApiErrorMessage(err)))
  }

  const toggleFilter = () => {
    if (filterOpen) {
      setFilterOpen(false)
      return
    }
    // Seed the popover with the currently applied filters.
    setDraftJobType(jobType)
    setDraftStatus(status)
    setDraftLocation(locationFilter)
    setFilterOpen(true)
  }

  const applyFilters = () => {
    setJobType(draftJobType)
    setStatus(draftStatus)
    setLocationFilter(draftLocation)
    setPage(1)
    setFilterOpen(false)
  }

  const clearFilters = () => {
    setSearchInput('')
    setAppliedQ('')
    setJobType('')
    setStatus('')
    setLocationFilter('')
    setDraftJobType('')
    setDraftStatus('')
    setDraftLocation('')
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
    <DashboardShell role="Recruiter" authUser={auth.user}>
      {/* Page header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
            {view === 'drafts' ? 'Drafts' : 'Jobs'}
          </h1>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            {view === 'drafts'
              ? 'Your saved drafts — publish when ready, or delete permanently.'
              : 'Manage and track your current job postings.'}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {/* View switcher — Drafts sits right next to Create Job */}
          <div className="flex items-center gap-1 rounded-lg border border-outline-variant bg-surface-container-low p-1">
            <button
              type="button"
              onClick={() => switchView('active')}
              className={cn(
                'rounded-md px-4 py-2 font-label-md text-label-md font-medium transition-colors',
                view === 'active'
                  ? 'bg-surface-container-lowest text-primary shadow-sm'
                  : 'text-on-surface-variant hover:text-on-surface',
              )}
            >
              Active Jobs
            </button>
            <button
              type="button"
              onClick={() => switchView('drafts')}
              className={cn(
                'flex items-center gap-1.5 rounded-md px-4 py-2 font-label-md text-label-md font-medium transition-colors',
                view === 'drafts'
                  ? 'bg-surface-container-lowest text-primary shadow-sm'
                  : 'text-on-surface-variant hover:text-on-surface',
              )}
            >
              <span className="material-symbols-outlined text-lg">
                description
              </span>
              Drafts
            </button>
          </div>
          <Link
            to="/dashboard/recruiter/jobs/new"
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90"
          >
            <span className="material-symbols-outlined text-xl">add</span>
            Create Job
          </Link>
        </div>
      </div>

      {/* Success toast */}
      {toast && (
        <div
          role="status"
          className="mt-6 flex items-center gap-2.5 rounded-xl border border-success/30 bg-surface-container-lowest px-4 py-3 font-label-md text-label-md text-success"
        >
          <span className="material-symbols-outlined text-lg">
            check_circle
          </span>
          {toast}
        </div>
      )}

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

      {/* Jobs card */}
      <div className="mt-6 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
        {/* Search + filter bar */}
        <div className="flex flex-col gap-3 border-b border-surface-variant bg-surface-bright p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="relative w-full max-w-md">
            <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">
              search
            </span>
            <input
              type="search"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Search job titles or locations..."
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
            <div className="relative">
              <button
                type="button"
                onClick={toggleFilter}
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
                      {view === 'active' && (
                        <div>
                          <label className="font-label-sm text-label-sm font-medium text-on-surface">
                            Status
                          </label>
                          <select
                            value={draftStatus}
                            onChange={(e) =>
                              setDraftStatus(e.target.value as JobStatus | '')
                            }
                            className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                          >
                            {STATUS_OPTIONS.map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                          </select>
                        </div>
                      )}
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

        {/* Table / states */}
        {loading ? (
          <SkeletonRows />
        ) : error ? null : jobs.length === 0 ? (
          <EmptyState
            hasFilters={filtersDirty}
            isDrafts={view === 'drafts'}
            onClear={clearFilters}
            onCreate={() => navigate('/dashboard/recruiter/jobs/new')}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="border-b border-surface-variant bg-surface-container-low">
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Job Title
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Company
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Location
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Status
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Applications
                  </th>
                  <th className="px-6 py-4 text-right font-label-md text-label-md font-semibold text-on-surface-variant">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-variant">
                {jobs.map((job) => {
                  const badge = statusBadge(job)
                  return (
                    <tr
                      key={job.id}
                      className="group transition-colors hover:bg-surface-container-low"
                    >
                      <td className="px-6 py-4">
                        <div className="font-body-md font-semibold text-on-surface">
                          {job.title}
                        </div>
                        <div className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
                          ID: {job.id.slice(0, 8).toUpperCase()}
                        </div>
                      </td>
                      <td className="px-6 py-4 font-body-md text-on-surface-variant">
                        {job.company || '—'}
                      </td>
                      <td className="px-6 py-4 font-body-md text-on-surface-variant">
                        {job.location || '—'}
                      </td>
                      <td className="px-6 py-4">
                        <span
                          className={cn(
                            'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium',
                            badge.className,
                          )}
                        >
                          {badge.label}
                        </span>
                      </td>
                      <td
                        className="px-6 py-4 font-body-md text-on-surface-variant"
                        title="Applications tracking is coming soon"
                      >
                        —
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center justify-end gap-1">
                          {view === 'drafts' && (
                            <button
                              type="button"
                              onClick={() => handlePublish(job)}
                              className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-primary-container hover:text-primary"
                              title="Publish"
                            >
                              <span className="material-symbols-outlined text-lg">
                                publish
                              </span>
                            </button>
                          )}
                          {job.status === 'published' ? (
                            <span
                              className="p-2 text-on-surface-variant/40"
                              title="Published jobs can't be edited"
                            >
                              <span className="material-symbols-outlined text-lg">
                                edit
                              </span>
                            </span>
                          ) : (
                            <Link
                              to={`/dashboard/recruiter/jobs/${job.id}/edit`}
                              className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container hover:text-primary"
                              title="Edit"
                            >
                              <span className="material-symbols-outlined text-lg">
                                edit
                              </span>
                            </Link>
                          )}
                          <button
                            type="button"
                            onClick={() => handleDelete(job, view === 'drafts')}
                            className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-error-container hover:text-error"
                            title={
                              view === 'drafts'
                                ? 'Delete permanently'
                                : 'Delete'
                            }
                          >
                            <span className="material-symbols-outlined text-lg">
                              delete
                            </span>
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination footer */}
        {pagination && pagination.totalCount > 0 && (
          <div className="flex flex-col gap-3 border-t border-surface-variant bg-surface-bright p-4 sm:flex-row sm:items-center sm:justify-between">
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
      </div>
    </DashboardShell>
  )
}

/* ------------------------------------------------------------- States */

function SkeletonRows() {
  return (
    <div className="divide-y divide-surface-variant">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-center gap-6 px-6 py-5">
          <div className="flex-1 space-y-2">
            <div className="h-4 w-1/3 animate-pulse rounded bg-surface-container" />
            <div className="h-3 w-1/4 animate-pulse rounded bg-surface-container-low" />
          </div>
          <div className="h-4 w-24 animate-pulse rounded bg-surface-container" />
          <div className="h-4 w-32 animate-pulse rounded bg-surface-container" />
          <div className="h-6 w-20 animate-pulse rounded-full bg-surface-container" />
          <div className="h-4 w-8 animate-pulse rounded bg-surface-container" />
          <div className="h-8 w-20 animate-pulse rounded bg-surface-container" />
        </div>
      ))}
    </div>
  )
}

function EmptyState({
  hasFilters,
  isDrafts,
  onClear,
  onCreate,
}: {
  hasFilters: boolean
  isDrafts: boolean
  onClear: () => void
  onCreate: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
        <span className="material-symbols-outlined text-3xl">
          {hasFilters ? 'filter_alt_off' : isDrafts ? 'drafts' : 'work_off'}
        </span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        {hasFilters
          ? isDrafts
            ? 'No matching drafts'
            : 'No matching jobs'
          : isDrafts
            ? 'No drafts yet'
            : 'No jobs yet'}
      </h2>
      <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
        {hasFilters
          ? 'Try adjusting your search or filters.'
          : isDrafts
            ? 'Save a job as a draft and finish it later.'
            : 'Create your first job posting and reach qualified talent.'}
      </p>
      <div className="mt-6 flex gap-3">
        {hasFilters ? (
          <button
            type="button"
            onClick={onClear}
            className="rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
          >
            Clear filters
          </button>
        ) : (
          <button
            type="button"
            onClick={onCreate}
            className="inline-flex items-center gap-2 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90"
          >
            <span className="material-symbols-outlined text-lg">add</span>
            Create Job
          </button>
        )}
      </div>
    </div>
  )
}
