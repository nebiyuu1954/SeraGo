import { useCallback, useEffect, useState } from 'react'
import {
  approveJob,
  deleteJob,
  fetchJobs,
  getApiErrorMessage,
  getStoredAuthTokens,
  rejectJob,
  restoreJob,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { JobResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { JOB_TYPE_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'

const STATUS_FILTERS = [
  { value: '', label: 'All' },
  { value: 'published', label: 'Published' },
  { value: 'pendingApproval', label: 'Pending' },
  { value: 'draft', label: 'Draft' },
  { value: 'rejected', label: 'Rejected' },
]

const STATUS_COLORS: Record<string, string> = {
  published: 'bg-success-container text-on-success-container',
  pendingApproval: 'bg-amber-100 text-amber-800',
  draft: 'bg-surface-container-low text-on-surface-variant',
  rejected: 'bg-error-container text-on-error-container',
}

export default function AdminJobsPage() {
  const auth = useRequireRole('Admin')
  const [jobs, setJobs] = useState<JobResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [hasNext, setHasNext] = useState(false)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [pageSize] = useState(20)
  const [rejectModal, setRejectModal] = useState<{ jobId: string; jobTitle: string } | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const load = useCallback(async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const data = await fetchJobs(
        {
          q: search || undefined,
          status: statusFilter || undefined,
          includeInactive: true,
          page,
          pageSize,
        },
        tokens.accessToken,
      )
      setJobs(data.items)
      setTotalCount(data.pagination.totalCount)
      setTotalPages(data.pagination.totalPages)
      setHasNext(data.pagination.hasNextPage)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [search, statusFilter, page, pageSize])

  useEffect(() => { load() }, [load])

  const handleApprove = async (job: JobResponse) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await approveJob(job.id, tokens.accessToken)
      setJobs((prev) => prev.map((j) => j.id === job.id ? { ...j, status: 'published', isActive: true } : j))
    } catch (err) { setError(getApiErrorMessage(err)) }
  }

  const handleReject = async () => {
    if (!rejectModal) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await rejectJob(rejectModal.jobId, rejectReason.trim(), tokens.accessToken)
      setJobs((prev) => prev.map((j) => j.id === rejectModal.jobId ? { ...j, status: 'rejected', isActive: false } : j))
      setRejectModal(null)
      setRejectReason('')
    } catch (err) { setError(getApiErrorMessage(err)) }
  }

  const handleRestore = async (job: JobResponse) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await restoreJob(job.id, tokens.accessToken)
      setJobs((prev) => prev.map((j) => j.id === job.id ? { ...j, isActive: true } : j))
    } catch (err) { setError(getApiErrorMessage(err)) }
  }

  const handleDelete = async (job: JobResponse) => {
    if (!confirm(`Permanently delete "${job.title}"? This cannot be undone.`)) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await deleteJob(job.id, tokens.accessToken, true)
      setJobs((prev) => prev.filter((j) => j.id !== job.id))
      setTotalCount((c) => c - 1)
    } catch (err) { setError(getApiErrorMessage(err)) }
  }

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">Jobs</h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Review and manage all {totalCount} job listings.
      </p>

      {/* Filters */}
      <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">search</span>
          <input
            type="search"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search jobs..."
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 pl-10 pr-4 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex gap-1 rounded-lg border border-surface-variant bg-surface-container-lowest p-1">
          {STATUS_FILTERS.map((f) => (
            <button
              key={f.value}
              type="button"
              onClick={() => { setStatusFilter(f.value); setPage(1) }}
              className={cn(
                'rounded-md px-3 py-1.5 font-label-sm text-label-sm transition-colors',
                statusFilter === f.value
                  ? 'bg-primary font-medium text-on-primary'
                  : 'text-on-surface-variant hover:text-on-surface',
              )}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {error && (
        <div role="alert" className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
          {error}
        </div>
      )}

      {/* Jobs table */}
      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : jobs.length === 0 ? (
        <div className="mt-10 text-center font-body-md text-body-md text-on-surface-variant">No jobs found.</div>
      ) : (
        <div className="mt-6 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
          <table className="w-full">
            <thead>
              <tr className="border-b border-surface-variant bg-surface-container-low">
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Job</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Type</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Source</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Status</th>
                <th className="px-5 py-3 text-right font-label-md text-label-md font-semibold text-on-surface">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-variant">
              {jobs.map((job) => (
                <tr key={job.id} className={cn('transition-colors hover:bg-surface-container-low/50', !job.isActive && 'opacity-60')}>
                  <td className="px-5 py-4">
                    <p className="truncate font-body-md text-body-md font-medium text-on-surface max-w-xs">{job.title}</p>
                    <p className="truncate font-label-sm text-label-sm text-on-surface-variant">
                      {job.company || 'No company'} · {job.location || 'No location'}
                    </p>
                  </td>
                  <td className="px-5 py-4 font-label-sm text-label-sm text-on-surface-variant">
                    {JOB_TYPE_LABELS[job.jobType as keyof typeof JOB_TYPE_LABELS] ?? job.jobType}
                  </td>
                  <td className="px-5 py-4 font-label-sm text-label-sm text-on-surface-variant">
                    {job.sourceName || 'SeraGo'}
                  </td>
                  <td className="px-5 py-4">
                    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium', STATUS_COLORS[job.status] ?? '')}>
                      {job.status === 'pendingApproval' ? 'Pending' : job.status.charAt(0).toUpperCase() + job.status.slice(1)}
                    </span>
                  </td>
                  <td className="px-5 py-4 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {job.status === 'pendingApproval' && (
                        <>
                          <button
                            type="button"
                            onClick={() => handleApprove(job)}
                            title="Approve"
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-success transition-colors hover:bg-success-container/30"
                          >
                            <span className="material-symbols-outlined text-lg">check_circle</span>
                          </button>
                          <button
                            type="button"
                            onClick={() => setRejectModal({ jobId: job.id, jobTitle: job.title })}
                            title="Reject"
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-error transition-colors hover:bg-error-container/30"
                          >
                            <span className="material-symbols-outlined text-lg">cancel</span>
                          </button>
                        </>
                      )}
                      {!job.isActive && job.status !== 'rejected' && (
                        <button
                          type="button"
                          onClick={() => handleRestore(job)}
                          title="Restore"
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-primary transition-colors hover:bg-primary-container/30"
                        >
                          <span className="material-symbols-outlined text-lg">restore</span>
                        </button>
                      )}
                      {job.status === 'rejected' && (
                        <button
                          type="button"
                          onClick={() => handleRestore(job)}
                          title="Re-approve"
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-primary transition-colors hover:bg-primary-container/30"
                        >
                          <span className="material-symbols-outlined text-lg">restore</span>
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => handleDelete(job)}
                        title="Hard delete"
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-error transition-colors hover:bg-error-container/30"
                      >
                        <span className="material-symbols-outlined text-lg">delete_forever</span>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center gap-2">
          <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
            <span className="material-symbols-outlined text-lg">chevron_left</span>
          </button>
          <span className="font-label-md text-label-md text-on-surface-variant">Page {page} of {totalPages}</span>
          <button type="button" disabled={!hasNext} onClick={() => setPage((p) => p + 1)}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
            <span className="material-symbols-outlined text-lg">chevron_right</span>
          </button>
        </div>
      )}

      {/* Reject modal */}
      {rejectModal && (
        <>
          <div className="fixed inset-0 z-50 bg-inverse-surface/50" onClick={() => setRejectModal(null)} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="w-full max-w-md rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
              <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Reject "{rejectModal.jobTitle}"</h2>
              <p className="mt-1 font-body-sm text-sm text-on-surface-variant">Optionally provide a reason — the recruiter will see it.</p>
              <textarea
                value={rejectReason}
                onChange={(e) => {
                  if (e.target.value.length <= 500) setRejectReason(e.target.value)
                }}
                placeholder="Reason for rejection (optional)..."
                rows={3}
                maxLength={500}
                className="mt-4 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-3 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
              <p className="mt-1 text-right font-label-sm text-label-sm text-on-surface-variant/60">
                {rejectReason.length}/500
              </p>
              <div className="mt-4 flex justify-end gap-3">
                <button type="button" onClick={() => setRejectModal(null)}
                  className="rounded-lg border border-outline-variant px-5 py-2.5 font-label-md text-label-md text-on-surface hover:bg-surface-container-low">Cancel</button>
                <button type="button" onClick={handleReject}
                  className="rounded-lg bg-error px-5 py-2.5 font-label-md text-label-md font-medium text-on-error transition-opacity hover:opacity-90">Reject</button>
              </div>
            </div>
          </div>
        </>
      )}
    </DashboardShell>
  )
}
