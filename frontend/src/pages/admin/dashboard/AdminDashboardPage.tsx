import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  fetchAdminStatsOverview,
  approveJob,
  rejectJob,
  getApiErrorMessage,
  getStoredAuthTokens,
  syncScrapedJobs,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { AdminStatsOverviewResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import PieChart from '../../../components/ui/PieChart.tsx'
import { cn } from '../../../lib/cn.ts'

const STATUS_COLORS: Record<string, string> = {
  success: 'bg-success-container text-on-success-container',
  partial: 'bg-amber-100 text-amber-800',
  failed: 'bg-error-container text-on-error-container',
  running: 'bg-blue-100 text-blue-800',
  unreachable: 'bg-error-container text-on-error-container',
  unconfigured: 'bg-surface-container-low text-on-surface-variant',
  no_data: 'bg-surface-container-low text-on-surface-variant',
  week_stats: 'bg-blue-100 text-blue-800',
}

export default function AdminDashboardPage() {
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  const [overview, setOverview] = useState<AdminStatsOverviewResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [syncing, setSyncing] = useState(false)

  // Quick approve/deny state
  const [rejectModal, setRejectModal] = useState<{ jobId: string; jobTitle: string } | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const load = useCallback(async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const data = await fetchAdminStatsOverview(tokens.accessToken)
      setOverview(data)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const handleSyncNow = async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setSyncing(true)
    try {
      await syncScrapedJobs(tokens.accessToken)
      await load()
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setSyncing(false)
    }
  }

  const handleQuickApprove = async (jobId: string) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await approveJob(jobId, tokens.accessToken)
      setOverview((prev) => {
        if (!prev) return prev
        return {
          ...prev,
          jobs: {
            ...prev.jobs,
            pendingApproval: prev.jobs.pendingApproval - 1,
            published: prev.jobs.published + 1,
            pendingJobs: prev.jobs.pendingJobs.filter((j) => j.id !== jobId),
          },
        }
      })
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
  }

  const handleQuickReject = async () => {
    if (!rejectModal) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await rejectJob(rejectModal.jobId, rejectReason.trim(), tokens.accessToken)
      setOverview((prev) => {
        if (!prev) return prev
        return {
          ...prev,
          jobs: {
            ...prev.jobs,
            pendingApproval: prev.jobs.pendingApproval - 1,
            rejected: prev.jobs.rejected + 1,
            pendingJobs: prev.jobs.pendingJobs.filter((j) => j.id !== rejectModal.jobId),
          },
        }
      })
      setRejectModal(null)
      setRejectReason('')
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
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
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">Admin Dashboard</h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Platform overview, scraper health, and pending approvals.
      </p>

      {error && (
        <div role="alert" className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
          {error}
        </div>
      )}

      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : overview ? (
        <>
          {/* ──── Users row ──── */}
          <div className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
            <KPICard icon="group" label="Total users" value={overview.users.total}
              onClick={() => navigate('/dashboard/admin/users')} />
            <KPICard icon="person_add" label="New this week" value={overview.users.newThisWeek} accent="text-primary" />
            <KPICard icon="today" label="New today" value={overview.users.newToday} accent="text-accent" />
            <KPICard icon="work" label="Jobs posted" value={overview.jobs.total}
              onClick={() => navigate('/dashboard/admin/jobs')} />
          </div>

          {/* ──── Roles + Applications row ──── */}
          <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Users by role</h2>
              <div className="mt-3">
                <PieChart
                  slices={[
                    { label: 'Talent', value: overview.users.byRole.talent, color: '#6750A4' },
                    { label: 'Admin', value: overview.users.byRole.admin, color: '#7D5260' },
                  ]}
                  size={130}
                />
              </div>
            </div>

            <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Applications</h2>
              <p className="mt-1 font-headline-lg text-headline-lg font-bold text-on-surface">{overview.applications.total}</p>
              <div className="mt-2 space-y-1">
                <MiniStat label="Pending" value={overview.applications.pending} />
                <MiniStat label="Reviewed" value={overview.applications.reviewed} />
                <MiniStat label="Shortlisted" value={overview.applications.shortlisted} />
                <MiniStat label="Interview" value={overview.applications.interview} />
                <MiniStat label="Hired" value={overview.applications.hired} />
                <MiniStat label="Rejected" value={overview.applications.rejected} />
              </div>
            </div>

            <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Job views</h2>
              <p className="mt-1 font-headline-lg text-headline-lg font-bold text-on-surface">{overview.jobs.totalViews.toLocaleString()}</p>
              <div className="mt-3 space-y-1">
                <MiniStat label="Published" value={overview.jobs.published} />
                <MiniStat label="Pending approval" value={overview.jobs.pendingApproval} accent={overview.jobs.pendingApproval > 0} />
                <MiniStat label="Drafts" value={overview.jobs.drafts} />
                <MiniStat label="Rejected" value={overview.jobs.rejected} />
              </div>
            </div>
          </div>

          {/* ──── Scraper health ──── */}
          <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="font-label-md text-label-md font-semibold text-on-surface">Scraper health</h2>
                <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                  {overview.scraper.day
                    ? `Data for ${new Date(overview.scraper.day).toLocaleDateString()}`
                    : 'No scrape data available'}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <span className={cn(
                  'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium',
                  STATUS_COLORS[overview.scraper.status] ?? 'bg-surface-container-low text-on-surface-variant',
                )}>
                  {overview.scraper.status}
                </span>
                <button
                  type="button"
                  onClick={handleSyncNow}
                  disabled={syncing}
                  className="rounded-lg bg-primary px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-primary transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50"
                >
                  {syncing ? 'Syncing…' : 'Sync now'}
                </button>
              </div>
            </div>

            {/* Totals */}
            <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-5">
              <ScrapeStatPill label="Sites scraped" value={overview.scraper.sitesScraped} />
              <ScrapeStatPill label="Total runs" value={overview.scraper.totalRunCount} />
              <ScrapeStatPill label="API hits" value={overview.scraper.totalApiHits} />
              <ScrapeStatPill label="Items found" value={overview.scraper.totalItemsFound} />
              <ScrapeStatPill label="Items inserted" value={overview.scraper.totalItemsInserted} />
            </div>

            {/* Per-site table */}
            {overview.scraper.sites.length > 0 && (
              <div className="mt-4 overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-surface-variant">
                      <th className="px-3 py-2 text-left font-label-sm text-label-sm font-semibold text-on-surface">Site</th>
                      <th className="px-3 py-2 text-left font-label-sm text-label-sm font-semibold text-on-surface">Status</th>
                      <th className="px-3 py-2 text-right font-label-sm text-label-sm font-semibold text-on-surface">Runs</th>
                      <th className="px-3 py-2 text-right font-label-sm text-label-sm font-semibold text-on-surface">API hits</th>
                      <th className="px-3 py-2 text-right font-label-sm text-label-sm font-semibold text-on-surface">Found</th>
                      <th className="px-3 py-2 text-right font-label-sm text-label-sm font-semibold text-on-surface">Inserted</th>
                      <th className="px-3 py-2 text-right font-label-sm text-label-sm font-semibold text-on-surface">Updated</th>
                      <th className="px-3 py-2 text-right font-label-sm text-label-sm font-semibold text-on-surface">Skipped</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-variant">
                    {overview.scraper.sites.map((site) => (
                      <tr key={site.source} className="transition-colors hover:bg-surface-container-low/50">
                        <td className="px-3 py-2.5 font-body-sm text-sm font-medium text-on-surface">{site.name}</td>
                        <td className="px-3 py-2.5">
                          <span className={cn(
                            'inline-flex items-center rounded-full px-2 py-0.5 font-label-xs text-[11px] font-medium',
                            STATUS_COLORS[site.status] ?? 'bg-surface-container-low text-on-surface-variant',
                          )}>
                            {site.status}
                          </span>
                        </td>
                        <td className="px-3 py-2.5 text-right font-body-sm text-sm text-on-surface">{site.runCount}</td>
                        <td className="px-3 py-2.5 text-right font-body-sm text-sm text-on-surface">{site.apiHits}</td>
                        <td className="px-3 py-2.5 text-right font-body-sm text-sm text-on-surface">{site.itemsFound}</td>
                        <td className="px-3 py-2.5 text-right font-body-sm text-sm text-success">{site.itemsInserted}</td>
                        <td className="px-3 py-2.5 text-right font-body-sm text-sm text-on-surface-variant">{site.itemsUpdated}</td>
                        <td className="px-3 py-2.5 text-right font-body-sm text-sm text-on-surface-variant">{site.itemsSkipped}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* ──── Pending jobs (quick approve/deny) ──── */}
          <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="font-label-md text-label-md font-semibold text-on-surface">
                  Pending approval
                  {overview.jobs.pendingApproval > 0 && (
                    <span className="ml-2 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-error px-1.5 font-label-xs text-[11px] font-bold text-on-error">
                      {overview.jobs.pendingApproval}
                    </span>
                  )}
                </h2>
                <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                  Jobs waiting for admin review
                </p>
              </div>
              {overview.jobs.pendingApproval > 0 && (
                <button
                  type="button"
                  onClick={() => navigate('/dashboard/admin/jobs?status=pendingApproval')}
                  className="font-label-sm text-label-sm font-medium text-primary transition-colors hover:text-primary/80"
                >
                  View all →
                </button>
              )}
            </div>

            {overview.jobs.pendingJobs.length === 0 ? (
              <p className="mt-4 font-body-sm text-sm text-on-surface-variant">No pending jobs. 🎉</p>
            ) : (
              <div className="mt-4 space-y-2">
                {overview.jobs.pendingJobs.map((job) => (
                  <div key={job.id} className="flex items-center justify-between gap-3 rounded-lg border border-surface-variant px-4 py-3 transition-colors hover:bg-surface-container-low/50">
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-body-sm text-sm font-medium text-on-surface">{job.title}</p>
                      <p className="truncate font-label-xs text-label-xs text-on-surface-variant">
                        {job.company || 'No company'} · {job.postedByName}
                        {job.sectorName && ` · ${job.sectorName}`}
                      </p>
                    </div>
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => handleQuickApprove(job.id)}
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
                      <button
                        type="button"
                        onClick={() => window.open(`/dashboard/admin/jobs/${job.id}`, '_blank')}
                        title="View details"
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-surface-container-high hover:text-on-surface"
                      >
                        <span className="material-symbols-outlined text-lg">open_in_new</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* ──── Sync info ──── */}
          <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">Last sync</h2>
            {overview.lastSync.ranAt ? (
              <div className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-5">
                <MiniStat label="Ran at" value={new Date(overview.lastSync.ranAt).toLocaleString()} wide />
                <MiniStat label="Inserted" value={overview.lastSync.inserted} />
                <MiniStat label="Updated" value={overview.lastSync.updated} />
                <MiniStat label="Unchanged" value={overview.lastSync.unchanged} />
                <MiniStat label="Auto-sync" value={overview.lastSync.schedulerEnabled ? 'ON' : 'OFF'} />
              </div>
            ) : (
              <p className="mt-3 font-body-sm text-sm text-on-surface-variant">No sync runs yet.</p>
            )}
          </div>

          {/* ──── Reject modal ──── */}
          {rejectModal && (
            <>
              <div className="fixed inset-0 z-50 bg-inverse-surface/50" onClick={() => setRejectModal(null)} />
              <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
                <div className="w-full max-w-md rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
                  <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Reject "{rejectModal.jobTitle}"</h2>
                  <p className="mt-1 font-body-sm text-sm text-on-surface-variant">Optionally provide a reason — it will be shown on the job.</p>
                  <textarea
                    value={rejectReason}
                    onChange={(e) => { if (e.target.value.length <= 500) setRejectReason(e.target.value) }}
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
                    <button type="button" onClick={handleQuickReject}
                      className="rounded-lg bg-error px-5 py-2.5 font-label-md text-label-md font-medium text-on-error transition-opacity hover:opacity-90">Reject</button>
                  </div>
                </div>
              </div>
            </>
          )}
        </>
      ) : (
        <div className="mt-10 text-center font-body-md text-body-md text-on-surface-variant">No data available.</div>
      )}
    </DashboardShell>
  )
}

function KPICard({ icon, label, value, accent, onClick }: {
  icon: string; label: string; value: number | string; accent?: string; onClick?: () => void
}) {
  const Tag = onClick ? 'button' : 'div'
  return (
    <Tag
      {...(onClick ? { onClick, type: 'button' as const } : {})}
      className={cn(
        'flex items-center gap-4 rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm text-left transition-all',
        onClick && 'cursor-pointer hover:border-outline-variant hover:shadow-md',
      )}
    >
      <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary-container/30">
        <span className="material-symbols-outlined text-2xl text-primary">{icon}</span>
      </div>
      <div>
        <p className={cn('font-headline-lg text-headline-lg font-bold', accent ?? 'text-on-surface')}>{typeof value === 'number' ? value.toLocaleString() : value}</p>
        <p className="font-label-sm text-label-sm text-on-surface-variant">{label}</p>
      </div>
    </Tag>
  )
}

function MiniStat({ label, value, accent, wide }: { label: string; value: number | string; accent?: boolean; wide?: boolean }) {
  return (
    <div className={cn('flex items-center justify-between', wide && 'col-span-2 sm:col-span-5')}>
      <span className="font-label-sm text-label-sm text-on-surface-variant">{label}</span>
      <span className={cn('font-label-sm text-label-sm font-medium', accent ? 'text-error' : 'text-on-surface')}>
        {typeof value === 'number' ? value.toLocaleString() : value}
      </span>
    </div>
  )
}

function ScrapeStatPill({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg bg-surface-container-high px-3 py-2 text-center">
      <p className="font-headline-sm text-headline-sm font-bold text-on-surface">{value.toLocaleString()}</p>
      <p className="font-label-xs text-label-xs text-on-surface-variant">{label}</p>
    </div>
  )
}
