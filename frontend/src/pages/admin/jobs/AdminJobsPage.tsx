import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  approveJob,
  deleteJob,
  fetchAdminStatsOverview,
  fetchJobs,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
  rejectJob,
  restoreJob,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { AdminStatsOverviewResponse, JobResponse, SectorResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { JOB_TYPE_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'

const STATUS_COLORS: Record<string, string> = {
  published: 'bg-success-container text-on-success-container',
  pendingApproval: 'bg-amber-100 text-amber-800',
  draft: 'bg-surface-container-low text-on-surface-variant',
  rejected: 'bg-error-container text-on-error-container',
}

const SOURCE_OPTIONS = [
  { value: '', label: 'All sources' },
  { value: 'SeraGo', label: 'SeraGo (posted directly)' },
  { value: 'Afriwork', label: 'Afriwork' },
  { value: 'EthioJobs', label: 'EthioJobs' },
  { value: 'HaHuJobs', label: 'HaHuJobs' },
  { value: 'GeezJobs', label: 'GeezJobs' },
  { value: 'ReporterJobs', label: 'ReporterJobs' },
]

const SORT_OPTIONS = [
  { value: 'views_desc', label: 'Most views' },
  { value: 'views_asc', label: 'Fewest views' },
  { value: 'applications_desc', label: 'Most applications' },
  { value: 'applications_asc', label: 'Fewest applications' },
  { value: 'newest', label: 'Recently posted' },
  { value: 'oldest', label: 'Oldest posted' },
  { value: 'updated_desc', label: 'Recently updated' },
]

export default function AdminJobsPage() {
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()
  const [jobs, setJobs] = useState<JobResponse[]>([])
  const [stats, setStats] = useState<AdminStatsOverviewResponse['jobs'] | null>(null)
  const [sectors, setSectors] = useState<SectorResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [totalPages, setTotalPages] = useState(0)
  const [hasNext, setHasNext] = useState(false)
  const [searchParams, setSearchParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const statusFilter = searchParams.get('status') ?? ''
  const sourceFilter = searchParams.get('source') ?? ''
  const sectorFilter = searchParams.get('sectorId') ?? ''
  const sort = searchParams.get('sort') ?? 'views_desc'
  const page = Math.max(1, parseInt(searchParams.get('page') ?? '1', 10) || 1)
  const [pageSize] = useState(20)

  const updateParams = (updates: Record<string, string>) => {
    setSearchParams((p) => {
      for (const [k, v] of Object.entries(updates)) {
        if (v) p.set(k, v)
        else p.delete(k)
      }
      return p
    })
  }
  const [rejectModal, setRejectModal] = useState<{ jobId: string; jobTitle: string } | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const load = useCallback(async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const [jobsData, statsData] = await Promise.allSettled([
        fetchJobs({
          q: search || undefined,
          status: statusFilter || undefined,
          includeInactive: true,
          source: sourceFilter === 'SeraGo' ? undefined : sourceFilter || undefined,
          sectorId: sectorFilter || undefined,
          sort,
          page,
          pageSize,
        }, tokens.accessToken),
        fetchAdminStatsOverview(tokens.accessToken),
      ])
      if (jobsData.status === 'fulfilled') {
        let items = jobsData.value.items
        // "SeraGo" means posted directly — the API filters by SourceName, so
        // SeraGo jobs (null source) are filtered client-side.
        if (sourceFilter === 'SeraGo') {
          items = items.filter((j) => !j.sourceName)
        }
        setJobs(items)
        setTotalPages(jobsData.value.pagination.totalPages)
        setHasNext(jobsData.value.pagination.hasNextPage)
      }
      if (statsData.status === 'fulfilled') {
        setStats(statsData.value.jobs)
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [search, statusFilter, sourceFilter, sectorFilter, sort, page, pageSize])

  // Load the sector list once for the sector filter dropdown.
  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    fetchSectors(tokens.accessToken)
      .then(setSectors)
      .catch(() => { /* non-critical */ })
  }, [])

  useEffect(() => { load() }, [load])

  const handleApprove = async (job: JobResponse) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await approveJob(job.id, tokens.accessToken)
      setJobs((prev) => prev.map((j) => j.id === job.id ? { ...j, status: 'published', isActive: true } : j))
      setStats((prev) => prev ? { ...prev, published: prev.published + 1, pendingApproval: prev.pendingApproval - 1 } : prev)
    } catch (err) { setError(getApiErrorMessage(err)) }
  }

  const handleReject = async () => {
    if (!rejectModal) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await rejectJob(rejectModal.jobId, rejectReason.trim(), tokens.accessToken)
      setJobs((prev) => prev.map((j) => j.id === rejectModal.jobId ? { ...j, status: 'rejected', isActive: false } : j))
      setStats((prev) => prev ? { ...prev, rejected: prev.rejected + 1, pendingApproval: prev.pendingApproval - 1 } : prev)
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
    } catch (err) { setError(getApiErrorMessage(err)) }
  }

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  const totalPageViews = jobs.reduce((sum, j) => sum + j.viewCount, 0)

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">Jobs</h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Review and manage all job listings.
      </p>

      {/* ──── Stat cards (12) ──── */}
      {stats && (
        <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-6">
          <StatCard icon="work" label="Total" value={stats.total} accent="text-on-surface" active={statusFilter === ''} onClick={() => updateParams({ status: '', page: '1' })} />
          <StatCard icon="check_circle" label="Published" value={stats.published} accent="text-success" active={statusFilter === 'published'} onClick={() => updateParams({ status: 'published', page: '1' })} />
          <StatCard icon="schedule" label="Pending" value={stats.pendingApproval} accent="text-amber-600" active={statusFilter === 'pendingApproval'} onClick={() => updateParams({ status: 'pendingApproval', page: '1' })} badge={stats.pendingApproval} />
          <StatCard icon="draft" label="Drafts" value={stats.drafts} accent="text-on-surface-variant" active={statusFilter === 'draft'} onClick={() => updateParams({ status: 'draft', page: '1' })} />
          <StatCard icon="cancel" label="Rejected" value={stats.rejected} accent="text-error" active={statusFilter === 'rejected'} onClick={() => updateParams({ status: 'rejected', page: '1' })} />
          <StatCard
            icon="insights"
            label="View analytics"
            accent="text-primary"
            onClick={() => navigate('/dashboard/admin/jobs/views', { state: { from: '/dashboard/admin/jobs' } })}
          />
          <StatCard icon="today" label="Views today" value={stats.viewsToday} accent="text-primary" />
          <StatCard icon="date_range" label="Views this week" value={stats.viewsThisWeek} accent="text-primary" />
          <StatCard icon="calendar_month" label="Views this month" value={stats.viewsThisMonth} accent="text-primary" />
          <StatCard icon="event" label="Views this year" value={stats.viewsThisYear} accent="text-primary" />
          <StatCard icon="preview" label="SeraGo jobs" value={stats.seragoJobs} accent="text-on-surface" />
          <StatCard icon="visibility" label="Total views" value={stats.totalViews} accent="text-primary" />
        </div>
      )}

      {/* Filters */}
      <div className="mt-5 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm lg:flex-row lg:items-center">
        <div className="relative flex-1">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">search</span>
          <input
            type="search"
            value={search}
            onChange={(e) => { setSearch(e.target.value); updateParams({ page: '1' }) }}
            placeholder="Search jobs..."
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 pl-10 pr-4 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <select
            value={sourceFilter}
            onChange={(e) => updateParams({ source: e.target.value, page: '1' })}
            className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            {SOURCE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
          <select
            value={sectorFilter}
            onChange={(e) => updateParams({ sectorId: e.target.value, page: '1' })}
            className="max-w-44 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option value="">All sectors</option>
            {sectors.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
          <select
            value={sort}
            onChange={(e) => updateParams({ sort: e.target.value, page: '1' })}
            className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            {SORT_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
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
        <div className="mt-5 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
          <table className="w-full">
            <thead>
              <tr className="border-b border-surface-variant bg-surface-container-low">
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Job</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Type</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Source</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Sector</th>
                <th className="px-5 py-3 text-right font-label-md text-label-md font-semibold text-on-surface">Views</th>
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
                  <td className="px-5 py-4 font-label-sm text-label-sm text-on-surface-variant">
                    {job.sectorName || '—'}
                  </td>
                  <td className="px-5 py-4 text-right">
                    <span className="inline-flex items-center gap-1 font-headline-sm font-bold text-on-surface">
                      <span className="material-symbols-outlined text-base">visibility</span>
                      {job.viewCount.toLocaleString()}
                    </span>
                  </td>
                  <td className="px-5 py-4">
                    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium', STATUS_COLORS[job.status] ?? '')}>
                      {job.status === 'pendingApproval' ? 'Pending' : job.status.charAt(0).toUpperCase() + job.status.slice(1)}
                    </span>
                  </td>
                  <td className="px-5 py-4 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {/* View as talent — opens the talent-style page under the admin path */}
                      <button type="button" onClick={() => navigate(`/dashboard/admin/jobs/${job.id}/preview`)} title="View as talent"
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-primary transition-colors hover:bg-primary-container/30">
                        <span className="material-symbols-outlined text-lg">visibility</span>
                      </button>
                      {job.status === 'pendingApproval' && (
                        <>
                          <button type="button" onClick={() => handleApprove(job)} title="Approve"
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-success transition-colors hover:bg-success-container/30">
                            <span className="material-symbols-outlined text-lg">check_circle</span>
                          </button>
                          <button type="button" onClick={() => setRejectModal({ jobId: job.id, jobTitle: job.title })} title="Reject"
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-error transition-colors hover:bg-error-container/30">
                            <span className="material-symbols-outlined text-lg">cancel</span>
                          </button>
                        </>
                      )}
                      {!job.isActive && job.status !== 'rejected' && (
                        <button type="button" onClick={() => handleRestore(job)} title="Restore"
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-primary transition-colors hover:bg-primary-container/30">
                          <span className="material-symbols-outlined text-lg">restore</span>
                        </button>
                      )}
                      {job.status === 'rejected' && (
                        <button type="button" onClick={() => handleRestore(job)} title="Re-approve"
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-primary transition-colors hover:bg-primary-container/30">
                          <span className="material-symbols-outlined text-lg">restore</span>
                        </button>
                      )}
                      <button type="button" onClick={() => navigate(`/dashboard/admin/jobs/${job.id}/edit`)} title="Edit"
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-surface-container-high hover:text-on-surface">
                        <span className="material-symbols-outlined text-lg">edit</span>
                      </button>
                      <button type="button" onClick={() => handleDelete(job)} title="Hard delete"
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-error transition-colors hover:bg-error-container/30">
                        <span className="material-symbols-outlined text-lg">delete_forever</span>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-surface-variant font-body-sm text-sm text-on-surface">
                <td colSpan={4} className="px-5 py-3 font-medium">Total views (this page)</td>
                <td className="px-5 py-3 text-right font-semibold">{totalPageViews.toLocaleString()}</td>
                <td colSpan={2} />
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center gap-2">
          <button type="button" disabled={page <= 1} onClick={() => updateParams({ page: String(Math.max(1, page - 1)) })}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
            <span className="material-symbols-outlined text-lg">chevron_left</span>
          </button>
          <span className="font-label-md text-label-md text-on-surface-variant">Page {page} of {totalPages}</span>
          <button type="button" disabled={!hasNext} onClick={() => updateParams({ page: String(page + 1) })}
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
              <textarea value={rejectReason} onChange={(e) => { if (e.target.value.length <= 500) setRejectReason(e.target.value) }}
                placeholder="Reason for rejection (optional)..." rows={3} maxLength={500}
                className="mt-4 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-3 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
              <p className="mt-1 text-right font-label-sm text-label-sm text-on-surface-variant/60">{rejectReason.length}/500</p>
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

function StatCard({ icon, label, value, accent, active, onClick, badge }: {
  icon: string; label?: string; value?: number; accent: string; active?: boolean; onClick?: () => void; badge?: number
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'relative flex items-center gap-3 rounded-xl border bg-surface-container-lowest p-4 shadow-sm text-left transition-all',
        active ? 'border-primary ring-1 ring-primary' : 'border-surface-variant hover:border-outline-variant hover:shadow-md',
      )}
    >
      {badge !== undefined && badge > 0 && (
        <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-error px-1 font-label-xs text-[11px] font-bold text-on-error">
          {badge}
        </span>
      )}
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary-container/30">
        <span className="material-symbols-outlined text-xl text-primary">{icon}</span>
      </div>
      <div>
        {value !== undefined && (
          <p className={cn('font-headline-md text-headline-md font-bold', accent)}>{value.toLocaleString()}</p>
        )}
        {label && <p className="font-label-xs text-label-xs text-on-surface-variant">{label}</p>}
      </div>
    </button>
  )
}
