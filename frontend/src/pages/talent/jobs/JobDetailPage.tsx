import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchJob, getApiErrorMessage, getStoredAuthTokens } from '../../../api'
import { useRequireRole, useSavedJobs } from '../../../hooks'
import type { JobResponse, JobType } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import SaveJobButton from '../../../components/dashboard/SaveJobButton.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import { JOB_TYPE_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { formatDate, postedLabel } from '../../../lib/date.ts'
import {
  cleanEmploymentTypeSection,
  extractOverviewExtras,
  highlightLabels,
  parseDescriptionSections,
  splitInlineList,
  type DescriptionSection,
} from '../../../lib/descriptionSections.ts'
import { initialsOf } from '../../../lib/initials.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'

const DAY_MS = 86_400_000

function jobTypeLabel(job: JobResponse): string {
  return JOB_TYPE_LABELS[job.jobType as JobType] ?? 'Other'
}

function workModeLabel(mode: string): string {
  switch (mode) {
    case 'remote':
      return 'Remote'
    case 'hybrid':
      return 'Hybrid'
    default:
      return 'Onsite'
  }
}

/** \"2026-09-15T14:00:00Z\" → \"Closes today\" / \"Closes in 5 days\" — or null. */
function closesLabel(deadline: string | null): string | null {
  if (!deadline) return null
  const days = Math.ceil((new Date(deadline).getTime() - Date.now()) / DAY_MS)
  if (days <= 0) return 'Closes today'
  if (days === 1) return 'Closes tomorrow'
  return `Closes in ${days} days`
}

/** One icon + caption + value row in the facts sidebar. */
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
          className="mt-0.5 font-body-md text-body-md font-medium text-on-surface"
          title={value}
        >
          {value}
        </p>
      </div>
    </div>
  )
}

/** Bolds recognized keyword labels ("Salary:", "Education Qualification:") in text. */
function LabeledText({ text }: { text: string }) {
  const segments = highlightLabels(text)
  if (segments.length <= 1) return <>{text}</>
  return (
    <>
      {segments.map((seg, i) =>
        seg.isLabel ? (
          <strong key={i} className="font-semibold text-on-surface">
            {seg.text}
          </strong>
        ) : (
          <span key={i}>{seg.text}</span>
        ),
      )}
    </>
  )
}

/** A dotted bullet list, optionally with bolded labels inside items. */
function BulletList({ items }: { items: string[] }) {
  return (
    <ul className="space-y-2">
      {items.map((item, j) => (
        <li key={j} className="flex gap-2.5">
          <span
            aria-hidden="true"
            className="mt-[0.55em] h-1.5 w-1.5 shrink-0 rounded-full bg-accent"
          />
          <span className="font-body-md text-body-md leading-relaxed text-on-surface">
            <LabeledText text={item} />
          </span>
        </li>
      ))}
    </ul>
  )
}

/**
 * Renders a raw chunk of description text: blank-line-separated paragraphs,
 * bulleted and numbered lists (both line-based and inline " • "/" - "
 * separators), with recognized keyword labels rendered bold.
 */
function SectionContent({ text }: { text: string }) {
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
        const isBulletList =
          lines.length > 1 && lines.every((l) => /^[-•*·]\s+/.test(l))
        if (isBulletList) {
          return (
            <BulletList
              key={i}
              items={lines.map((l) => l.replace(/^[-•*·]\s+/, ''))}
            />
          )
        }
        // Numbered sub-sections ("1. Strategic Leadership & Business
        // Growth" + its continuation lines) become an ordered list.
        const isNumberedList =
          lines.filter((l) => /^\d+[.)]\s+/.test(l)).length >= 2
        if (isNumberedList) {
          const items: string[] = []
          for (const line of lines) {
            if (/^\d+[.)]\s+/.test(line)) {
              items.push(line.replace(/^\d+[.)]\s+/, ''))
            } else if (items.length > 0) {
              items[items.length - 1] += ` ${line}`
            }
          }
          return (
            <ol key={i} className="list-decimal space-y-2 pl-5">
              {items.map((item, j) => (
                <li
                  key={j}
                  className="font-body-md text-body-md leading-relaxed text-on-surface"
                >
                  <LabeledText text={item} />
                </li>
              ))}
            </ol>
          )
        }
        // Lists packed onto one line (HaHu's "A - B - C", Afriwork's
        // "A • B • C") become a bulleted list.
        const inline = splitInlineList(paragraph)
        if (inline) return <BulletList key={i} items={inline} />
        return (
          <p
            key={i}
            className="whitespace-pre-line font-body-md text-body-md leading-relaxed text-on-surface"
          >
            <LabeledText text={lines.join('\n')} />
          </p>
        )
      })}
    </div>
  )
}

/** One parsed description section: icon + heading + content. */
function DescriptionSection({ section }: { section: ReturnType<typeof parseDescriptionSections>[number] }) {
  return (
    <section>
      {section.heading && (
        <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
          <span className="material-symbols-outlined text-xl text-primary">
            {section.icon}
          </span>
          {section.heading}
        </h3>
      )}
      <div className={section.heading ? 'mt-3' : undefined}>
        <SectionContent text={section.content} />
      </div>
    </section>
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
  const { showToast } = useToast()

  // Parsed description sections. A "How to apply" section with real
  // instructions is pulled out of the body and shown in its own CTA block at
  // the bottom — jobs without specific instructions just use the Apply now
  // button at the top.
  const descriptionSections = job?.description
    ? parseDescriptionSections(job.description)
    : []
  const applySections = descriptionSections.filter(
    (s) => s.heading === 'How to apply' && s.content.trim().length > 0,
  )
  // Salary and vacancies are shown as description sections (the job's own
  // labels, or synthesized from the employment-type blob / the synced
  // salary). Sections that just repeat an overview fact (location, deadline,
  // job type) are dropped from the body.
  const overviewExtras = extractOverviewExtras(descriptionSections)
  const bodySections: DescriptionSection[] = descriptionSections
    .filter((s) => {
      if (s.heading === 'Location & workplace') return !job?.location
      if (s.heading === 'Deadline') return !job?.deadline
      return true
    })
    .map((s) =>
      s.heading === 'Employment type'
        ? cleanEmploymentTypeSection(s)
        : s,
    )
    .filter(
      (s): s is DescriptionSection =>
        s !== null && s.content.trim().length > 0,
    )
    .filter((s) => s.heading !== 'How to apply')

  // Make sure salary and vacancies always surface as sections — a real
  // "Salary & compensation"/"Vacancies" section from the description wins;
  // otherwise build one from the synced salary or the employment-blob text.
  const hasSalarySection = bodySections.some(
    (s) => s.heading === 'Salary & compensation' || s.heading === 'Salary',
  )
  const hasVacanciesSection = bodySections.some(
    (s) => s.heading === 'Vacancies',
  )
  const synthesized: DescriptionSection[] = []
  const salaryValue = job?.salary || overviewExtras.salary
  if (salaryValue && !hasSalarySection) {
    synthesized.push({
      heading: 'Salary',
      icon: 'payments',
      content: salaryValue,
    })
  }
  if (overviewExtras.vacancies && !hasVacanciesSection) {
    synthesized.push({
      heading: 'Vacancies',
      icon: 'group',
      content: overviewExtras.vacancies,
    })
  }
  const descriptionBody = [...synthesized, ...bodySections]

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
        <div className="mx-auto mt-6 max-w-5xl space-y-6">
          {/* Header: logo, title, badges, and the primary actions */}
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
                  <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-on-surface">
                    {job.title}
                  </h1>
                  <p className="mt-1.5 font-body-lg text-body-lg font-medium text-on-surface-variant">
                    {job.company || 'Company undisclosed'}
                  </p>
                  <div className="mt-2.5 flex flex-wrap items-center gap-2">
                    <span className="rounded-full bg-primary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-primary">
                      {jobTypeLabel(job)}
                    </span>
                    {job.workMode === 'remote' || job.workMode === 'hybrid' ? (
                      <span className="rounded-full bg-accent/10 px-3 py-1 font-label-sm text-label-sm font-medium text-accent">
                        {workModeLabel(job.workMode)}
                      </span>
                    ) : null}
                    {closesLabel(job.deadline) && (
                      <span
                        className={
                          new Date(job.deadline!).getTime() - Date.now() <=
                          7 * DAY_MS
                            ? 'rounded-full bg-error-container/70 px-3 py-1 font-label-sm text-label-sm font-medium text-on-error-container'
                            : 'rounded-full bg-tertiary-container/70 px-3 py-1 font-label-sm text-label-sm font-medium text-on-tertiary-container'
                        }
                      >
                        {closesLabel(job.deadline)}
                      </span>
                    )}
                  </div>
                  {job.location && (
                    <p className="mt-2.5 flex items-center gap-1.5 font-body-md text-body-md text-on-surface-variant">
                      <span className="material-symbols-outlined text-base">
                        location_on
                      </span>
                      {job.location}
                    </p>
                  )}
                </div>
              </div>

              <div className="flex shrink-0 flex-col gap-2 sm:flex-row lg:w-44 lg:flex-col">
                {job.url ? (
                  <a
                    href={job.url}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90"
                  >
                    Apply now
                    <span className="material-symbols-outlined text-lg">
                      open_in_new
                    </span>
                  </a>
                ) : (
                  <span
                    className="inline-flex w-full cursor-not-allowed items-center justify-center gap-1.5 rounded-lg border border-outline-variant px-6 py-3 font-label-md text-label-md text-on-surface-variant/60"
                    title="No application link provided"
                  >
                    Apply
                  </span>
                )}
                <SaveJobButton
                  saved={isSaved(job.id)}
                  onToggle={() => {
                    toggleSaved(job.id).then((result) => {
                      if (result === 'saved') showToast('Job saved for later')
                      else if (result === 'removed')
                        showToast('Removed from saved jobs')
                      else showToast('Could not update saved jobs', 'error')
                    })
                  }}
                  className="w-full"
                />
              </div>
            </div>
          </div>

          {/* Body: description (main) + key facts (sticky sidebar) */}
          <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
            {/* Facts first on mobile; right column on desktop */}
            <aside className="space-y-6 lg:order-2 lg:sticky lg:top-6">
              <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
                <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                  Job overview
                </h2>
                <div className="mt-5 space-y-5">
                  <Fact
                    icon="location_on"
                    label="Location"
                    value={job.location || 'Not specified'}
                  />
                  <Fact
                    icon="home_work"
                    label="Work arrangement"
                    value={workModeLabel(job.workMode)}
                  />
                  <Fact
                    icon="schedule"
                    label="Job type"
                    value={jobTypeLabel(job)}
                  />

                  {job.experienceLevel && (
                    <Fact
                      icon="work"
                      label="Experience"
                      value={job.experienceLevel}
                    />
                  )}
                  {job.sectorName && (
                    <Fact
                      icon="domain"
                      label="Sector"
                      value={job.sectorName}
                    />
                  )}
                  {(job.publishedAt || job.refreshedAt) && (
                    <Fact
                      icon="history"
                      label={
                        job.refreshedAt &&
                        (!job.publishedAt ||
                          new Date(job.refreshedAt) >
                            new Date(job.publishedAt))
                          ? 'Refreshed'
                          : 'Posted'
                      }
                      value={
                        postedLabel(job.publishedAt, job.refreshedAt) ||
                        'Recently'
                      }
                    />
                  )}
                  {job.deadline && (
                    <Fact
                      icon="event"
                      label="Closes"
                      value={`${formatDate(job.deadline)} (${
                        closesLabel(job.deadline) ?? ''
                      })`}
                    />
                  )}
                  {job.sourceName &&
                    job.sourceName.toLowerCase() !== 'serago' && (
                      <Fact
                        icon="language"
                        label="Source"
                        value={`via ${job.sourceName}`}
                      />
                    )}
                </div>
              </div>

              {job.sourceName &&
                job.sourceName.toLowerCase() !== 'serago' &&
                job.sourceUrl && (
                  <a
                    href={job.sourceUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center justify-center gap-1.5 rounded-xl border border-outline-variant bg-surface-container-lowest px-4 py-3 font-label-md text-label-md text-primary shadow-sm transition-colors hover:bg-surface-container-low"
                  >
                    View original on {job.sourceName}
                    <span className="material-symbols-outlined text-base">
                      open_in_new
                    </span>
                  </a>
                )}
            </aside>

            {/* Main: parsed description sections */}
            <div className="min-w-0 lg:order-1">
              <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
                <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                  Job description
                </h2>
                <div className="mt-5 space-y-8">
                  {job.description ? (
                    descriptionBody.map((section, i) => (
                      <DescriptionSection
                        key={`${section.heading ?? 'intro'}-${i}`}
                        section={section}
                      />
                    ))
                  ) : (
                    <p className="font-body-md text-body-md text-on-surface-variant">
                      No description provided for this role.
                    </p>
                  )}
                </div>
              </div>

              {/* How to apply CTA — only when the employer posted specific
                  instructions; otherwise the Apply now button at the top is
                  the application path. */}
              {applySections.length > 0 && (
                <div className="mt-6 rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
                  <h2 className="flex items-center gap-2 font-headline-md text-headline-md font-bold text-on-surface">
                    <span className="material-symbols-outlined text-2xl text-primary">
                      how_to_reg
                    </span>
                    How to apply
                  </h2>
                  <div className="mt-3 space-y-4">
                    {applySections.map((section, i) => (
                      <SectionContent key={i} text={section.content} />
                    ))}
                  </div>
                  {job.url && (
                    <a
                      href={job.url}
                      target="_blank"
                      rel="noreferrer"
                      className="mt-5 inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-accent px-6 py-3.5 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-opacity hover:opacity-90 sm:w-auto"
                    >
                      Apply now
                      <span className="material-symbols-outlined text-lg">
                        open_in_new
                      </span>
                    </a>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </DashboardShell>
  )
}
