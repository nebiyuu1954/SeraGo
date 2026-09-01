import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getApiErrorMessage,
  getStoredAuthTokens,
  unsaveJob,
} from '../../../api'
import { useSavedJobsQuery } from '../../../hooks/query.ts'
import { prefetchJob } from '../../../hooks/useJobDetailQuery.ts'
import { useRequireRole } from '../../../hooks'
import type { SavedJobItem } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import SaveJobButton from '../../../components/dashboard/SaveJobButton.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import { PAGE_SIZE_OPTIONS } from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'
import { formatDate } from '../../../lib/date.ts'
import { initialsOf } from '../../../lib/initials.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'

// ---------------------------------------------------------------- Helpers

const DAY_MS = 86_400_000

/** Whole days until an ISO date, floor at 1. */
function daysUntil(iso: string | null): number | null {
  if (!iso) return null
  const ms = new Date(iso).getTime() - Date.now()
  return Math.max(1, Math.ceil(ms / DAY_MS))
}

function StatusBadge({ item }: { item: SavedJobItem }) {
  if (item.status === 'removed') {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-error-container px-3 py-1 font-label-sm text-label-sm font-medium text-on-error-container">
        <span className="material-symbols-outlined text-base">block</span>
        No longer available
      </span>
    )
  }
  if (item.status === 'deadlinePassed') {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-tertiary-container/70 px-3 py-1 font-label-sm text-label-sm font-medium text-on-tertiary-container">
        <span className="material-symbols-outlined text-base">schedule</span>
        Deadline passed
      </span>
    )
  }
  return null
}

function Countdown({ item }: { item: SavedJobItem }) {
  if (item.status === 'open') return null
  const days = daysUntil(item.removedAt)
  if (days === null) return null
  return (
    <p className="flex items-center gap-1.5 font-label-sm text-label-sm text-on-surface-variant">
      <span className="material-symbols-outlined text-base">hourglass_top</span>
      Removed from your saved list in {days} day{days === 1 ? '' : 's'}
    </p>
  )
}

// ---------------------------------------------------------------- Page

/**
 * Talent's saved jobs — every job the user bookmarked, with the same card
 * layout as the main jobs feed. Includes search, sort, per-page, and
 * pagination (all client-side since the API returns the full list).
 */
export default function SavedJobsPage() {
  const auth = useRequireRole('Talent')

  const {
    data,
    isLoading,
    error,
    refresh: refreshSaved,
  } = useSavedJobsQuery(auth.status === 'authenticated')
  const { showToast } = useToast()

  const items = data?.items ?? []
  const totalSaved = data?.totalSaved ?? 0
  const affectedCount = data?.affectedCount ?? 0

  // --- Search / sort / pagination state (all client-side) ---
  const [searchInput, setSearchInput] = useState('')
  const [sort, setSort] = useState('newest_saved')
  const [pageSize, setPageSize] = useState(10)
  const [page, setPage] = useState(1)

  // Reset to page 1 when search, sort, or page size changes
  useEffect(() => {
    setPage(1)
  }, [searchInput, sort, pageSize])

  const handleUnsave = useCallback(
    async (item: SavedJobItem) => {
      const tokens = getStoredAuthTokens()
      if (!tokens || !item.jobId) return
      try {
        await unsaveJob(item.jobId, tokens.accessToken)
        showToast('Removed from saved jobs')
        refreshSaved()
      } catch {
        showToast('Could not remove this job', 'error')
      }
    },
    [refreshSaved, showToast],
  )

  // --- Apply search ---
  const filtered = useMemo(() => {
    const q = searchInput.trim().toLowerCase()
    if (!q) return items
    return items.filter(
      (item) =>
        item.title.toLowerCase().includes(q) ||
        (item.company || '').toLowerCase().includes(q),
    )
  }, [items, searchInput])

  // --- Sort ---
  const sorted = useMemo(() => {
    const copy = [...filtered]
    switch (sort) {
      case 'newest_saved':
        return copy.sort(
          (a, b) =>
            new Date(b.savedAt).getTime() - new Date(a.savedAt).getTime(),
        )
      case 'oldest_saved':
        return copy.sort(
          (a, b) =>
            new Date(a.savedAt).getTime() - new Date(b.savedAt).getTime(),
        )
      case 'deadline':
        return copy.sort((a, b) => {
          if (!a.deadline) return 1
          if (!b.deadline) return -1
          return (
            new Date(a.deadline).getTime() - new Date(b.deadline).getTime()
          )
        })
      default:
        return copy
    }
  }, [filtered, sort])

  // --- Pagination ---
  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize))
  const paged = sorted.slice((page - 1) * pageSize, page * pageSize)
  const hasNextPage = page < totalPages
  const from = sorted.length > 0 ? (page - 1) * pageSize + 1 : 0
  const to = Math.min(page * pageSize, sorted.length)

  const searchDirty = searchInput !== ''

  const clearSearch = () => {
    setSearchInput('')
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

  return (
    <DashboardShell role="Talent" authUser={auth.user}>
      {/* Page header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
            Saved jobs
          </h1>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            {totalSaved} saved in total — a job stays here until its deadline
            passes and the 7-day grace window ends, then it leaves with a
            countdown so you always know.
          </p>
        </div>
      </div>

      {/* Banner: something was removed or passed its deadline */}
      {affectedCount > 0 && (
        <div
          role="status"
          className="mt-5 flex flex-wrap items-center gap-3 rounded-xl border border-tertiary-container/40 bg-tertiary-container/40 px-4 py-3 font-label-md text-label-md text-on-tertiary-container"
        >
          <span className="material-symbols-outlined text-lg">
            notifications_active
          </span>
          <span>
            {affectedCount} of your saved{' '}
            {affectedCount === 1 ? 'job is' : 'jobs are'} no longer available
            — they'll be removed from your list once the countdown ends.
          </span>
        </div>
      )}

      {/* Error banner */}
      {error && (
        <div
          role="alert"
          className="mt-5 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          <span className="flex items-center gap-2.5">
            <span className="material-symbols-outlined text-lg">error</span>
            <span>{getApiErrorMessage(error)}</span>
          </span>
          <button
            type="button"
            onClick={() => refreshSaved()}
            className="rounded-lg border border-on-error-container/30 px-3 py-1.5 font-label-md text-label-md font-medium transition-colors hover:bg-on-error-container/10"
          >
            Retry
          </button>
        </div>
      )}

      {/* Search + sort toolbar */}
      <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center">
        <div className="relative w-full flex-1">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">
            search
          </span>
          <input
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search saved job titles or companies..."
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 pl-10 pr-4 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex items-center gap-3">
          {searchDirty && (
            <button
              type="button"
              onClick={clearSearch}
              className="font-label-md text-label-md text-on-surface-variant transition-colors hover:text-on-surface"
            >
              Clear search
            </button>
          )}
          <label className="flex items-center gap-2 font-label-md text-label-md text-on-surface-variant">
            Sort by
            <select
              value={sort}
              onChange={(e) => setSort(e.target.value)}
              aria-label="Sort saved jobs"
              className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="newest_saved">Recently saved</option>
              <option value="oldest_saved">Oldest saved first</option>
              <option value="deadline">Closing soon</option>
            </select>
          </label>
        </div>
      </div>

      {/* Results */}
      {!isLoading && !error && (
        <p className="mt-6 font-label-md text-label-md text-on-surface-variant">
          {sorted.length === 0
            ? items.length === 0
              ? 'No saved jobs yet'
              : 'No saved jobs match your search'
            : `${sorted.length} saved ${sorted.length === 1 ? 'job' : 'jobs'}`}
        </p>
      )}

      {isLoading ? (
        <div className="mt-5">
          <SkeletonGrid count={pageSize} />
        </div>
      ) : error ? null : sorted.length === 0 ? (
        <div className="mt-5">
          {items.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-16 text-center shadow-sm">
              <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
                <span className="material-symbols-outlined text-3xl">
                  bookmark
                </span>
              </span>
              <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
                Nothing saved yet
              </h2>
              <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
                Bookmark jobs you want to come back to — they'll wait for you
                here with a countdown if they ever close.
              </p>
              <Link
                to="/dashboard/talent"
                className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
              >
                Browse jobs
              </Link>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-16 text-center shadow-sm">
              <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
                <span className="material-symbols-outlined text-3xl">
                  search_off
                </span>
              </span>
              <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
                No matching saved jobs
              </h2>
              <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
                Try adjusting your search.
              </p>
              <button
                type="button"
                onClick={clearSearch}
                className="mt-6 rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
              >
                Clear search
              </button>
            </div>
          )}
        </div>
      ) : (
        <div className="mt-5 grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
          {paged.map((item) => (
            <SavedJobCard
              key={item.savedJobId}
              item={item}
              saved
              onUnsave={() => handleUnsave(item)}
            />
          ))}
        </div>
      )}

      {/* Pagination */}
      {sorted.length > 0 && (
        <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <span className="font-label-md text-label-md text-on-surface-variant">
            Showing {from} to {to} of {sorted.length} results
          </span>
          <div className="flex flex-wrap items-center gap-3">
            <label className="flex items-center gap-2 font-label-md text-label-md text-on-surface-variant">
              Per page
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value))
                  setPage(1)
                }}
                aria-label="Saved jobs per page"
                className="rounded-lg border border-outline-variant bg-surface-container-lowest px-2 py-1.5 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              >
                {PAGE_SIZE_OPTIONS.map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
            </label>
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
                Page {page} of {totalPages}
              </span>
              <button
                type="button"
                disabled={!hasNextPage}
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
        </div>
      )}
    </DashboardShell>
  )
}

// --------------------------------------------------------------- Card

/**
 * A saved job card that matches the visual layout of the normal JobCard.
 * Uses the SavedJobItem fields (which lack some job details like skills,
 * jobType, experience, sector) but preserves the same structure.
 */
function SavedJobCard({
  item,
  saved,
  onUnsave,
}: {
  item: SavedJobItem
  saved: boolean
  onUnsave: () => void
}) {
  const brandLogo = sourceLogo(item.sourceName)

  const meta = [
    { icon: 'location_on', label: item.location || 'Location not specified' },
    { icon: 'payments', label: item.salary || 'Salary not specified' },
    ...(item.deadline
      ? [{ icon: 'event', label: `Closes ${formatDate(item.deadline)}` }]
      : []),
    { icon: 'bookmark_added', label: `Saved ${formatDate(item.savedAt)}` },
  ]

  return (
    <article
      className={cn(
        'group flex flex-col overflow-hidden rounded-lg border bg-surface-container-lowest transition-all duration-300 hover:shadow-sm',
        item.status === 'removed'
          ? 'border-error/30'
          : item.status === 'deadlinePassed'
            ? 'border-tertiary-container'
            : 'border-surface-variant hover:border-outline-variant',
      )}
      onMouseEnter={() => item.jobId && prefetchJob(item.jobId)}
    >
      {/* Header: company mark + title — mirrors JobCard */}
      <div className="flex flex-grow flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
        <div className="flex h-12 w-28 shrink-0 items-center justify-center overflow-hidden rounded bg-surface-container-low px-2">
          {brandLogo ? (
            <img
              className="max-h-8 w-auto max-w-full object-contain"
              src={brandLogo}
              alt={item.sourceName ?? ''}
            />
          ) : item.companyLogoUrl ? (
            <img
              className="max-h-8 w-auto max-w-full object-contain"
              src={item.companyLogoUrl}
              alt=""
            />
          ) : item.company ? (
            <span className="font-headline-md text-headline-md font-semibold text-primary">
              {initialsOf(item.company)}
            </span>
          ) : (
            <span className="material-symbols-outlined text-2xl text-on-surface-variant">
              work
            </span>
          )}
        </div>
        <div className="min-w-0">
          <h4 className="mb-1 truncate font-headline-md text-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint">
            {item.title}
          </h4>
          <p className="truncate font-body-md text-body-md text-secondary">
            {item.company || 'Company undisclosed'}
          </p>
          {item.sourceName &&
            item.sourceName.toLowerCase() !== 'serago' &&
            (item.sourceUrl ? (
              <a
                href={item.sourceUrl}
                target="_blank"
                rel="noreferrer"
                className="mt-0.5 inline-flex items-center gap-1 font-label-sm text-label-sm text-primary transition-colors hover:text-surface-tint"
              >
                via {item.sourceName}
                <span className="material-symbols-outlined text-sm">
                  open_in_new
                </span>
              </a>
            ) : (
              <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                via {item.sourceName}
              </p>
            ))}
        </div>
        <div className="shrink-0">
          <StatusBadge item={item} />
        </div>
      </div>

      {/* Meta grid + countdown + actions — mirrors JobCard */}
      <div className="flex flex-col gap-5 bg-surface/30 p-6">
        <div className="grid grid-cols-2 gap-3 font-label-md text-label-md text-on-surface-variant">
          {meta.map((m) => (
            <div key={m.icon} className="flex items-center gap-2">
              <span className="material-symbols-outlined text-[18px]">
                {m.icon}
              </span>
              <span className="truncate">{m.label}</span>
            </div>
          ))}
        </div>

        {/* Countdown for affected items */}
        <Countdown item={item} />

        <div className="mt-auto flex gap-2">
          {item.jobId ? (
            <Link
              to={`/dashboard/talent/jobs/${item.jobId}`}
              className="flex-1 rounded bg-accent px-4 py-2.5 text-center font-label-md text-label-md text-on-accent transition-colors hover:opacity-90"
            >
              Apply Now
            </Link>
          ) : (
            <span
              title="This job's listing has been removed"
              className="flex-1 cursor-not-allowed rounded bg-accent px-4 py-2.5 text-center font-label-md text-label-md text-on-accent opacity-60"
            >
              Apply Now
            </span>
          )}
          {item.jobId ? (
            <Link
              to={`/dashboard/talent/jobs/${item.jobId}`}
              className="flex-1 rounded border border-surface-variant px-4 py-2.5 text-center font-label-md text-label-md text-primary transition-colors hover:bg-surface-container-low"
            >
              See description
            </Link>
          ) : (
            <span
              title="This job's listing has been removed"
              className="flex-1 cursor-not-allowed rounded border border-surface-variant px-4 py-2.5 text-center font-label-md text-label-md text-on-surface-variant/60"
            >
              Listing removed
            </span>
          )}
          <SaveJobButton saved={saved} onToggle={onUnsave} />
        </div>
      </div>
    </article>
  )
}

// ------------------------------------------------------------- Skeleton

function SkeletonGrid({ count }: { count: number }) {
  return (
    <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
      {Array.from({ length: Math.min(count, 9) }).map((_, i) => (
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
