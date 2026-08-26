import { useState, useRef, useEffect } from 'react'
import { Link } from 'react-router-dom'
import {
  getApiErrorMessage,
  getStoredAuthTokens,
  updateApplicationStatus,
} from '../../../api'
import {
  useRecruiterJobStatsQuery,
  useRecruiterAllApplicationsQuery,
} from '../../../hooks/query.ts'
import { useRequireRole } from '../../../hooks'
import type {
  ApplicationResponse,
  ApplicationStatus,
  RecruiterJobStats,
} from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import RichTextDisplay from '../../../components/ui/RichTextDisplay'
import ResumeLink from '../../../components/ui/ResumeLink.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'

const PER_PAGE_OPTIONS = [10, 20, 50]

const STATUS_FILTERS: { value: ApplicationStatus | ''; label: string }[] = [
  { value: '', label: 'All' },
  { value: 'pending', label: 'Pending' },
  { value: 'reviewed', label: 'Reviewed' },
  { value: 'interview', label: 'Interview' },
  { value: 'hired', label: 'Hired' },
  { value: 'rejected', label: 'Rejected' },
]

const SORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'newest', label: 'Newest first' },
  { value: 'oldest', label: 'Oldest first' },
]

function statusBadge(status: ApplicationStatus): { label: string; className: string } {
  switch (status) {
    case 'pending':
      return { label: 'Pending', className: 'bg-surface-container text-on-surface-variant' }
    case 'reviewed':
      return { label: 'Reviewed', className: 'bg-blue-100 text-blue-900' }
    case 'interview':
      return { label: 'Interview', className: 'bg-amber-100 text-amber-900' }
    case 'hired':
      return { label: 'Hired', className: 'bg-primary-fixed text-on-primary-fixed-variant' }
    case 'rejected':
      return { label: 'Rejected', className: 'bg-error-container text-on-error-container' }
    default:
      return { label: status, className: 'bg-surface-container text-on-surface-variant' }
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit' })
}

/**
 * Recruiter's applications page: job cards grid → click card → filtered applications list.
 */
export default function ApplicationsPage() {
  const auth = useRequireRole('Recruiter')
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [selectedJobTitle, setSelectedJobTitle] = useState('')

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  const handleSelectJob = (jobId: string, jobTitle: string) => {
    setSelectedJobId(jobId)
    setSelectedJobTitle(jobTitle)
  }

  const handleBackToCards = () => {
    setSelectedJobId(null)
    setSelectedJobTitle('')
  }

  return (
    <DashboardShell role="Recruiter" authUser={auth.user}>
      <div>
        <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
          Applications
        </h1>
        <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
          {selectedJobId
            ? `Applications for ${selectedJobTitle}`
            : 'Select a job to view its applications.'}
        </p>
      </div>

      {selectedJobId ? (
        <ApplicationsList
          jobId={selectedJobId}
          onBack={handleBackToCards}
        />
      ) : (
        <JobCardsGrid onSelectJob={handleSelectJob} />
      )}
    </DashboardShell>
  )
}

// ───────────────────────── Job Cards Grid ─────────────────────────

function JobCardsGrid({ onSelectJob }: { onSelectJob: (id: string, title: string) => void }) {
  const { stats, isLoading, error, refresh } = useRecruiterJobStatsQuery(true)

  if (isLoading) {
    return <CardsSkeleton />
  }

  if (error) {
    return (
      <div className="mt-6 rounded-xl border border-error/30 bg-error-container px-4 py-8 text-center">
        <p className="font-body-md text-body-md text-on-error-container">{getApiErrorMessage(error)}</p>
        <button type="button" onClick={() => refresh()} className="mt-3 rounded-lg border border-on-error-container/30 px-4 py-2 font-label-md text-label-md text-on-error-container transition-colors hover:bg-on-error-container/10">
          Retry
        </button>
      </div>
    )
  }

  if (stats.length === 0) {
    return (
      <div className="mt-8 flex flex-col items-center justify-center rounded-xl border border-dashed border-outline-variant px-6 py-16 text-center">
        <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
          <span className="material-symbols-outlined text-3xl">description</span>
        </span>
        <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
          No published jobs yet
        </h2>
        <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
          Create and publish job postings to start receiving applications.
        </p>
      </div>
    )
  }

  return (
    <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {stats.map((job) => (
        <JobCard key={job.jobId} job={job} onSelect={() => onSelectJob(job.jobId, job.jobTitle)} />
      ))}
    </div>
  )
}

function JobCard({ job, onSelect }: { job: RecruiterJobStats; onSelect: () => void }) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className="group flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest p-5 text-left shadow-sm transition-all hover:border-primary/40 hover:shadow-md"
    >
      {/* Header */}
      <div className="min-w-0">
        <h3 className="truncate font-headline-md text-headline-md font-semibold text-on-surface group-hover:text-primary">
          {job.jobTitle}
        </h3>
        <p className="mt-1 truncate font-body-sm text-body-sm text-on-surface-variant">
          {job.jobCompany || 'No company'}
          {job.jobLocation ? ` · ${job.jobLocation}` : ''}
        </p>
      </div>

      {/* View count */}
      <div className="mt-4 flex items-center gap-1.5 text-on-surface-variant">
        <span className="material-symbols-outlined text-base">visibility</span>
        <span className="font-body-sm text-body-sm font-medium">{job.viewCount}</span>
        <span className="font-body-sm text-body-sm">views</span>
      </div>

      {/* Status breakdown */}
      <div className="mt-3 grid grid-cols-5 gap-1.5">
        <StatusPill label="Pending" count={job.pendingCount} color="text-on-surface-variant" />
        <StatusPill label="Reviewed" count={job.reviewedCount} color="text-blue-900" />
        <StatusPill label="Interview" count={job.interviewCount} color="text-amber-900" />
        <StatusPill label="Hired" count={job.hiredCount} color="text-on-primary-fixed-variant" />
        <StatusPill label="Rejected" count={job.rejectedCount} color="text-on-error-container" />
      </div>

      {/* Total + CTA */}
      <div className="mt-auto flex items-center justify-between pt-4 border-t border-surface-variant">
        <span className="font-label-md text-label-md font-medium text-on-surface-variant">
          {job.totalApplications} application{job.totalApplications === 1 ? '' : 's'}
        </span>
        <span className="font-label-md text-label-md font-medium text-primary group-hover:underline">
          View all →
        </span>
      </div>
    </button>
  )
}

function StatusPill({ label, count, color }: { label: string; count: number; color: string }) {
  return (
    <div className="flex flex-col items-center rounded-lg bg-surface-container-low px-1.5 py-1.5">
      <span className={`font-label-lg text-label-lg font-bold ${color}`}>{count}</span>
      <span className="font-label-xs text-label-xs text-on-surface-variant leading-tight">{label}</span>
    </div>
  )
}

function CardsSkeleton() {
  return (
    <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-5">
          <div className="h-5 w-2/3 animate-pulse rounded bg-surface-container" />
          <div className="mt-2 h-3 w-1/2 animate-pulse rounded bg-surface-container-low" />
          <div className="mt-4 h-4 w-1/3 animate-pulse rounded bg-surface-container" />
          <div className="mt-3 grid grid-cols-4 gap-2">
            {Array.from({ length: 4 }).map((_, j) => (
              <div key={j} className="h-12 animate-pulse rounded-lg bg-surface-container-low" />
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

// ─────────────────────── Applications List ────────────────────────

function ApplicationsList({ jobId, onBack }: { jobId: string; onBack: () => void }) {
  const { showToast } = useToast()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [statusFilter, setStatusFilter] = useState<ApplicationStatus | ''>('')
  const [sort, setSort] = useState('newest')
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [detailApp, setDetailApp] = useState<ApplicationResponse | null>(null)
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)

  useEffect(() => {
    searchTimer.current = setTimeout(() => setDebouncedSearch(search), 400)
    return () => { if (searchTimer.current) clearTimeout(searchTimer.current) }
  }, [search])

  const filterOpts = {
    status: statusFilter || undefined,
    sort,
    search: debouncedSearch || undefined,
    jobId,
  }

  const { applications, pagination, isLoading, error, refresh } = useRecruiterAllApplicationsQuery(true, page, pageSize, filterOpts)

  const handleOpenDetails = async (app: ApplicationResponse) => {
    if (app.status === 'pending') {
      const tokens = getStoredAuthTokens()
      if (tokens) {
        try {
          await updateApplicationStatus(app.id, { status: 'reviewed' }, tokens.accessToken)
          setDetailApp({ ...app, status: 'reviewed', statusUpdatedAt: new Date().toISOString() })
          refresh()
          showToast('Application marked as reviewed')
        } catch (err) {
          setDetailApp(app)
          showToast(getApiErrorMessage(err), 'error')
        }
        return
      }
    }
    setDetailApp(app)
  }

  return (
    <>
      {/* Back button */}
      <button
        type="button"
        onClick={onBack}
        className="mt-4 inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to all jobs
      </button>

      {/* Filters */}
      <div className="mt-4 space-y-3">
        {/* Search */}
        <div className="relative">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-lg text-on-surface-variant">search</span>
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search by name, email, or company..."
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
                statusFilter === filter.value
                  ? 'bg-primary text-on-primary'
                  : 'border border-outline-variant bg-surface-container-lowest text-on-surface-variant hover:bg-surface-container-low'
              }`}
            >
              {filter.label}
            </button>
          ))}

          <div className="flex-1" />

          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-lg text-on-surface-variant">sort</span>
            <select value={sort} onChange={(e) => { setSort(e.target.value); setPage(1) }} className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              {SORT_OPTIONS.map((opt) => (<option key={opt.value} value={opt.value}>{opt.label}</option>))}
            </select>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="mt-4 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
        {isLoading ? (
          <ListSkeleton />
        ) : error ? (
          <div className="px-6 py-8 text-center">
            <p className="font-body-md text-body-md text-on-surface-variant">{getApiErrorMessage(error)}</p>
            <button type="button" onClick={() => refresh()} className="mt-3 rounded-lg border border-outline-variant px-4 py-2 font-label-md text-label-md text-primary transition-colors hover:bg-surface-container-low">Retry</button>
          </div>
        ) : applications.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
            <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
              <span className="material-symbols-outlined text-3xl">person_search</span>
            </span>
            <h3 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">No applications found</h3>
            <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
              {statusFilter || debouncedSearch ? 'No applications match your filters.' : 'No one has applied to this job yet.'}
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="border-b border-surface-variant bg-surface-container-low">
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Applicant</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Headline</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Status</th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">Applied</th>
                  <th className="px-6 py-4 text-right font-label-md text-label-md font-semibold text-on-surface-variant">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-variant">
                {applications.map((app) => (
                  <ApplicationRow key={app.id} application={app} onStatusChange={refresh} onViewDetails={handleOpenDetails} />
                ))}
              </tbody>
            </table>
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
            <span className="font-label-md text-label-md text-on-surface-variant">Page {pagination.page} of {Math.max(pagination.totalPages, 1)}</span>
            <div className="flex items-center gap-2">
              <button type="button" disabled={page <= 1} onClick={() => setPage(1)} className="flex h-9 items-center gap-1 rounded-lg border border-outline-variant px-3 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40" title="First page">
                <span className="material-symbols-outlined text-lg">first_page</span>
              </button>
              <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))} className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
                <span className="material-symbols-outlined text-lg">chevron_left</span>
              </button>
              {getPageNumbers(pagination.page, pagination.totalPages).map((p, i) =>
                p === '...' ? (
                  <span key={`e-${i}`} className="px-1 font-label-md text-label-md text-on-surface-variant">…</span>
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
      </div>

      {/* Detail dialog */}
      {detailApp && (
        <ApplicationDetailDialog application={detailApp} onClose={() => { setDetailApp(null); refresh() }} onStatusChange={refresh} />
      )}
    </>
  )
}

// ─────────────────────── Shared Components ────────────────────────

function ApplicationRow({ application, onStatusChange, onViewDetails }: { application: ApplicationResponse; onStatusChange: () => void; onViewDetails: (app: ApplicationResponse) => void }) {
  const { showToast } = useToast()
  const [updating, setUpdating] = useState(false)
  const badge = statusBadge(application.status)

  const handleStatusChange = async (newStatus: ApplicationStatus) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setUpdating(true)
    try {
      await updateApplicationStatus(application.id, { status: newStatus }, tokens.accessToken)
      showToast(`Application ${newStatus}`)
      onStatusChange()
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error')
    } finally {
      setUpdating(false)
    }
  }

  return (
    <tr className="group transition-colors hover:bg-surface-container-low">
      <td className="px-6 py-4">
        <div className="flex items-center gap-3">
          {application.applicantAvatarUrl ? (
            <img src={application.applicantAvatarUrl} alt="" className="h-9 w-9 rounded-full object-cover" />
          ) : (
            <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary-container/60 font-label-sm text-label-sm font-semibold text-primary">
              {application.applicantName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()}
            </span>
          )}
          <div>
            <div className="font-body-md font-semibold text-on-surface">{application.applicantName}</div>
            <div className="font-label-sm text-label-sm text-on-surface-variant">{application.applicantEmail}</div>
          </div>
        </div>
      </td>
      <td className="px-6 py-4 font-body-md text-on-surface-variant">{application.applicantHeadline || '—'}</td>
      <td className="px-6 py-4">
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium ${badge.className}`}>{badge.label}</span>
      </td>
      <td className="px-6 py-4 font-body-md text-on-surface-variant">{formatDate(application.appliedAt)}</td>
      <td className="px-6 py-4">
        <div className="flex items-center justify-end gap-1">
          <Link to={`/dashboard/recruiter/applications/${application.id}/talent`} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-primary-container/50 hover:text-primary" title="View talent profile">
            <span className="material-symbols-outlined text-lg">person</span>
          </Link>
          <button type="button" disabled={updating} onClick={() => onViewDetails(application)} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-blue-50 hover:text-blue-700" title="View details">
            <span className="material-symbols-outlined text-lg">visibility</span>
          </button>
          {/* Pipeline actions: move forward or reject at each stage */}
          {application.status === 'pending' && (
            <button type="button" disabled={updating} onClick={() => handleStatusChange('reviewed')} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-blue-50 hover:text-blue-700" title="Mark as reviewed">
              <span className="material-symbols-outlined text-lg">visibility</span>
            </button>
          )}
          {application.status === 'reviewed' && (
            <button type="button" disabled={updating} onClick={() => handleStatusChange('interview')} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-amber-50 hover:text-amber-700" title="Move to interview">
              <span className="material-symbols-outlined text-lg">event</span>
            </button>
          )}
          {application.status === 'interview' && (
            <button type="button" disabled={updating} onClick={() => handleStatusChange('hired')} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-primary-container/50 hover:text-primary" title="Hire">
              <span className="material-symbols-outlined text-lg">check_circle</span>
            </button>
          )}
          {/* Reject available from pending, reviewed, interview */}
          {(application.status === 'pending' || application.status === 'reviewed' || application.status === 'interview') && (
            <button type="button" disabled={updating} onClick={() => handleStatusChange('rejected')} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-error-container/50 hover:text-error" title="Reject">
              <span className="material-symbols-outlined text-lg">cancel</span>
            </button>
          )}
          {application.resumeUrl && (
            <ResumeLink
              resumeUrl={application.resumeUrl}
              className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container hover:text-primary"
              title="View resume"
            >
              <span className="material-symbols-outlined text-lg">description</span>
            </ResumeLink>
          )}
        </div>
      </td>
    </tr>
  )
}

function ApplicationDetailDialog({ application, onClose, onStatusChange }: { application: ApplicationResponse; onClose: () => void; onStatusChange: () => void }) {
  const { showToast } = useToast()
  const [updating, setUpdating] = useState(false)
  const badge = statusBadge(application.status)

  const handleStatusChange = async (newStatus: ApplicationStatus) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setUpdating(true)
    try {
      await updateApplicationStatus(application.id, { status: newStatus }, tokens.accessToken)
      showToast(`Application ${newStatus}`)
      onStatusChange()
      onClose()
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error')
    } finally {
      setUpdating(false)
    }
  }

  return (
    <>
      <div className="fixed inset-0 z-50 bg-inverse-surface/50" onClick={onClose} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="w-full max-w-lg rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
          <div className="flex items-center justify-between">
            <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Application details</h2>
            <button type="button" onClick={onClose} className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container">
              <span className="material-symbols-outlined">close</span>
            </button>
          </div>

          <div className="mt-5 flex items-center gap-3">
            {application.applicantAvatarUrl ? (
              <img src={application.applicantAvatarUrl} alt="" className="h-12 w-12 rounded-full object-cover" />
            ) : (
              <span className="flex h-12 w-12 items-center justify-center rounded-full bg-primary-container/60 font-headline-sm text-headline-sm font-semibold text-primary">
                {application.applicantName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()}
              </span>
            )}
            <div>
              <p className="font-body-lg font-semibold text-on-surface">{application.applicantName}</p>
              <p className="font-body-sm text-body-sm text-on-surface-variant">{application.applicantEmail}</p>
              {application.applicantHeadline && <p className="font-body-sm text-body-sm text-on-surface-variant">{application.applicantHeadline}</p>}
            </div>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-4">
            <div>
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">Status</p>
              <span className={`mt-1 inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium ${badge.className}`}>{badge.label}</span>
            </div>
            <div>
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">Applied</p>
              <p className="mt-1 font-body-md text-body-md text-on-surface">{formatDateTime(application.appliedAt)}</p>
            </div>
            {application.statusUpdatedAt && (
              <div>
                <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">Last updated</p>
                <p className="mt-1 font-body-md text-body-md text-on-surface">{formatDateTime(application.statusUpdatedAt)}</p>
              </div>
            )}
          </div>

          <div className="mt-5">
            <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">Cover letter</p>
            {application.coverLetter ? (
              <div className="mt-2 rounded-xl border border-surface-variant bg-surface-container-low p-4"><RichTextDisplay html={application.coverLetter} /></div>
            ) : (
              <p className="mt-2 font-body-md text-body-md text-on-surface-variant italic">No cover letter submitted</p>
            )}
          </div>

          {application.resumeUrl && (
            <div className="mt-4">
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">Resume</p>
              <ResumeLink
                resumeUrl={application.resumeUrl}
                className="mt-2 inline-flex items-center gap-1.5 font-body-md text-body-md"
              >
                <span className="material-symbols-outlined text-lg">description</span>
                View resume
                <span className="material-symbols-outlined text-sm">open_in_new</span>
              </ResumeLink>
            </div>
          )}

          <div className="mt-4">
            <Link to={`/dashboard/recruiter/applications/${application.id}/talent`} className="inline-flex items-center gap-1.5 rounded-lg border border-primary/40 bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-primary transition-colors hover:bg-primary-container/20">
              <span className="material-symbols-outlined text-lg">person</span>
              View talent profile
              <span className="material-symbols-outlined text-sm">open_in_new</span>
            </Link>
          </div>

          <div className="mt-6 flex items-center justify-between border-t border-surface-variant pt-4">
            <div className="flex gap-2">
              {/* Pending → Review */}
              {application.status === 'pending' && (
                <button type="button" disabled={updating} onClick={() => handleStatusChange('reviewed')} className="inline-flex items-center gap-1.5 rounded-lg bg-blue-600 px-4 py-2.5 font-label-md text-label-md font-medium text-white shadow-sm transition-opacity hover:opacity-90 disabled:opacity-50">
                  <span className="material-symbols-outlined text-lg">visibility</span>
                  Mark as reviewed
                </button>
              )}
              {/* Reviewed → Interview */}
              {application.status === 'reviewed' && (
                <button type="button" disabled={updating} onClick={() => handleStatusChange('interview')} className="inline-flex items-center gap-1.5 rounded-lg bg-amber-600 px-4 py-2.5 font-label-md text-label-md font-medium text-white shadow-sm transition-opacity hover:opacity-90 disabled:opacity-50">
                  <span className="material-symbols-outlined text-lg">event</span>
                  Move to interview
                </button>
              )}
              {/* Interview → Hired */}
              {application.status === 'interview' && (
                <button type="button" disabled={updating} onClick={() => handleStatusChange('hired')} className="inline-flex items-center gap-1.5 rounded-lg bg-primary px-4 py-2.5 font-label-md text-label-md font-medium text-on-primary shadow-sm transition-opacity hover:opacity-90 disabled:opacity-50">
                  <span className="material-symbols-outlined text-lg">check_circle</span>
                  Hire
                </button>
              )}
              {/* Reject from any active stage */}
              {(application.status === 'pending' || application.status === 'reviewed' || application.status === 'interview') && (
                <button type="button" disabled={updating} onClick={() => handleStatusChange('rejected')} className="inline-flex items-center gap-1.5 rounded-lg border border-error/30 bg-error-container px-4 py-2.5 font-label-md text-label-md font-medium text-on-error-container transition-colors hover:bg-error-container/80 disabled:opacity-50">
                  <span className="material-symbols-outlined text-lg">cancel</span>
                  Reject
                </button>
              )}
              {/* Terminal states */}
              {application.status === 'hired' && (
                <span className="inline-flex items-center gap-1.5 rounded-lg bg-primary-fixed px-4 py-2.5 font-label-md text-label-md font-medium text-on-primary-fixed-variant">
                  <span className="material-symbols-outlined text-lg">check_circle</span>
                  Hired
                </span>
              )}
              {application.status === 'rejected' && (
                <span className="inline-flex items-center gap-1.5 rounded-lg border border-error/20 bg-error-container/50 px-4 py-2.5 font-label-md text-label-md font-medium text-on-error-container/70">
                  <span className="material-symbols-outlined text-lg">cancel</span>
                  Rejected
                </span>
              )}
            </div>
            <button type="button" onClick={onClose} className="rounded-lg border border-outline-variant bg-surface-container-lowest px-5 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low">Close</button>
          </div>
        </div>
      </div>
    </>
  )
}

function ListSkeleton() {
  return (
    <div className="divide-y divide-surface-variant">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-center gap-6 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="h-9 w-9 animate-pulse rounded-full bg-surface-container" />
            <div className="space-y-1.5">
              <div className="h-4 w-32 animate-pulse rounded bg-surface-container" />
              <div className="h-3 w-40 animate-pulse rounded bg-surface-container-low" />
            </div>
          </div>
          <div className="h-4 w-40 animate-pulse rounded bg-surface-container" />
          <div className="h-6 w-20 animate-pulse rounded-full bg-surface-container" />
          <div className="h-4 w-24 animate-pulse rounded bg-surface-container" />
          <div className="flex gap-1">
            <div className="h-8 w-8 animate-pulse rounded bg-surface-container" />
            <div className="h-8 w-8 animate-pulse rounded bg-surface-container" />
          </div>
        </div>
      ))}
    </div>
  )
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
