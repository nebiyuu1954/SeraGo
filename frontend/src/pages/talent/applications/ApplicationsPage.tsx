import { useState, useRef, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getApiErrorMessage } from '../../../api'
import { useMyApplicationsQuery } from '../../../hooks/query.ts'
import { useRequireRole } from '../../../hooks'
import type { ApplicationResponse, ApplicationStatus } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { statusBadge, getChipClasses } from '../../../lib/statusBadge'


const PER_PAGE_OPTIONS = [10, 20, 50]

const STATUS_FILTERS: { value: ApplicationStatus | ''; label: string }[] = [
  { value: '', label: 'All' },
  { value: 'pending', label: 'Pending' },
  { value: 'reviewed', label: 'Reviewed' },
  { value: 'shortlisted', label: 'Shortlisted' },
  { value: 'interview', label: 'Interview' },
  { value: 'hired', label: 'Hired' },
  { value: 'rejected', label: 'Rejected' },
]

const SORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'newest', label: 'Newest first' },
  { value: 'oldest', label: 'Oldest first' },
]

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  })
}

function formatDeadline(iso: string): string {
  const date = new Date(iso)
  const now = new Date()
  const diffMs = date.getTime() - now.getTime()
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24))
  const formatted = formatDate(iso)
  if (diffDays <= 0) return `${formatted} (expired)`
  if (diffDays < 7) return `${formatted} (${diffDays} day${diffDays === 1 ? '' : 's'} left)`
  const weeks = Math.round(diffDays / 7)
  return `${formatted} (${weeks} week${weeks === 1 ? '' : 's'} left)`
}

function getPageNumbers(current: number, total: number): (number | '...')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)
  const pages: (number | '...')[] = [1]
  if (current > 3) pages.push('...')
  const start = Math.max(2, current - 1)
  const end = Math.min(total - 1, current + 1)
  for (let i = start; i <= end; i++) pages.push(i)
  if (current < total - 2) pages.push('...')
  pages.push(total)
  return pages
}

/**
 * Talent's applications page: shows all jobs the talent has applied to,
 * with search, status filters, list/grid views, sort, and pagination.
 */
export default function ApplicationsPage() {
  const auth = useRequireRole('Talent')

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

  return (
    <DashboardShell role="Talent" authUser={auth.user}>
      <div>
        <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
          My applications
        </h1>
        <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
          Track the status of your job applications.
        </p>
      </div>

      <ApplicationsList />
    </DashboardShell>
  )
}

// ─────────────────────── Applications List ────────────────────────

function ApplicationsList() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [statusFilter, setStatusFilter] = useState<ApplicationStatus | ''>('')
  const [sort, setSort] = useState('newest')
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('grid')
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)

  useEffect(() => {
    searchTimer.current = setTimeout(() => setDebouncedSearch(search), 400)
    return () => { if (searchTimer.current) clearTimeout(searchTimer.current) }
  }, [search])

  const filterOpts = {
    status: statusFilter || undefined,
    sort,
    search: debouncedSearch || undefined,
  }

  const { applications, pagination, isLoading, error, refresh } = useMyApplicationsQuery(
    true,
    page,
    pageSize,
    filterOpts,
  )

  return (
    <>
      {/* Filters */}
      <div className="mt-6 space-y-3">
        {/* Search */}
        <div className="relative">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-lg text-on-surface-variant">search</span>
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search by job name or company..."
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2.5 pl-10 pr-4 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
          {search && (
            <button type="button" onClick={() => { setSearch(''); setPage(1) }} className="absolute right-3 top-1/2 -translate-y-1/2 rounded p-1 text-on-surface-variant transition-colors hover:bg-surface-container">
              <span className="material-symbols-outlined text-lg">close</span>
            </button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {STATUS_FILTERS.map((filter) => (
            <button
              key={filter.value}
              type="button"
              onClick={() => { setStatusFilter(filter.value); setPage(1) }}
              className={`rounded-full px-4 py-1.5 font-label-md text-label-md font-medium transition-colors ${
                getChipClasses(filter.value, statusFilter === filter.value)
              }`}
            >
              {filter.label}
            </button>
          ))}

          <div className="flex-1" />

          <div className="flex items-center gap-2">
            {/* List / Grid toggle */}
            <div className="flex rounded-lg border border-outline-variant bg-surface-container-lowest p-0.5">
              <button
                type="button"
                onClick={() => setViewMode('list')}
                className={`flex items-center gap-1 rounded-md px-2.5 py-1.5 font-label-md text-label-md transition-colors ${viewMode === 'list' ? 'bg-primary text-on-primary' : 'text-on-surface-variant hover:text-on-surface'}`}
                title="List view"
              >
                <span className="material-symbols-outlined text-[18px]">view_list</span>
              </button>
              <button
                type="button"
                onClick={() => setViewMode('grid')}
                className={`flex items-center gap-1 rounded-md px-2.5 py-1.5 font-label-md text-label-md transition-colors ${viewMode === 'grid' ? 'bg-primary text-on-primary' : 'text-on-surface-variant hover:text-on-surface'}`}
                title="Grid view"
              >
                <span className="material-symbols-outlined text-[18px]">grid_view</span>
              </button>
            </div>
            <span className="material-symbols-outlined text-lg text-on-surface-variant">sort</span>
            <select value={sort} onChange={(e) => { setSort(e.target.value); setPage(1) }} className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              {SORT_OPTIONS.map((opt) => (<option key={opt.value} value={opt.value}>{opt.label}</option>))}
            </select>
          </div>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div
          role="alert"
          className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          <span className="flex items-center gap-2.5">
            <span className="material-symbols-outlined text-lg">error</span>
            <span>{getApiErrorMessage(error)}</span>
          </span>
          <button
            type="button"
            onClick={() => refresh()}
            className="rounded-lg border border-on-error-container/30 px-3 py-1.5 font-label-md text-label-md font-medium transition-colors hover:bg-on-error-container/10"
          >
            Retry
          </button>
        </div>
      )}

      {/* Content: list or grid */}
      {isLoading ? (
        viewMode === 'list' ? <ListSkeleton /> : <GridSkeleton />
      ) : error ? null : applications.length === 0 ? (
        <EmptyState hasFilters={!!statusFilter || !!debouncedSearch} />
      ) : viewMode === 'list' ? (
        <div className="mt-4 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="border-b border-surface-variant bg-surface-container-low">
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Job</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Company</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Location</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Status</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Applied</th>
                  <th className="px-6 py-4 text-right font-label-md text-label-md font-semibold text-on-surface-variant">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-variant">
                {applications.map((app) => (
                  <ApplicationRow key={app.id} application={app} />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {applications.map((app) => (
            <ApplicationCard key={app.id} application={app} />
          ))}
        </div>
      )}

      {/* Pagination */}
      {pagination && pagination.totalCount > 0 && (
        <div className="flex flex-wrap items-center justify-between gap-4 border-t border-surface-variant bg-surface-bright px-6 py-4">
          <div className="flex items-center gap-2">
            <label className="font-label-md text-label-md text-on-surface-variant">Show</label>
            <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1) }} className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              {PER_PAGE_OPTIONS.map((size) => (<option key={size} value={size}>{size}</option>))}
            </select>
            <span className="font-label-md text-label-md text-on-surface-variant">per page</span>
          </div>
          <span className="font-label-md text-label-md text-on-surface-variant">
            {pagination.totalCount} application{pagination.totalCount === 1 ? '' : 's'} &middot; Page {pagination.page} of {Math.max(pagination.totalPages, 1)}
          </span>
          <div className="flex items-center gap-2">
            <button type="button" disabled={page <= 1} onClick={() => setPage(1)} className="flex h-9 items-center gap-1 rounded-lg border border-outline-variant px-3 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40" title="First page">
              <span className="material-symbols-outlined text-lg">first_page</span>
            </button>
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))} className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
              <span className="material-symbols-outlined text-lg">chevron_left</span>
            </button>
            {getPageNumbers(pagination.page, pagination.totalPages).map((p, i) =>
              p === '...' ? (
                <span key={`e-${i}`} className="px-1 font-label-md text-label-md text-on-surface-variant">&hellip;</span>
              ) : (
                <button key={p} type="button" onClick={() => setPage(p as number)} className={`flex h-9 w-9 items-center justify-center rounded-lg font-label-md text-label-md transition-colors ${p === pagination.page ? 'bg-primary text-on-primary' : 'border border-outline-variant text-on-surface-variant hover:bg-surface-container-low'}`}>{p}</button>
              ),
            )}
            <button type="button" disabled={!pagination.hasNextPage} onClick={() => setPage((p) => p + 1)} className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
              <span className="material-symbols-outlined text-lg">chevron_right</span>
            </button>
            <button type="button" disabled={!pagination.hasNextPage} onClick={() => setPage(pagination.totalPages)} className="flex h-9 items-center gap-1 rounded-lg border border-outline-variant px-3 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40" title="Last page">
              <span className="material-symbols-outlined text-lg">last_page</span>
            </button>
          </div>
        </div>
      )}
    </>
  )
}

// ─────────────────────── List View Row ────────────────────────

function ApplicationRow({ application }: { application: ApplicationResponse }) {
  const badge = statusBadge(application.status)
  const navigate = useNavigate()

  return (
    <tr className="group transition-colors hover:bg-surface-container-low">
      <td className="px-6 py-4">
        <Link
          to={`/dashboard/talent/jobs/${application.jobId}`}
          className="font-body-md font-semibold text-primary transition-colors hover:text-surface-tint"
        >
          {application.jobTitle}
        </Link>
      </td>
      <td className="px-6 py-4 font-body-md text-on-surface-variant">
        {application.jobCompany || '—'}
      </td>
      <td className="px-6 py-4 font-body-md text-on-surface-variant">
        {application.jobLocation || '—'}
      </td>
      <td className="px-6 py-4">
        <span
          className={`inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium ${badge.className}`}
        >
          {badge.label}
        </span>
      </td>
      <td className="px-6 py-4 font-body-md text-on-surface-variant">
        {formatDate(application.appliedAt)}
      </td>
      <td className="px-6 py-4">
        <div className="flex items-center justify-end gap-1">
          <button
            type="button"
            onClick={() => navigate(`/dashboard/talent/applications/${application.id}`)}
            className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container hover:text-primary"
            title="View application details"
          >
            <span className="material-symbols-outlined text-lg">visibility</span>
          </button>
          <Link
            to={`/dashboard/talent/jobs/${application.jobId}`}
            className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container hover:text-primary"
            title="View job"
          >
            <span className="material-symbols-outlined text-lg">open_in_new</span>
          </Link>
        </div>
      </td>
    </tr>
  )
}

// ─────────────────────── Grid View Card ────────────────────────

/** Grid card view — job info focused: title, company, location, status, salary, applied date. */
function ApplicationCard({ application }: { application: ApplicationResponse }) {
  const badge = statusBadge(application.status)
  const navigate = useNavigate()

  return (
    <button
      type="button"
      onClick={() => navigate(`/dashboard/talent/applications/${application.id}`)}
      className="group flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest p-5 transition-all duration-300 hover:shadow-lg hover:border-accent hover:-translate-y-1 cursor-pointer text-left"
    >
      {/* Top row: status badge + job link */}
      <div className="flex items-center justify-between mb-3">
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium ${badge.className}`}>
          {badge.label}
        </span>
        <Link
          to={`/dashboard/talent/jobs/${application.jobId}`}
          onClick={(e) => e.stopPropagation()}
          className="inline-flex items-center gap-1 rounded-lg border border-outline-variant px-2.5 py-1 font-label-sm text-label-sm font-medium text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-primary"
        >
          <span className="material-symbols-outlined text-[14px]">open_in_new</span>
          Job
        </Link>
      </div>

      {/* Job title + Company with full-width horizontal bg */}
      <div className="-mx-5 -mt-1 mb-3">
        <div className={`${badge.bannerBg} px-8 py-3 flex items-center gap-3`}>
          {application.companyLogoUrl ? (
            <img src={application.companyLogoUrl} alt="" className="h-12 w-auto max-w-[80px] shrink-0 rounded-lg object-contain" />
          ) : (
            <div className="h-10 w-10 shrink-0 rounded-lg bg-primary/10 flex items-center justify-center">
              <span className="material-symbols-outlined text-lg text-primary">apartment</span>
            </div>
          )}
          <div className="min-w-0">
            <h3 className="font-headline-md text-headline-md text-on-surface truncate mb-1">
              {application.jobTitle}
            </h3>
            <p className="font-body-md text-on-surface-variant truncate">
              {application.jobCompany || '—'}
            </p>
          </div>
        </div>
      </div>

      {/* Meta info */}
      <div className="flex flex-col gap-2 mb-4">
        {application.jobLocation && (
          <span className="font-label-sm text-label-sm text-on-surface-variant flex items-center gap-1.5">
            <span className="material-symbols-outlined text-[14px]">location_on</span>
            {application.jobLocation}
          </span>
        )}
        {application.jobSalary && (
          <span className="font-label-sm text-label-sm text-on-surface-variant flex items-center gap-1.5">
            <span className="material-symbols-outlined text-[14px]">payments</span>
            {application.jobSalary}
          </span>
        )}
        <span className="font-label-sm text-label-sm text-on-surface-variant flex items-center gap-1.5">
          <span className="material-symbols-outlined text-[14px]">calendar_today</span>
          Applied {formatDate(application.appliedAt)}
        </span>
        {application.jobDeadline && (
          <span className="font-label-sm text-label-sm text-on-surface-variant flex items-center gap-1.5">
            <span className="material-symbols-outlined text-[14px]">timer</span>
            Expires {formatDeadline(application.jobDeadline)}
          </span>
        )}
        {application.jobSourceName && (
          <span className="font-label-sm text-label-sm text-on-surface-variant flex items-center gap-1.5">
            <span className="material-symbols-outlined text-[14px]">language</span>
            {application.jobSourceName}
          </span>
        )}
      </div>

      {/* View details — bottom right */}
      <div className="mt-auto flex justify-end border-t border-surface-variant pt-3">
        <span className="inline-flex items-center gap-1 rounded-lg bg-accent/10 px-3 py-1.5 font-label-md text-label-md font-semibold text-accent transition-colors group-hover:bg-accent group-hover:text-on-accent">
          View details
          <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
        </span>
      </div>
    </button>
  )
}

// ─────────────────────── Skeleton / Empty ────────────────────────

function ListSkeleton() {
  return (
    <div className="mt-4 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
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
            <div className="h-4 w-24 animate-pulse rounded bg-surface-container" />
            <div className="h-8 w-8 animate-pulse rounded bg-surface-container" />
          </div>
        ))}
      </div>
    </div>
  )
}

function GridSkeleton() {
  return (
    <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest p-5">
          <div className="flex items-center justify-between mb-3">
            <div className="h-5 w-20 animate-pulse rounded-full bg-surface-container" />
            <div className="h-7 w-14 animate-pulse rounded-lg bg-surface-container" />
          </div>
          <div className="h-5 w-2/3 animate-pulse rounded bg-surface-container mb-1" />
          <div className="h-4 w-1/3 animate-pulse rounded bg-surface-container-low mb-3" />
          <div className="flex flex-col gap-2 mb-4">
            <div className="h-3 w-1/2 animate-pulse rounded bg-surface-container-low" />
            <div className="h-3 w-1/3 animate-pulse rounded bg-surface-container-low" />
          </div>
          <div className="mt-auto flex justify-end border-t border-surface-variant pt-3">
            <div className="h-8 w-24 animate-pulse rounded-lg bg-surface-container-low" />
          </div>
        </div>
      ))}
    </div>
  )
}

function EmptyState({ hasFilters }: { hasFilters: boolean }) {
  return (
    <div className="mt-4 flex flex-col items-center justify-center rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
        <span className="material-symbols-outlined text-3xl">description</span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        {hasFilters ? 'No applications found' : 'No applications yet'}
      </h2>
      <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
        {hasFilters
          ? 'No applications match your filters.'
          : 'Browse Serago jobs and apply to start tracking your applications here.'}
      </p>
      {!hasFilters && (
        <Link
          to="/dashboard/talent"
          className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
        >
          Browse jobs
        </Link>
      )}
    </div>
  )
}
