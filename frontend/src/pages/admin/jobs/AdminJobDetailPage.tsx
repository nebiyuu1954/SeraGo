import { useCallback, useEffect, useState } from 'react'
import { Link, useParams, useNavigate } from 'react-router-dom'
import {
  approveJob,
  deleteJob,
  fetchJob,
  rejectJob,
  restoreJob,
  setJobSector,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { JobResponse, SectorResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { JOB_TYPE_LABELS, WORK_ARRANGEMENT_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'

const STATUS_COLORS: Record<string, string> = {
  published: 'bg-success-container text-on-success-container',
  pendingApproval: 'bg-amber-100 text-amber-800',
  draft: 'bg-surface-container-low text-on-surface-variant',
  rejected: 'bg-error-container text-on-error-container',
}

export default function AdminJobDetailPage() {
  const { jobId } = useParams<{ jobId: string }>()
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  const [job, setJob] = useState<JobResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionLoading, setActionLoading] = useState(false)

  // Reject modal
  const [rejectModal, setRejectModal] = useState(false)
  const [rejectReason, setRejectReason] = useState('')

  // Sector reassignment
  const [sectors, setSectors] = useState<SectorResponse[]>([])
  const [sectorModal, setSectorModal] = useState(false)
  const [selectedSector, setSelectedSector] = useState<string>('')

  const load = useCallback(async () => {
    if (!jobId) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const [jobData, sectorData] = await Promise.allSettled([
        fetchJob(jobId, tokens.accessToken),
        fetchSectors(tokens.accessToken),
      ])
      if (jobData.status === 'fulfilled') {
        setJob(jobData.value)
      } else {
        setError(getApiErrorMessage(jobData.reason))
      }
      if (sectorData.status === 'fulfilled') {
        setSectors(sectorData.value)
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [jobId])

  useEffect(() => { load() }, [load])

  const handleApprove = async () => {
    if (!job) return
    setActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await approveJob(job.id, tokens.accessToken)
      setJob((prev) => prev ? { ...prev, status: 'published', isActive: true } : prev)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
    }
  }

  const handleReject = async () => {
    if (!job) return
    setActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await rejectJob(job.id, rejectReason.trim(), tokens.accessToken)
      setJob((prev) => prev ? { ...prev, status: 'rejected', isActive: false, rejectionReason: rejectReason.trim() || null } : prev)
      setRejectModal(false)
      setRejectReason('')
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
    }
  }

  const handleRestore = async () => {
    if (!job) return
    setActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await restoreJob(job.id, tokens.accessToken)
      setJob((prev) => prev ? { ...prev, isActive: true } : prev)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
    }
  }

  const handleDelete = async () => {
    if (!job || !confirm(`Permanently delete "${job.title}"? This cannot be undone.`)) return
    setActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await deleteJob(job.id, tokens.accessToken, true)
      navigate('/dashboard/admin/jobs', { state: { toast: 'Job permanently deleted.' } })
    } catch (err) {
      setError(getApiErrorMessage(err))
      setActionLoading(false)
    }
  }

  const handleSectorChange = async () => {
    if (!job) return
    setActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await setJobSector(job.id, selectedSector || null, tokens.accessToken)
      const sectorName = sectors.find((s) => s.id === selectedSector)?.name ?? null
      setJob((prev) => prev ? { ...prev, sectorId: selectedSector || null, sectorName } : prev)
      setSectorModal(false)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
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
      <Link to="/dashboard/admin/jobs" className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary">
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to jobs
      </Link>

      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : error ? (
        <div role="alert" className="mt-6 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">{error}</div>
      ) : job ? (
        <>
          {/* Header */}
          <div className="mt-6 flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-3">
                <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-on-surface">{job.title}</h1>
                <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium', STATUS_COLORS[job.status] ?? '')}>
                  {job.status === 'pendingApproval' ? 'Pending' : job.status.charAt(0).toUpperCase() + job.status.slice(1)}
                </span>
              </div>
              <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                {job.company || 'No company'} · {job.location || 'No location'} · {job.sourceName || 'SeraGo'}
              </p>
            </div>
          </div>

          {/* Actions bar */}
          <div className="mt-4 flex flex-wrap gap-2 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
            {job.status === 'pendingApproval' && (
              <>
                <button type="button" onClick={handleApprove} disabled={actionLoading}
                  className="inline-flex items-center gap-2 rounded-lg bg-success px-5 py-2.5 font-label-md text-label-md font-medium text-on-success transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50">
                  <span className="material-symbols-outlined text-lg">check_circle</span>
                  Approve & publish
                </button>
                <button type="button" onClick={() => setRejectModal(true)} disabled={actionLoading}
                  className="inline-flex items-center gap-2 rounded-lg bg-error px-5 py-2.5 font-label-md text-label-md font-medium text-on-error transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50">
                  <span className="material-symbols-outlined text-lg">cancel</span>
                  Reject
                </button>
              </>
            )}
            {job.status === 'rejected' && (
              <button type="button" onClick={handleRestore} disabled={actionLoading}
                className="inline-flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50">
                <span className="material-symbols-outlined text-lg">restore</span>
                Restore
              </button>
            )}
            <button type="button" onClick={() => navigate(`/dashboard/admin/jobs/${job.id}/edit`)}
              className="inline-flex items-center gap-2 rounded-lg border border-outline-variant px-5 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low">
              <span className="material-symbols-outlined text-lg">edit</span>
              Edit
            </button>
            <button type="button" onClick={() => { setSectorModal(true); setSelectedSector(job.sectorId ?? '') }}
              className="inline-flex items-center gap-2 rounded-lg border border-outline-variant px-5 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low">
              <span className="material-symbols-outlined text-lg">category</span>
              {job.sectorName ? `Sector: ${job.sectorName}` : 'Assign sector'}
            </button>
            <button type="button" onClick={handleDelete} disabled={actionLoading}
              className="ml-auto inline-flex items-center gap-2 rounded-lg border border-error/30 px-5 py-2.5 font-label-md text-label-md text-error transition-colors hover:bg-error-container/30 disabled:pointer-events-none disabled:opacity-50">
              <span className="material-symbols-outlined text-lg">delete_forever</span>
              Hard delete
            </button>
          </div>

          {/* Rejection reason */}
          {job.status === 'rejected' && job.rejectionReason && (
            <div className="mt-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 font-label-md text-label-md text-amber-900">
              <span className="font-semibold">Rejection reason:</span> {job.rejectionReason}
            </div>
          )}

          {/* Job details */}
          <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
            {/* Main info */}
            <div className="lg:col-span-2 space-y-6">
              <DetailCard title="Description">
                <div className="prose prose-sm max-w-none font-body-sm text-sm text-on-surface" dangerouslySetInnerHTML={{ __html: job.description || '<em>No description</em>' }} />
              </DetailCard>

              {job.sourceUrl && (
                <DetailCard title="Original listing">
                  <a href={job.sourceUrl} target="_blank" rel="noopener noreferrer"
                    className="font-body-sm text-sm text-primary underline hover:text-primary/80">
                    {job.sourceUrl}
                  </a>
                </DetailCard>
              )}
            </div>

            {/* Sidebar info */}
            <div className="space-y-4">
              <DetailCard title="Details">
                <div className="space-y-2 font-body-sm text-sm">
                  <InfoRow label="Type" value={JOB_TYPE_LABELS[job.jobType as keyof typeof JOB_TYPE_LABELS] ?? job.jobType} />
                  <InfoRow label="Work mode" value={WORK_ARRANGEMENT_LABELS[job.workMode as keyof typeof WORK_ARRANGEMENT_LABELS] ?? job.workMode} />
                  <InfoRow label="Sector" value={job.sectorName ?? 'Uncategorized'} />
                  <InfoRow label="Experience" value={job.experienceLevel ?? 'Not specified'} />
                  {job.experienceMinYears != null && (
                    <InfoRow label="Exp. range" value={`${job.experienceMinYears}–${job.experienceMaxYears ?? '…'} years`} />
                  )}
                  <InfoRow label="Positions" value={String(job.numberOfPositions)} />
                </div>
              </DetailCard>

              <DetailCard title="Salary">
                <div className="space-y-2 font-body-sm text-sm">
                  <InfoRow label="Text" value={job.salary || 'Not specified'} />
                  {job.salaryMin != null && (
                    <InfoRow label="Range" value={`${job.salaryMin.toLocaleString()} – ${job.salaryMax?.toLocaleString() ?? '…'} ${job.salaryCurrency ?? ''}`} />
                  )}
                  {job.salaryPeriod && <InfoRow label="Period" value={job.salaryPeriod} />}
                </div>
              </DetailCard>

              <DetailCard title="Dates">
                <div className="space-y-2 font-body-sm text-sm">
                  <InfoRow label="Created" value={new Date(job.createdAt).toLocaleString()} />
                  {job.publishedAt && <InfoRow label="Published" value={new Date(job.publishedAt).toLocaleString()} />}
                  {job.deadline && <InfoRow label="Deadline" value={new Date(job.deadline).toLocaleString()} />}
                  {job.refreshedAt && <InfoRow label="Refreshed" value={new Date(job.refreshedAt).toLocaleString()} />}
                </div>
              </DetailCard>

              <DetailCard title="Analytics">
                <div className="space-y-2 font-body-sm text-sm">
                  <InfoRow label="Views" value={String(job.viewCount)} />
                  {job.matchScore != null && <InfoRow label="AI match" value={`${job.matchScore}%`} />}
                </div>
              </DetailCard>
            </div>
          </div>

          {/* Reject modal */}
          {rejectModal && (
            <>
              <div className="fixed inset-0 z-50 bg-inverse-surface/50" onClick={() => setRejectModal(false)} />
              <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
                <div className="w-full max-w-md rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
                  <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Reject "{job.title}"</h2>
                  <p className="mt-1 font-body-sm text-sm text-on-surface-variant">Optionally provide a reason — the recruiter will see it.</p>
                  <textarea value={rejectReason} onChange={(e) => { if (e.target.value.length <= 500) setRejectReason(e.target.value) }}
                    placeholder="Reason for rejection (optional)..." rows={3} maxLength={500}
                    className="mt-4 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-3 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                  <p className="mt-1 text-right font-label-sm text-label-sm text-on-surface-variant/60">{rejectReason.length}/500</p>
                  <div className="mt-4 flex justify-end gap-3">
                    <button type="button" onClick={() => setRejectModal(false)}
                      className="rounded-lg border border-outline-variant px-5 py-2.5 font-label-md text-label-md text-on-surface hover:bg-surface-container-low">Cancel</button>
                    <button type="button" onClick={handleReject} disabled={actionLoading}
                      className="rounded-lg bg-error px-5 py-2.5 font-label-md text-label-md font-medium text-on-error transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50">
                      {actionLoading ? 'Rejecting…' : 'Reject'}
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}

          {/* Sector modal */}
          {sectorModal && (
            <>
              <div className="fixed inset-0 z-50 bg-inverse-surface/50" onClick={() => setSectorModal(false)} />
              <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
                <div className="w-full max-w-md rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
                  <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Assign sector</h2>
                  <p className="mt-1 font-body-sm text-sm text-on-surface-variant">Choose the canonical sector for this job.</p>
                  <select value={selectedSector} onChange={(e) => setSelectedSector(e.target.value)}
                    className="mt-4 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                    <option value="">No sector (uncategorized)</option>
                    {sectors.filter((s) => s.isActive).map((s) => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>
                  <div className="mt-4 flex justify-end gap-3">
                    <button type="button" onClick={() => setSectorModal(false)}
                      className="rounded-lg border border-outline-variant px-5 py-2.5 font-label-md text-label-md text-on-surface hover:bg-surface-container-low">Cancel</button>
                    <button type="button" onClick={handleSectorChange} disabled={actionLoading}
                      className="rounded-lg bg-primary px-5 py-2.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50">
                      {actionLoading ? 'Saving…' : 'Save'}
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}
        </>
      ) : null}
    </DashboardShell>
  )
}

function DetailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
      <h3 className="font-label-md text-label-md font-semibold text-on-surface">{title}</h3>
      <div className="mt-3">{children}</div>
    </div>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-on-surface-variant">{label}</span>
      <span className="font-medium text-on-surface">{value}</span>
    </div>
  )
}
