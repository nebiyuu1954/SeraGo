import { Link, useLocation } from 'react-router-dom'
import type { JobResponse, JobType } from '../../types'
import { JOB_TYPE_LABELS } from '../dashboard/jobOptions.ts'
import { formatDate, postedLabel } from '../../lib/date.ts'
import { initialsOf } from '../../lib/initials.ts'
import { sourceLogo } from '../../lib/sourceLogos.ts'
import { parseSkills } from '../../lib/sourceCapabilities.ts'
import SaveJobButton from '../dashboard/SaveJobButton.tsx'
import seragoLogo from '../../assets/logos/serago.svg'
import { prefetchJob } from '../../hooks/useJobDetailQuery.ts'

function jobTypeLabel(job: JobResponse): string {
  return JOB_TYPE_LABELS[job.jobType as JobType] ?? 'Other'
}

export function MatchScoreChip({
  score,
  matched = 0,
  missing = 0,
  compact = false,
}: {
  score: number
  matched?: number
  missing?: number
  compact?: boolean
}) {
  const tone =
    score >= 75
      ? 'bg-success/10 text-success'
      : score >= 50
        ? 'bg-amber-100 text-amber-800'
        : 'bg-surface-container-low text-on-surface-variant'
  const detail = [
    'Match score based on your profile and For You preferences.',
    matched > 0 ? `${matched} matched` : '',
    missing > 0 ? `${missing} missing` : '',
  ]
    .filter(Boolean)
    .join(' · ')
  return (
    <span
      title={detail}
      className={`ml-auto inline-flex shrink-0 items-center gap-1 self-start rounded-full ${
        compact ? 'px-2.5 py-0.5 text-[11px]' : 'px-3 py-1 font-label-sm text-label-sm'
      } font-semibold ${tone}`}
    >
      <span className="material-symbols-outlined text-sm">auto_awesome</span>
      {score}% match
    </span>
  )
}

export interface JobCardProps {
  job: JobResponse
  saved?: boolean
  applied?: boolean
  onToggleSave?: () => void
  onApply?: (job: JobResponse) => void
  onPreview?: (job: JobResponse) => void
  detailPath?: string
  onPrefetch?: (id: string) => void
  compact?: boolean
}

export default function JobCard({
  job,
  saved,
  applied,
  onToggleSave,
  onApply,
  onPreview,
  detailPath,
  onPrefetch,
  compact,
}: JobCardProps) {
  const location = useLocation()
  const isDashboard = location.pathname.startsWith('/dashboard')
  // On public pages (/jobs and homepage) where --ui-scale: 0.72 is NOT active,
  // compact mode applies the exact rendered font/padding sizes (16px title, 13px body,
  // 12px meta, p-4) so the cards look identical in visual density to the dashboard.
  const isCompact = compact ?? !isDashboard

  const brandLogo = sourceLogo(job.sourceName)
  // Build structured salary display: prefer min/max range, fall back to free text.
  const salaryLabel =
    job.salaryMin != null || job.salaryMax != null
      ? [
          job.salaryCurrency || '',
          job.salaryMin != null && job.salaryMax != null
            ? `${job.salaryMin.toLocaleString()} – ${job.salaryMax.toLocaleString()}`
            : job.salaryMin != null
              ? `From ${job.salaryMin.toLocaleString()}`
              : `Up to ${job.salaryMax!.toLocaleString()}`,
          job.salaryPeriod ? `/${job.salaryPeriod}` : '',
        ]
          .filter(Boolean)
          .join(' ')
      : job.salary || ''

  // Build experience display: prefer numeric range, fall back to text.
  const experienceLabel =
    job.experienceMinYears != null || job.experienceMaxYears != null
      ? job.experienceMinYears != null && job.experienceMaxYears != null
        ? `${job.experienceMinYears}–${job.experienceMaxYears} years`
        : job.experienceMinYears != null
          ? `${job.experienceMinYears}+ years`
          : `Up to ${job.experienceMaxYears} years`
      : job.experienceLevel || ''

  const meta = [
    {
      icon: 'location_on',
      label: job.location || 'Location not specified',
    },
    ...(salaryLabel
      ? [{ icon: 'payments', label: salaryLabel }]
      : [{ icon: 'payments', label: 'Salary not specified' }]),
    { icon: 'schedule', label: jobTypeLabel(job) },
    ...(experienceLabel ? [{ icon: 'work', label: experienceLabel }] : []),
    ...(job.sectorName ? [{ icon: 'domain', label: job.sectorName }] : []),
    ...(job.numberOfPositions > 1
      ? [{ icon: 'group', label: `${job.numberOfPositions} positions` }]
      : []),
    {
      icon: 'history',
      label: postedLabel(job.publishedAt, job.refreshedAt) || 'Posted recently',
    },
    ...(job.deadline
      ? [{ icon: 'event', label: `Closes ${formatDate(job.deadline)}` }]
      : []),
  ]

  const skills = parseSkills(job.skills).slice(0, 4) // show max 4 chips on card

  return (
    <article
      className="group flex flex-col overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest transition-all duration-300 hover:border-outline-variant hover:shadow-sm"
      onMouseEnter={() => {
        onPrefetch?.(job.id)
        prefetchJob(job.id)
      }}
    >
      {/* Header: company mark + title */}
      <div
        className={`flex flex-grow flex-row items-center border-b border-surface-variant/50 ${
          isCompact ? 'gap-3.5 p-4' : 'gap-4 p-5'
        }`}
      >
        <div
          className={`flex shrink-0 items-center justify-center overflow-hidden rounded bg-surface-container-low px-2 ${
            isCompact ? 'h-10 w-20' : 'h-11 w-24'
          }`}
        >
          {brandLogo ? (
            <img
              className={`${isCompact ? 'max-h-7' : 'max-h-8'} w-auto max-w-full object-contain`}
              src={brandLogo}
              alt={job.sourceName ?? ''}
            />
          ) : job.companyLogoUrl ? (
            <img
              className={`${isCompact ? 'max-h-7' : 'max-h-8'} w-auto max-w-full object-contain`}
              src={job.companyLogoUrl}
              alt=""
            />
          ) : job.company ? (
            <span
              className={`${
                isCompact ? 'text-[15px]' : 'font-headline-md text-headline-md'
              } font-semibold text-primary`}
            >
              {initialsOf(job.company)}
            </span>
          ) : (
            <img
              className={`${isCompact ? 'max-h-7' : 'max-h-8'} w-auto max-w-full object-contain`}
              src={seragoLogo}
              alt="SeraGo"
            />
          )}
        </div>
        <div className="min-w-0 flex-1">
          <h4
            className={`truncate font-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint ${
              isCompact ? 'mb-0.5 text-[16px] font-semibold' : 'mb-1 text-headline-md'
            }`}
          >
            {job.title}
          </h4>
          <p
            className={`truncate text-secondary ${
              isCompact ? 'text-[13px]' : 'font-body-md text-body-md'
            }`}
          >
            {job.company || 'Company undisclosed'}
          </p>
          {job.sourceName &&
            job.sourceName.toLowerCase() !== 'serago' &&
            (job.sourceUrl ? (
              <a
                href={job.sourceUrl}
                target="_blank"
                rel="noreferrer"
                className={`mt-0.5 inline-flex items-center gap-1 text-primary transition-colors hover:text-surface-tint ${
                  isCompact ? 'text-[11px]' : 'font-label-sm text-label-sm'
                }`}
              >
                via {job.sourceName}
                <span className="material-symbols-outlined text-sm">
                  open_in_new
                </span>
              </a>
            ) : (
              <p
                className={`mt-0.5 text-on-surface-variant ${
                  isCompact ? 'text-[11px]' : 'font-label-sm text-label-sm'
                }`}
              >
                via {job.sourceName}
              </p>
            ))}
        </div>
        {job.matchScore != null && (
          <MatchScoreChip
            score={job.matchScore}
            matched={job.matchedSkills?.length ?? 0}
            missing={job.missingSkills?.length ?? 0}
            compact={isCompact}
          />
        )}
      </div>

      {/* Meta grid + actions */}
      <div className={`flex flex-col bg-surface/30 ${isCompact ? 'gap-3.5 p-4' : 'gap-4 p-5'}`}>
        <div
          className={`grid grid-cols-2 text-on-surface-variant ${
            isCompact ? 'gap-2 text-[12px]' : 'gap-3 font-label-md text-label-md'
          }`}
        >
          {meta.map((item) => (
            <div key={item.icon + item.label} className="flex items-center gap-2 truncate">
              <span
                className={`material-symbols-outlined ${
                  isCompact ? 'text-[16px]' : 'text-[18px]'
                }`}
              >
                {item.icon}
              </span>
              <span className="truncate">{item.label}</span>
            </div>
          ))}
        </div>
        {skills.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {skills.map((skill) => (
              <span
                key={skill}
                className={`inline-flex items-center rounded-full bg-primary-container/40 text-primary ${
                  isCompact
                    ? 'px-2.5 py-0.5 text-[11px] font-medium'
                    : 'px-2.5 py-0.5 font-label-sm text-label-sm'
                }`}
              >
                {skill}
              </span>
            ))}
          </div>
        )}
        <div className="mt-auto flex gap-2 pt-0.5">
          {applied ? (
            <span
              className={`flex-1 rounded border border-success/30 bg-success/10 text-center font-semibold text-success ${
                isCompact ? 'px-3 py-2 text-[13px]' : 'px-4 py-2.5 font-label-md text-label-md'
              }`}
            >
              Applied
            </span>
          ) : onApply ? (
            <button
              type="button"
              onClick={() => onApply(job)}
              className={`flex-1 rounded bg-accent text-center font-semibold text-on-accent transition-colors hover:opacity-90 ${
                isCompact ? 'px-3 py-2 text-[13px]' : 'px-4 py-2.5 font-label-md text-label-md'
              }`}
            >
              Apply Now
            </button>
          ) : job.url ? (
            <a
              href={job.url}
              target="_blank"
              rel="noreferrer"
              className={`flex-1 rounded bg-accent text-center font-semibold text-on-accent transition-colors hover:opacity-90 ${
                isCompact ? 'px-3 py-2 text-[13px]' : 'px-4 py-2.5 font-label-md text-label-md'
              }`}
            >
              Apply Now
            </a>
          ) : (
            <span
              title="No application link provided"
              className={`flex-1 cursor-not-allowed rounded bg-accent text-center font-semibold text-on-accent opacity-60 ${
                isCompact ? 'px-3 py-2 text-[13px]' : 'px-4 py-2.5 font-label-md text-label-md'
              }`}
            >
              Apply Now
            </span>
          )}
          {onPreview ? (
            <button
              type="button"
              onClick={() => onPreview(job)}
              className={`flex-1 rounded border border-surface-variant text-center font-semibold text-primary transition-colors hover:bg-surface-container-low ${
                isCompact ? 'px-3 py-2 text-[13px]' : 'px-4 py-2.5 font-label-md text-label-md'
              }`}
            >
              See description
            </button>
          ) : (
            <Link
              to={detailPath ?? `/dashboard/talent/jobs/${job.id}`}
              className={`flex-1 rounded border border-surface-variant text-center font-semibold text-primary transition-colors hover:bg-surface-container-low ${
                isCompact ? 'px-3 py-2 text-[13px]' : 'px-4 py-2.5 font-label-md text-label-md'
              }`}
            >
              See description
            </Link>
          )}
          {onToggleSave && (
            <SaveJobButton
              saved={!!saved}
              onToggle={onToggleSave}
              className={isCompact ? 'px-3 py-2 text-[13px]' : undefined}
            />
          )}
        </div>
      </div>
    </article>
  )
}
