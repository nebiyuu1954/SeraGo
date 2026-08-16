import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchJob, getApiErrorMessage, getStoredAuthTokens } from '../../../api'
import { useRequireRole, useSavedJobs } from '../../../hooks'
import type { JobResponse, JobType } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import SaveJobButton from '../../../components/dashboard/SaveJobButton.tsx'
import { JOB_TYPE_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { formatDate, timeAgo } from '../../../lib/date.ts'
import { initialsOf } from '../../../lib/initials.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'

function jobTypeLabel(job: JobResponse): string {
  return JOB_TYPE_LABELS[job.jobType as JobType] ?? 'Other'
}

/** One icon + caption + value row in the job overview grid. */
function Fact({
  icon,
  label,
  value,
}: {
  icon: string
  label: string
  value: string
}) {
  return (
    <div className="flex items-start gap-3">
      <span className="material-symbols-outlined mt-0.5 text-lg text-on-surface-variant">
        {icon}
      </span>
      <div className="min-w-0">
        <p className="font-label-sm text-label-sm uppercase tracking-wide text-on-surface-variant">
          {label}
        </p>
        <p
          className="mt-0.5 truncate font-body-md text-body-md font-medium text-on-surface"
          title={value}
        >
          {value}
        </p>
      </div>
    </div>
  )
}

/**
 * Renders the free-text description nicely: blank-line-separated paragraphs,
 * and lines starting with "-", "•" or "*" become a bulleted list.
 */
function Description({ text }: { text: string }) {
  const paragraphs = text
    .split(/\n{2,}/)
    .map((p) => p.trim())
    .filter(Boolean)
  if (paragraphs.length === 0) return null
  return (
    <div className="space-y-4">
      {paragraphs.map((paragraph, i) => {
        const lines = paragraph
          .split('\n')
          .map((l) => l.trim())
          .filter(Boolean)
        const isList =
          lines.length > 1 && lines.every((l) => /^[-•*]\s+/.test(l))
        if (isList) {
          return (
            <ul key={i} className="space-y-2">
              {lines.map((line, j) => (
                <li key={j} className="flex gap-2.5">
                  <span
                    aria-hidden="true"
                    className="mt-[0.55em] h-1.5 w-1.5 shrink-0 rounded-full bg-accent"
                  />
                  <span className="font-body-md text-body-md leading-relaxed text-on-surface">
                    {line.replace(/^[-•*]\s+/, '')}
                  </span>
                </li>
              ))}
            </ul>
          )
        }
        return (
          <p
            key={i}
            className="whitespace-pre-line font-body-md text-body-md leading-relaxed text-on-surface"
          >
            {lines.join('\n')}
          </p>
        )
      })}
    </div>
  )
}

/**
 * Talent's full view of a single job posting (opened from a job card).
 * Loads the job by id — hidden/draft jobs 404 from the API, so a missing
 * job just shows the error state with a way back to the job board.
 */
export default function JobDetailPage() {
  const { jobId } = useParams<{ jobId: string }>()
  const auth = useRequireRole('Talent')

  const [job, setJob] = useState<JobResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const brandLogo = job ? sourceLogo(job.sourceName) : null
  const { isSaved, toggleSaved } = useSavedJobs(auth.status === 'authenticated')

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens || auth.status !== 'authenticated' || !jobId) return
    let cancelled = false
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const j = await fetchJob(jobId, tokens.accessToken)
        if (!cancelled) setJob(j)
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
  }, [auth.status, jobId, refreshKey])

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
      <Link
        to="/dashboard/talent"
        className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to all jobs
      </Link>

      {loading && (
        <div className="mt-10 flex items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      )}

      {error && (
        <div
          role="alert"
          className="mt-6 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
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

      {!loading && !error && job && (
        <div className="mx-auto mt-6 max-w-4xl space-y-6">
          {/* Header: logo, title, and the apply CTA */}
          <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
            <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
              <div className="flex min-w-0 items-start gap-4">
                {brandLogo ? (
                  <span className="flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-surface-container-low">
                    <img
                      className="max-h-10 w-auto max-w-full object-contain"
                      src={brandLogo}
                      alt={job.sourceName ?? ''}
                    />
                  </span>
                ) : job.companyLogoUrl ? (
                  <span className="flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-surface-container-low">
                    <img
                      className="max-h-10 w-auto max-w-full object-contain"
                      src={job.companyLogoUrl}
                      alt=""
                    />
                  </span>
                ) : job.company ? (
                  <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-primary-container/60 font-headline-md text-headline-md font-semibold text-primary">
                    {initialsOf(job.company)}
                  </span>
                ) : null}
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
                    <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-on-surface">
                      {job.title}
                    </h1>
                    <span className="rounded-full bg-primary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-primary">
                      {jobTypeLabel(job)}
                    </span>
                  </div>
                  <p className="mt-1.5 font-body-lg text-body-lg font-medium text-on-surface-variant">
                    {job.company || 'Company undisclosed'}
                  </p>
                  {job.location && (
                    <p className="mt-2 flex items-center gap-1.5 font-body-md text-body-md text-on-surface-variant">
                      <span className="material-symbols-outlined text-base">
                        location_on
                      </span>
                      {job.location}
                    </p>
                  )}
                  {job.sourceName &&
                    job.sourceName.toLowerCase() !== 'serago' &&
                    (job.sourceUrl ? (
                      <a
                        href={job.sourceUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="mt-2 inline-flex items-center gap-1 font-label-md text-label-md text-primary transition-colors hover:text-surface-tint"
                      >
                        View original on {job.sourceName}
                        <span className="material-symbols-outlined text-base">
                          open_in_new
                        </span>
                      </a>
                    ) : (
                      <p className="mt-2 font-label-md text-label-md text-on-surface-variant">
                        via {job.sourceName}
                      </p>
                    ))}
                </div>
              </div>

              {job.url ? (
                <a
                  href={job.url}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex w-full shrink-0 items-center justify-center gap-1.5 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90 lg:w-auto"
                >
                  Apply now
                  <span className="material-symbols-outlined text-lg">
                    open_in_new
                  </span>
                </a>
              ) : (
                <span
                  className="inline-flex w-full shrink-0 cursor-not-allowed items-center justify-center gap-1.5 rounded-lg border border-outline-variant px-6 py-3 font-label-md text-label-md text-on-surface-variant/60 lg:w-auto"
                  title="No application link provided"
                >
                  Apply
                </span>
              )}
              <SaveJobButton
                saved={isSaved(job.id)}
                onToggle={() => toggleSaved(job.id)}
                className="w-full lg:w-auto"
              />
            </div>

            {/* Overview facts */}
            <div className="mt-7 grid grid-cols-1 gap-x-6 gap-y-5 border-t border-surface-variant pt-6 sm:grid-cols-2 lg:grid-cols-3">
              <Fact
                icon="schedule"
                label="Job type"
                value={jobTypeLabel(job)}
              />
              {job.salary && (
                <Fact icon="payments" label="Salary" value={job.salary} />
              )}
              {job.sectorName && (
                <Fact icon="domain" label="Sector" value={job.sectorName} />
              )}
              {job.experienceLevel && (
                <Fact
                  icon="work"
                  label="Experience"
                  value={job.experienceLevel}
                />
              )}
              {job.publishedAt && (
                <Fact
                  icon="history"
                  label="Posted"
                  value={timeAgo(job.publishedAt)}
                />
              )}
              {job.deadline && (
                <Fact
                  icon="event"
                  label="Closes"
                  value={formatDate(job.deadline)}
                />
              )}
            </div>
          </div>

          {/* Full description */}
          <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
            <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
              Job description
            </h2>
            <div className="mt-4">
              {job.description ? (
                <Description text={job.description} />
              ) : (
                <p className="font-body-md text-body-md text-on-surface-variant">
                  No description provided for this role.
                </p>
              )}
            </div>
          </div>
        </div>
      )}
    </DashboardShell>
  )
}
