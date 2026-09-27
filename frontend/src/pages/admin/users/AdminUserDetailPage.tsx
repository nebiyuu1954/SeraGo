import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  fetchAdminUser,
  fetchJobs,
  anonymizeUser,
  sendAdminPasswordReset,
  updateUserRole,
  updateUserStatus,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { AdminUserActivity, AdminUserResponse, JobResponse } from '../../../types'

/** Human labels for the completion fields the backend reports as missing. */
const PROFILE_FIELD_LABELS: Record<string, string> = {
  headline: 'Headline',
  about: 'About',
  experienceLevel: 'Experience level',
  yearsOfExperience: 'Years of experience',
  desiredRoles: 'Desired roles',
  skills: 'Skills',
  desiredJobTypes: 'Desired job types',
  workMode: 'Work mode',
  availability: 'Availability',
  companyName: 'Company name',
  industry: 'Industry',
  companySize: 'Company size',
  websiteUrl: 'Website',
  foundedYear: 'Founded year',
  headquarters: 'Headquarters',
  phoneNumber: 'Phone number',
  email: 'Contact email',
  companyType: 'Company type',
}
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { JOB_TYPE_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'

const ROLE_OPTIONS = ['Talent', 'Admin']
const ROLE_COLORS: Record<string, string> = {
  Talent: 'bg-blue-100 text-blue-800',
  Admin: 'bg-purple-100 text-purple-800',
}

/**
 * Role-specific engagement summary. Talents see their job-hunting numbers,
 * plus activity days, last-seen and profile completion.
 */
function ActivitySection({ activity }: { activity: AdminUserActivity }) {
  const isTalent = activity.role === 'Talent'

  // Admins have no role profile and no role activity to report.
  if (!isTalent) return null

  const roleTiles = [
    {
      icon: 'visibility',
      label: 'Jobs viewed',
      value: activity.talent?.jobsViewed ?? 0,
    },
    {
      icon: 'send',
      label: 'Applications submitted',
      value: activity.talent?.applicationsSubmitted ?? 0,
    },
  ]

  const missingLabels = activity.missingProfileFields.map(
    (field) => PROFILE_FIELD_LABELS[field] ?? field,
  )

  return (
    <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
      <h2 className="font-label-md text-label-md font-semibold text-on-surface">Activity</h2>
      <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
        What this talent has been doing on SeraGo.
      </p>

      {/* Role numbers */}
      <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
        {roleTiles.map((tile) => (
          <ActivityTile key={tile.label} icon={tile.icon} label={tile.label} value={tile.value} />
        ))}
      </div>

      {/* Shared: activity days + last seen */}
      <div className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3">
        <ActivityTile
          icon="calendar_view_week"
          label="Active days this week"
          value={activity.activeDaysThisWeek}
        />
        <ActivityTile
          icon="calendar_month"
          label="Active days this month"
          value={activity.activeDaysThisMonth}
        />
        <ActivityTile
          icon="schedule"
          label="Last active"
          value={activity.lastActiveAt ? formatRelative(activity.lastActiveAt) : 'Never'}
        />
      </div>

      {/* Profile completion */}
      <div className="mt-4 rounded-lg border border-surface-variant px-4 py-3">
        <div className="flex items-center justify-between gap-3">
          <p className="font-label-sm text-label-sm font-medium text-on-surface">
            Profile completion
          </p>
          <p className="font-label-md text-label-md font-bold text-primary">
            {activity.profileCompletionPercent}%
          </p>
        </div>
        <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-surface-container-high">
          <div
            className={cn(
              'h-2 rounded-full',
              activity.profileComplete ? 'bg-success' : 'bg-primary',
            )}
            style={{ width: `${activity.profileCompletionPercent}%` }}
          />
        </div>
        {missingLabels.length > 0 ? (
          <p className="mt-2 font-label-xs text-label-xs text-on-surface-variant">
            Still missing: {missingLabels.join(', ')}
          </p>
        ) : (
          <p className="mt-2 font-label-xs text-label-xs text-success">
            Profile is complete.
          </p>
        )}
      </div>

      <p className="mt-3 font-label-xs text-label-xs text-on-surface-variant">
        &ldquo;Active days&rdquo; counts distinct UTC days with a recorded action (job view or
        application) — SeraGo has no login audit trail, so this is the closest
        available measure of how often they were online.
      </p>
    </div>
  )
}

function ActivityTile({
  icon,
  label,
  value,
}: {
  icon: string
  label: string
  value: number | string
}) {
  return (
    <div className="rounded-lg border border-surface-variant bg-surface-container-low px-3 py-2.5">
      <div className="flex items-center gap-1.5 text-on-surface-variant">
        <span className="material-symbols-outlined text-base">{icon}</span>
        <p className="font-label-xs text-label-xs">{label}</p>
      </div>
      <p className="mt-1 font-headline-sm text-headline-sm font-bold text-on-surface">
        {typeof value === 'number' ? value.toLocaleString() : value}
      </p>
    </div>
  )
}

/** "2h ago" / "3d ago" / a date for anything older than a month. */
function formatRelative(value: string): string {
  const then = new Date(value)
  if (Number.isNaN(then.getTime())) return value
  const diffMs = Date.now() - then.getTime()
  const minutes = Math.floor(diffMs / 60_000)
  if (minutes < 1) return 'Just now'
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 30) return `${days}d ago`
  return then.toLocaleDateString()
}

export default function AdminUserDetailPage() {
  const { userId } = useParams<{ userId: string }>()
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  const [user, setUser] = useState<AdminUserResponse | null>(null)
  const [jobs, setJobs] = useState<JobResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionLoading, setActionLoading] = useState(false)

  // Anonymize confirm modal
  const [anonymizeModal, setAnonymizeModal] = useState(false)
  const [anonymizeConfirm, setAnonymizeConfirm] = useState('')

  const load = useCallback(async () => {
    if (!userId) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const userData = await fetchAdminUser(userId, tokens.accessToken)
      setUser(userData)

      // Only admins can own jobs — fetching this for a talent user just
      // returned an empty list and rendered a misleading "Posted jobs (0)" card.
      if (userData.userType === 'Admin') {
        const jobsData = await fetchJobs(
          { postedBy: userId, pageSize: 50 },
          tokens.accessToken,
        )
        setJobs(jobsData.items)
      } else {
        setJobs([])
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [userId])

  useEffect(() => { load() }, [load])

  const handleRoleChange = async (newRole: string) => {
    if (!user || !confirm(`Change this user's role to ${newRole}?`)) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setActionLoading(true)
    setActionError(null)
    try {
      const updated = await updateUserRole(user.id, newRole, tokens.accessToken)
      setUser(updated)
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
    }
  }

  const handleToggleActive = async () => {
    if (!user) return
    const action = user.isActive ? 'Deactivate' : 'Activate'
    if (!confirm(`${action} this user?`)) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setActionLoading(true)
    setActionError(null)
    try {
      const updated = await updateUserStatus(user.id, !user.isActive, tokens.accessToken)
      setUser(updated)
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
    }
  }

  const handleSendResetEmail = async () => {
    if (!user) return
    if (!confirm(`Send a password reset email to ${user.email}?`)) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setActionLoading(true)
    setActionError(null)
    try {
      await sendAdminPasswordReset(user.id, tokens.accessToken)
      alert('Password reset email sent.')
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    } finally {
      setActionLoading(false)
    }
  }

  const handleAnonymize = async () => {
    if (!user) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setActionLoading(true)
    setActionError(null)
    try {
      const updated = await anonymizeUser(user.id, tokens.accessToken)
      setUser(updated)
      setAnonymizeModal(false)
      setAnonymizeConfirm('')
    } catch (err) {
      setActionError(getApiErrorMessage(err))
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
      <Link
        to="/dashboard/admin/users"
        className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to users
      </Link>

      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : error ? (
        <div role="alert" className="mt-6 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
          {error}
        </div>
      ) : user ? (
        <>
          {/* User header */}
          <div className="mt-6 flex items-start gap-5 rounded-xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
            {user.avatarUrl ? (
              <img src={user.avatarUrl} alt="" className="h-16 w-16 rounded-full object-cover" />
            ) : (
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary font-headline-lg text-headline-lg font-bold text-on-primary">
                {user.firstName[0]}{user.lastName[0]}
              </div>
            )}
            <div className="min-w-0 flex-1">
              <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-on-surface">
                {user.firstName} {user.middleName ? `${user.middleName} ` : ''}{user.lastName}
              </h1>
              <p className="font-body-md text-body-md text-on-surface-variant">{user.email}</p>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                <span className={cn(
                  'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium',
                  ROLE_COLORS[user.userType] ?? 'bg-gray-100 text-gray-800',
                )}>
                  {user.userType}
                </span>
                <span className={cn(
                  'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium',
                  user.isActive ? 'bg-success-container text-on-success-container' : 'bg-error-container text-on-error-container',
                )}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </span>
                {!user.emailConfirmed && (
                  <span className="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 font-label-sm text-label-sm font-medium text-amber-800">
                    Email unconfirmed
                  </span>
                )}
                {user.city && (
                  <span className="font-label-sm text-label-sm text-on-surface-variant">
                    📍 {user.city}{user.country ? `, ${user.country}` : ''}
                  </span>
                )}
                <span className="font-label-sm text-label-sm text-on-surface-variant">
                  Joined {new Date(user.createdAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          </div>

          {actionError && (
            <div role="alert" className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
              {actionError}
            </div>
          )}

          {/* Admin actions */}
          <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">Admin actions</h2>
            <div className="mt-4 flex flex-wrap gap-3">
              {/* Role change */}
              <div className="flex items-center gap-2">
                <span className="font-label-sm text-label-sm text-on-surface-variant">Role:</span>
                <select
                  value={user.userType}
                  onChange={(e) => handleRoleChange(e.target.value)}
                  disabled={actionLoading}
                  className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-label-sm text-label-sm text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                >
                  {ROLE_OPTIONS.map((r) => <option key={r} value={r}>{r}</option>)}
                </select>
              </div>

              {/* Toggle active */}
              <button
                type="button"
                onClick={handleToggleActive}
                disabled={actionLoading}
                className={cn(
                  'rounded-lg px-4 py-1.5 font-label-sm text-label-sm font-medium transition-colors',
                  user.isActive
                    ? 'border border-error/30 text-error hover:bg-error-container/30'
                    : 'border border-success/30 text-success hover:bg-success-container/30',
                )}
              >
                {user.isActive ? 'Deactivate' : 'Activate'}
              </button>

              {/* Send reset email */}
              <button
                type="button"
                onClick={handleSendResetEmail}
                disabled={actionLoading}
                className="rounded-lg border border-outline-variant px-4 py-1.5 font-label-sm text-label-sm font-medium text-on-surface transition-colors hover:bg-surface-container-low"
              >
                Send password reset email
              </button>

              {/* Anonymize */}
              <button
                type="button"
                onClick={() => setAnonymizeModal(true)}
                disabled={actionLoading}
                className="rounded-lg bg-error px-4 py-1.5 font-label-sm text-label-sm font-medium text-on-error transition-opacity hover:opacity-90"
              >
                Deactivate & anonymize
              </button>
            </div>
          </div>

          {/* Engagement — role-specific */}
          {user.activity && <ActivitySection activity={user.activity} />}

          {/* Posted jobs — admins only (talents never own jobs, so an empty
              "Posted jobs (0)" card was just noise) */}
          {user.userType === 'Admin' && (
          <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">
              Posted jobs ({jobs.length})
            </h2>
            {jobs.length === 0 ? (
              <p className="mt-3 font-body-sm text-sm text-on-surface-variant">No jobs posted by this user.</p>
            ) : (
              <div className="mt-3 space-y-2">
                {jobs.map((job) => (
                  <div key={job.id} className="flex items-center justify-between rounded-lg border border-surface-variant px-4 py-3 transition-colors hover:bg-surface-container-low/50">
                    <div className="min-w-0 flex-1">
                      <p className="truncate font-body-md text-body-md font-medium text-on-surface">{job.title}</p>
                      <p className="truncate font-label-sm text-label-sm text-on-surface-variant">
                        {job.company || 'No company'} · {JOB_TYPE_LABELS[job.jobType as keyof typeof JOB_TYPE_LABELS] ?? job.jobType}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 font-label-xs text-[11px] font-medium',
                        job.status === 'published' ? 'bg-success-container text-on-success-container'
                          : job.status === 'pendingApproval' ? 'bg-amber-100 text-amber-800'
                          : job.status === 'rejected' ? 'bg-error-container text-on-error-container'
                          : 'bg-surface-container-low text-on-surface-variant',
                      )}>
                        {job.status === 'pendingApproval' ? 'Pending' : job.status}
                      </span>
                      <button
                        type="button"
                        onClick={() => navigate(`/dashboard/admin/jobs/${job.id}/edit`)}
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-surface-container-high hover:text-on-surface"
                        title="Edit"
                      >
                        <span className="material-symbols-outlined text-lg">edit</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
          )}

          {/* Anonymize modal */}
          {anonymizeModal && (
            <>
              <div className="fixed inset-0 z-50 bg-inverse-surface/50" onClick={() => setAnonymizeModal(false)} />
              <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
                <div className="w-full max-w-md rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
                  <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Deactivate & anonymize</h2>
                  <p className="mt-2 font-body-sm text-sm text-on-surface-variant">
                    This will deactivate the account and strip all personal information
                    (name, email, avatar, location). The user will no longer be able to sign in.
                    This action cannot be undone.
                  </p>
                  <p className="mt-3 font-body-sm text-sm font-medium text-on-surface">
                    Type <span className="font-mono">ANONYMIZE</span> to confirm:
                  </p>
                  <input
                    type="text"
                    value={anonymizeConfirm}
                    onChange={(e) => setAnonymizeConfirm(e.target.value)}
                    placeholder="Type ANONYMIZE"
                    className="mt-2 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2.5 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                  <div className="mt-4 flex justify-end gap-3">
                    <button
                      type="button"
                      onClick={() => { setAnonymizeModal(false); setAnonymizeConfirm('') }}
                      className="rounded-lg border border-outline-variant px-5 py-2.5 font-label-md text-label-md text-on-surface hover:bg-surface-container-low"
                    >
                      Cancel
                    </button>
                    <button
                      type="button"
                      onClick={handleAnonymize}
                      disabled={anonymizeConfirm !== 'ANONYMIZE' || actionLoading}
                      className="rounded-lg bg-error px-5 py-2.5 font-label-md text-label-md font-medium text-on-error transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50"
                    >
                      {actionLoading ? 'Anonymizing…' : 'Deactivate & anonymize'}
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
