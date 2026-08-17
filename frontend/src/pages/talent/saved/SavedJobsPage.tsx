import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  fetchSavedJobs,
  getApiErrorMessage,
  getStoredAuthTokens,
  unsaveJob,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { SavedJobItem } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import { formatDate } from '../../../lib/date.ts'
import { initialsOf } from '../../../lib/initials.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'
import { cn } from '../../../lib/cn.ts'

const DAY_MS = 86_400_000

/** Whole days until an ISO date, floor at 1 (a card leaving "today" still shows 1). */
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

/**
 * Talent's saved jobs: every job the user bookmarked, with an honest status —
 * a removed/deadline-passed job shows a countdown until it leaves the list
 * (deadline + 7 days, the same lifecycle rule as the public feed). After the
 * window the card is gone, but the lifetime count (totalSaved) keeps counting.
 */
export default function SavedJobsPage() {
  const auth = useRequireRole('Talent')

  const [items, setItems] = useState<SavedJobItem[]>([])
  const [totalSaved, setTotalSaved] = useState(0)
  const [affectedCount, setAffectedCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const { showToast } = useToast()

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens || auth.status !== 'authenticated') return
    let cancelled = false
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await fetchSavedJobs(tokens.accessToken)
        if (!cancelled) {
          setItems(data.items)
          setTotalSaved(data.totalSaved)
          setAffectedCount(data.affectedCount)
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
  }, [auth.status, refreshKey])

  const handleUnsave = useCallback(
    async (item: SavedJobItem) => {
      const tokens = getStoredAuthTokens()
      if (!tokens || !item.jobId) return
      try {
        await unsaveJob(item.jobId, tokens.accessToken)
        showToast('Removed from saved jobs')
        setRefreshKey((k) => k + 1)
      } catch {
        showToast('Could not remove this job', 'error')
      }
    },
    [],
  )

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
      {/* Header */}
      <div className="flex flex-col gap-1">
        <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
          Saved jobs
        </h1>
        <p className="font-body-md text-body-md text-on-surface-variant">
          {totalSaved} saved in total — a job stays here until its deadline
          passes and the 7-day grace window ends, then it leaves with a
          countdown so you always know.
        </p>
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

      {error && (
        <div
          role="alert"
          className="mt-5 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
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

      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      ) : items.length === 0 ? (
        <div className="mt-8 flex flex-col items-center justify-center rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-16 text-center shadow-sm">
          <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
            <span className="material-symbols-outlined text-3xl">bookmark</span>
          </span>
          <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
            Nothing saved yet
          </h2>
          <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
            Bookmark jobs you want to come back to — they'll wait for you here
            with a countdown if they ever close.
          </p>
          <Link
            to="/dashboard/talent"
            className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
          >
            Browse jobs
          </Link>
        </div>
      ) : (
        <div className="mt-5 grid grid-cols-1 gap-6 xl:grid-cols-2">
          {items.map((item) => (
            <SavedCard
              key={item.savedJobId}
              item={item}
              onUnsave={() => handleUnsave(item)}
            />
          ))}
        </div>
      )}
    </DashboardShell>
  )
}

/* --------------------------------------------------------------- Card */

function SavedCard({
  item,
  onUnsave,
}: {
  item: SavedJobItem
  onUnsave: () => void
}) {
  const brandLogo = sourceLogo(item.sourceName)
  const meta = [
    { icon: 'location_on', label: item.location || 'Location not specified' },
    { icon: 'payments', label: item.salary || 'Salary not specified' },
    ...(item.deadline
      ? [{ icon: 'event', label: `Closes ${formatDate(item.deadline)}` }]
      : []),
  ]

  const cardBody = (
    <article
      className={cn(
        'group flex h-full flex-col overflow-hidden rounded-lg border bg-surface-container-lowest transition-all duration-300 hover:shadow-sm',
        item.status === 'removed'
          ? 'border-error/30'
          : item.status === 'deadlinePassed'
            ? 'border-tertiary-container'
            : 'border-surface-variant hover:border-outline-variant',
      )}
    >
      {/* Header: company mark + title + status */}
      <div className="flex flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
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
        <div className="min-w-0 flex-1">
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

      {/* Meta + countdown + actions */}
      <div className="flex flex-grow flex-col gap-4 bg-surface/30 p-6">
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
        <div className="mt-auto">
          <Countdown item={item} />
        </div>
        <div className="flex gap-2">
          {item.jobId ? (
            <Link
              to={`/dashboard/talent/jobs/${item.jobId}`}
              className="flex-1 rounded border border-surface-variant px-4 py-2.5 text-center font-label-md text-label-md text-primary transition-colors hover:bg-surface-container-low"
            >
              View job
            </Link>
          ) : (
            <span
              title="This job's listing has been removed"
              className="flex-1 cursor-not-allowed rounded border border-surface-variant px-4 py-2.5 text-center font-label-md text-label-md text-on-surface-variant/60"
            >
              Listing removed
            </span>
          )}
          <button
            type="button"
            onClick={onUnsave}
            className="flex items-center justify-center gap-1.5 rounded border border-surface-variant px-4 py-2.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-on-surface"
          >
            <span className="material-symbols-outlined text-lg">bookmark_remove</span>
            Remove
          </button>
        </div>
      </div>
    </article>
  )

  return cardBody
}
