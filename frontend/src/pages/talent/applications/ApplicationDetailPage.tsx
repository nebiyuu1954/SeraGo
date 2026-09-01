import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { fetchApplication, getApiErrorMessage, getStoredAuthTokens } from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { ApplicationResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import RichTextDisplay from '../../../components/ui/RichTextDisplay'
import ResumeLink from '../../../components/ui/ResumeLink.tsx'
import { statusBadge } from '../../../lib/statusBadge'

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}


// ────────────────────── page ──────────────────────

export default function ApplicationDetailPage() {
  const { applicationId } = useParams<{ applicationId: string }>()
  const navigate = useNavigate()
  const auth = useRequireRole('Talent')

  const [application, setApplication] = useState<ApplicationResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (auth.status !== 'authenticated' || !applicationId) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    let cancelled = false
    setLoading(true)
    setError(null)

    fetchApplication(applicationId, tokens.accessToken)
      .then((app) => {
        if (!cancelled) setApplication(app)
      })
      .catch((err) => {
        if (!cancelled) setError(getApiErrorMessage(err))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => { cancelled = true }
  }, [auth.status, applicationId])

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

  const badge = application ? statusBadge(application.status) : null
  const withProfile = application?.profileShared ?? false

  return (
    <DashboardShell role="Talent" authUser={auth.user}>
      {/* Back button */}
      <button
        type="button"
        onClick={() => navigate('/dashboard/talent/applications')}
        className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to applications
      </button>

      {/* Loading */}
      {loading && (
        <div className="flex min-h-[40svh] items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      )}

      {/* Error */}
      {error && (
        <div
          role="alert"
          className="mt-6 rounded-xl border border-error/30 bg-error-container px-4 py-8 text-center"
        >
          <span className="material-symbols-outlined text-3xl text-on-error-container">error</span>
          <p className="mt-2 font-body-md text-body-md text-on-error-container">{error}</p>
          <button
            type="button"
            onClick={() => navigate('/dashboard/talent/applications')}
            className="mt-4 rounded-lg border border-on-error-container/30 px-4 py-2 font-label-md text-label-md text-on-error-container transition-colors hover:bg-on-error-container/10"
          >
            Go back
          </button>
        </div>
      )}

      {/* Content */}
      {!loading && !error && application && (
        <div className="mt-6 flex flex-col gap-6">
          {/* Main card */}
          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-6 md:p-8">
            {/* Job + Status row */}
            <div>
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                Applied for
              </p>
              <Link
                to={`/dashboard/talent/jobs/${application.jobId}`}
                target="_blank"
                rel="noopener noreferrer"
                className="mt-1 inline-flex items-center gap-2 font-headline-md text-headline-md font-semibold text-primary transition-colors hover:text-surface-tint"
              >
                {application.jobTitle}
                <span className="material-symbols-outlined text-lg">open_in_new</span>
              </Link>
              {application.jobCompany && (
                <p className="mt-0.5 font-body-md text-body-md text-on-surface-variant">
                  {application.jobCompany}
                  {application.jobLocation ? ` · ${application.jobLocation}` : ''}
                </p>
              )}
            </div>

            {/* Dates + Status + Applied with row */}
            <div className="mt-6 flex flex-wrap items-start justify-between gap-6 border-t border-surface-variant pt-6">
              <div className="flex flex-wrap items-start gap-6">
                <div>
                  <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                    Applied
                  </p>
                  <p className="mt-1 font-body-md text-body-md text-on-surface">
                    {formatDateTime(application.appliedAt)}
                  </p>
                </div>
                {badge && (
                  <div>
                    <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                      Status
                    </p>
                    <span
                      className={`mt-1.5 inline-flex items-center rounded-full px-3 py-1 font-label-md text-label-md font-medium ${badge.className}`}
                    >
                      {badge.label}
                    </span>
                  </div>
                )}
                {application.statusUpdatedAt && (
                  <div>
                    <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                      Last updated
                    </p>
                    <p className="mt-1 font-body-md text-body-md text-on-surface">
                      {formatDateTime(application.statusUpdatedAt)}
                    </p>
                  </div>
                )}
              </div>
              <div>
                <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                  Applied with
                </p>
                <div className="mt-1.5 flex flex-wrap gap-3">
                  {application.resumeUrl && (
                    <ResumeLink
                      resumeUrl={application.resumeUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-2 rounded-lg border border-primary/30 bg-primary-container/10 px-4 py-2.5 font-label-md text-label-md font-medium text-primary transition-colors hover:bg-primary-container/20"
                    >
                      <span className="material-symbols-outlined text-lg">description</span>
                      View applied resume
                      <span className="material-symbols-outlined text-sm">open_in_new</span>
                    </ResumeLink>
                  )}
                  {withProfile && (
                    <Link
                      to={`/dashboard/talent/applications/${application.id}/profile`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-2 rounded-lg border border-primary/30 bg-primary-container/10 px-4 py-2.5 font-label-md text-label-md font-medium text-primary transition-colors hover:bg-primary-container/20"
                    >
                      <span className="material-symbols-outlined text-lg">person</span>
                      View applied profile
                      <span className="material-symbols-outlined text-sm">chevron_right</span>
                    </Link>
                  )}
                  {!application.resumeUrl && !withProfile && (
                    <p className="font-body-md text-body-md text-on-surface-variant italic">
                      No profile or resume submitted
                    </p>
                  )}
                </div>
              </div>
            </div>

            {/* Cover letter */}
            {application.coverLetter && (
              <div className="mt-6 border-t border-surface-variant pt-6">
                <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                  Cover letter
                </p>
                <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-low p-5">
                  <RichTextDisplay html={application.coverLetter} />
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </DashboardShell>
  )
}
