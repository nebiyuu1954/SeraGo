import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getApiErrorMessage } from '../../../api'
import { useMyApplicationsQuery } from '../../../hooks/query.ts'
import { useRequireRole } from '../../../hooks'
import type { ApplicationResponse, ApplicationStatus } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import ResumeLink from '../../../components/ui/ResumeLink.tsx'

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
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

/**
 * Talent's applications page: shows all jobs the talent has applied to,
 * with status, applied date, and a link to the job detail.
 */
export default function ApplicationsPage() {
  const auth = useRequireRole('Talent')
  const { applications, pagination, isLoading, error, refresh } = useMyApplicationsQuery(
    auth.status === 'authenticated',
  )
  const [detailApp, setDetailApp] = useState<ApplicationResponse | null>(null)

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

      {error && (
        <div
          role="alert"
          className="mt-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
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

      <div className="mt-6 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
        {isLoading ? (
          <SkeletonRows />
        ) : error ? null : applications.length === 0 ? (
          <EmptyState />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="border-b border-surface-variant bg-surface-container-low">
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Job
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Company
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Location
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Status
                  </th>
                  <th className="px-6 py-4 font-label-md text-label-md font-semibold text-on-surface-variant">
                    Applied
                  </th>
                  <th className="px-6 py-4 text-right font-label-md text-label-md font-semibold text-on-surface-variant">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-variant">
                {applications.map((app) => (
                  <ApplicationRow
                    key={app.id}
                    application={app}
                    onViewDetails={() => setDetailApp(app)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}

        {pagination && pagination.totalCount > 0 && (
          <div className="flex items-center justify-between border-t border-surface-variant bg-surface-bright px-6 py-4">
            <span className="font-label-md text-label-md text-on-surface-variant">
              {pagination.totalCount} application{pagination.totalCount === 1 ? '' : 's'}
            </span>
          </div>
        )}
      </div>

      {/* Application details dialog */}
      {detailApp && (
        <ApplicationDetailDialog
          application={detailApp}
          onClose={() => setDetailApp(null)}
        />
      )}
    </DashboardShell>
  )
}

function ApplicationRow({
  application,
  onViewDetails,
}: {
  application: ApplicationResponse
  onViewDetails: () => void
}) {
  const badge = statusBadge(application.status)

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
            onClick={onViewDetails}
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

/** Modal dialog showing full application details for the talent. */
function ApplicationDetailDialog({
  application,
  onClose,
}: {
  application: ApplicationResponse
  onClose: () => void
}) {
  const badge = statusBadge(application.status)

  return (
    <>
      <div
        className="fixed inset-0 z-50 bg-inverse-surface/50"
        onClick={onClose}
      />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div
          className="w-full max-w-lg rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex items-center justify-between">
            <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
              Application details
            </h2>
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container"
            >
              <span className="material-symbols-outlined">close</span>
            </button>
          </div>

          {/* Job info */}
          <div className="mt-5 rounded-xl border border-surface-variant bg-surface-container-low p-4">
            <Link
              to={`/dashboard/talent/jobs/${application.jobId}`}
              className="font-body-lg font-semibold text-primary transition-colors hover:text-surface-tint"
            >
              {application.jobTitle}
            </Link>
            <p className="mt-1 font-body-md text-on-surface-variant">
              {application.jobCompany || 'Company undisclosed'}
              {application.jobLocation ? ` · ${application.jobLocation}` : ''}
            </p>
          </div>

          {/* Status + Applied date */}
          <div className="mt-4 flex items-center gap-4">
            <div>
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                Status
              </p>
              <span
                className={`mt-1 inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium ${badge.className}`}
              >
                {badge.label}
              </span>
            </div>
            <div>
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                Applied
              </p>
              <p className="mt-1 font-body-md text-body-md text-on-surface">
                {formatDateTime(application.appliedAt)}
              </p>
            </div>
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

          {/* Cover letter */}
          <div className="mt-5">
            <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
              Cover letter
            </p>
            {application.coverLetter ? (
              <div className="mt-2 whitespace-pre-wrap rounded-xl border border-surface-variant bg-surface-container-low p-4 font-body-md text-body-md text-on-surface">
                {application.coverLetter}
              </div>
            ) : (
              <p className="mt-2 font-body-md text-body-md text-on-surface-variant italic">
                No cover letter submitted
              </p>
            )}
          </div>

          {/* Resume */}
          {application.resumeUrl && (
            <div className="mt-4">
              <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
                Resume
              </p>
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

          <div className="mt-6 flex justify-end">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-outline-variant bg-surface-container-lowest px-5 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </>
  )
}

function SkeletonRows() {
  return (
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
  )
}

function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
        <span className="material-symbols-outlined text-3xl">description</span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        No applications yet
      </h2>
      <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
        Browse Serago jobs and apply to start tracking your applications here.
      </p>
      <Link
        to="/dashboard/talent"
        className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
      >
        Browse jobs
      </Link>
    </div>
  )
}
