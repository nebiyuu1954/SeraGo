import { Link, useLocation, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import {
  applyToJob,
  getApiErrorMessage,
  getStoredAuthTokens,
  updateProfile,
} from '../../../api'
import { useJobDetailQuery, useMyApplicationsQuery, useProfileQuery, useSavedJobsQuery } from '../../../hooks/query.ts'
import { useRequireRoleAny } from '../../../hooks'
import type { JobResponse, JobType } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import SaveJobButton from '../../../components/dashboard/SaveJobButton.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import { JOB_TYPE_LABELS } from '../../../components/dashboard/jobOptions.ts'
import { formatDate, postedLabel } from '../../../lib/date.ts'
import { cn } from '../../../lib/cn.ts'
import FileUpload from '../../../components/ui/FileUpload.tsx'
import RichTextEditor from '../../../components/ui/RichTextEditor.tsx'
import RichTextDisplay from '../../../components/ui/RichTextDisplay'
import {
  highlightLabels,
  parseDescriptionSections,
  splitInlineList,
} from '../../../lib/descriptionSections.ts'
import SourceJobDescription from '../../../components/jobs/SourceJobDescription.tsx'
import { initialsOf } from '../../../lib/initials.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'
import { parseSkills } from '../../../lib/sourceCapabilities.ts'
import { saveJob, unsaveJob } from '../../../api'
import { trackEvent } from '../../../lib/analytics'

const DAY_MS = 86_400_000

/** A job is a "Serago job" when it was posted directly on the platform (not scraped). */
function isSeragoJob(job: JobResponse): boolean {
  return !job.sourceName || job.sourceName.toLowerCase() === 'serago'
}

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

/** "2026-09-15T14:00:00Z" → "Closes today" / "Closes in 5 days" — or null. */
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

/* ------------------------------------------------ Profile preview components */

function PreviewSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-surface-variant">
      <div className="border-b border-surface-variant px-4 py-2.5">
        <h3 className="font-label-md text-label-md font-semibold text-on-surface">
          {title}
        </h3>
      </div>
      <div className="divide-y divide-surface-variant">{children}</div>
    </div>
  )
}

function PreviewRow({ label, value }: { label: string; value?: string | null }) {
  if (!value) return null
  return (
    <div className="flex items-center justify-between px-4 py-2.5">
      <span className="font-body-sm text-sm text-on-surface-variant">{label}</span>
      <span className="font-body-sm text-sm font-medium text-on-surface">{value}</span>
    </div>
  )
}

function PreviewToggle({
  label,
  field,
  value,
  visibility,
  onToggle,
}: {
  label: string
  field: string
  value?: string | null
  visibility: Record<string, boolean>
  onToggle: (next: Record<string, boolean>) => void
}) {
  const isVisible = visibility[field] ?? true
  return (
    <div className="flex items-center justify-between px-4 py-2.5">
      <div className="min-w-0 flex-1">
        <span className="font-body-sm text-sm text-on-surface-variant">{label}</span>
        {value && (
          <p className={cn('mt-0.5 truncate font-body-sm text-sm', isVisible ? 'text-on-surface' : 'text-on-surface-variant/60 line-through')}>
            {value}
          </p>
        )}
        {!value && (
          <p className="mt-0.5 font-body-sm text-sm text-on-surface-variant/40 italic">Not set</p>
        )}
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={isVisible}
        onClick={() => onToggle({ ...visibility, [field]: !isVisible })}
        className={cn(
          'relative ml-3 inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors',
          isVisible ? 'bg-primary' : 'bg-surface-variant',
        )}
      >
        <span
          className={cn(
            'inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
            isVisible ? 'translate-x-4' : 'translate-x-0.5',
          )}
        />
      </button>
    </div>
  )
}

/**
 * Talent's full view of a single job posting (opened from a job card).
 * Uses SWR to cache the job by id — navigating away and back is instant.
 */
export default function JobDetailPage() {
  const { jobId } = useParams<{ jobId: string }>()
  // Admins may open any job to preview exactly what talents see — via the
  // /dashboard/admin/jobs/:jobId/preview route (or location.state.fromAdmin).
  const location = useLocation()
  const isPreview = location.pathname.startsWith('/dashboard/admin/')
    || (location.state as { fromAdmin?: boolean })?.fromAdmin === true
  const auth = useRequireRoleAny(['Talent', 'Admin'])

  const { job, isLoading, error, refresh: refreshJob } = useJobDetailQuery(
    jobId,
    auth.status === 'authenticated',
  )

  // The detail query is cache-first (revalidateOnMount: false) — it only
  // fetches when the jobs list has primed the cache via hover-prefetch.
  // Arriving directly (admin preview from the jobs table, hard refresh,
  // shared link) means nothing is cached, so trigger the first fetch
  // manually or the page renders blank.
  useEffect(() => {
    if (auth.status === 'authenticated' && !job && !isLoading && !error) {
      refreshJob()
    }
  }, [auth.status, job, isLoading, error, refreshJob])

  const { savedIds, refresh: refreshSaved } = useSavedJobsQuery(!isPreview)
  const brandLogo = job ? sourceLogo(job.sourceName) : null
  const { showToast } = useToast()

  // Application tracking — only for Serago jobs.
  const { applications, refresh: refreshApplications } = useMyApplicationsQuery(
    auth.status === 'authenticated' && !isPreview,
  )
  const [showApplyForm, setShowApplyForm] = useState(false)
  const [applyMode, setApplyMode] = useState<'coverletter' | 'profile'>('coverletter')
  const [coverLetter, setCoverLetter] = useState('')
  const [uploadedResumeUrl, setUploadedResumeUrl] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [applySuccess, setApplySuccess] = useState(false)
  const [applyError, setApplyError] = useState<string | null>(null)
  const [showProfilePreview, setShowProfilePreview] = useState(false)

  // Profile for the "use my profile" option.
  const { profile, refresh: refreshProfile } = useProfileQuery(
    auth.status === 'authenticated' && !isPreview,
  )

  // Local copy of visibility toggles for the preview dialog.
  const talent = profile?.talent
  const defaultVis = {
    phone: true, dateOfBirth: true, address: true, education: true,
    linkedin: true, github: true, portfolio: true, skills: true,
    experience: true, resume: true, avatar: true, middleName: true,
    city: true, country: true, currentIndustry: true, currentProfession: true,
    preferredLocations: true, about: true, desiredRoles: true,
    workMode: true, availability: true,
  }
  const [previewVisibility, setPreviewVisibility] = useState<Record<string, boolean>>(defaultVis)
  const [savingVisibility, setSavingVisibility] = useState(false)

  // Initialize preview visibility from profile when it loads.
  const profileLoaded = !!talent?.profileVisibility && talent.profileVisibility !== '{}'
  const [visibilityInit, setVisibilityInit] = useState(false)
  if (profileLoaded && !visibilityInit && talent) {
    try {
      const parsed = JSON.parse(talent.profileVisibility)
      setPreviewVisibility({ ...defaultVis, ...parsed })
      setVisibilityInit(true)
    } catch { setVisibilityInit(true) }
  }

  // Check if the user has already applied to this job.
  const hasApplied = job ? applications.some((a) => a.jobId === job.id) : false
  const isSerago = job ? isSeragoJob(job) : false

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

  const handleToggleSave = async () => {
    if (!job) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    const wasSaved = savedIds.has(job.id)
    try {
      if (wasSaved) await unsaveJob(job.id, tokens.accessToken)
      else await saveJob(job.id, tokens.accessToken)
      trackEvent(wasSaved ? 'unsave_job' : 'save_job', { job_id: job.id })
      refreshSaved()
      showToast(wasSaved ? 'Removed from saved jobs' : 'Job saved for later')
    } catch {
      showToast('Could not update saved jobs', 'error')
    }
  }

  const handleApply = async () => {
    if (!job) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    // Validate: resume is required in CV mode
    if (applyMode === 'coverletter') {
      const hasResume = !!uploadedResumeUrl || !!talent?.resumeUrl
      if (!hasResume) {
        setApplyError('Please upload a resume or use your profile resume.')
        return
      }
    }
    setApplyError(null)
    setSubmitting(true)
    try {
      const payload: { jobId: string; coverLetter?: string; resumeUrl?: string; shareProfile?: boolean } = {
        jobId: job.id,
      }
      if (coverLetter.trim() && coverLetter !== '<p></p>') payload.coverLetter = coverLetter
      // Use uploaded resume if available, otherwise use profile resume (only if resume visibility is on)
      if (uploadedResumeUrl) {
        payload.resumeUrl = uploadedResumeUrl
      } else if (applyMode === 'profile' && talent?.resumeUrl && (previewVisibility['resume'] ?? true)) {
        payload.resumeUrl = talent.resumeUrl
      }
      // Signal whether the talent is sharing their full profile
      if (applyMode === 'profile') {
        payload.shareProfile = true
      }
      await applyToJob(payload, tokens.accessToken)
      trackEvent('apply_job', { job_id: job.id })
      setApplySuccess(true)
      setShowApplyForm(false)
      setApplyMode('coverletter')
      setUploadedResumeUrl(null)
      setCoverLetter('')
      refreshApplications()
      showToast('Application submitted successfully!')
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error')
    } finally {
      setSubmitting(false)
    }
  }

  const handleSaveVisibility = async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setSavingVisibility(true)
    try {
      await updateProfile(tokens.accessToken, {
        talent: { profileVisibility: JSON.stringify(previewVisibility) },
      })
      refreshProfile()
      setShowProfilePreview(false)
      showToast('Profile visibility updated.')
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error')
    } finally {
      setSavingVisibility(false)
    }
  }

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
    <DashboardShell role={isPreview ? 'Admin' : 'Talent'} authUser={auth.user}>
      {isPreview ? (
        <div className="flex items-center gap-3">
          <Link
            to="/dashboard/admin/jobs"
            className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
          >
            <span className="material-symbols-outlined text-lg">arrow_back</span>
            Back to jobs
          </Link>
          <span className="inline-flex items-center gap-1.5 rounded-full bg-primary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-primary">
            <span className="material-symbols-outlined text-base">preview</span>
            Previewing as talent
          </span>
        </div>
      ) : (
        <Link
          to="/dashboard/talent"
          className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
        >
          <span className="material-symbols-outlined text-lg">arrow_back</span>
          Back to all jobs
        </Link>
      )}

      {isLoading && (
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
            <span>{getApiErrorMessage(error)}</span>
          </span>
          <button
            type="button"
            onClick={() => refreshJob()}
            className="rounded-lg border border-on-error-container/30 px-3 py-1.5 font-label-md text-label-md font-medium transition-colors hover:bg-on-error-container/10"
          >
            Retry
          </button>
        </div>
      )}

      {!isLoading && !error && job && (
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
                {isPreview ? (
                  // Admin preview: no apply/save actions — read-only view.
                  <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg border border-outline-variant px-6 py-3 font-label-md text-label-md text-on-surface-variant">
                    <span className="material-symbols-outlined text-lg">visibility</span>
                    Read-only preview
                  </span>
                ) : isSerago ? (
                  // Serago job: apply directly on the platform.
                  hasApplied || applySuccess ? (
                    <span className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg border border-primary/30 bg-primary-container/20 px-6 py-3 font-label-md text-label-md font-medium text-primary">
                      <span className="material-symbols-outlined text-lg">check_circle</span>
                      Applied
                    </span>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setShowApplyForm(true)}
                      className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90"
                    >
                      Apply on SeraGo
                      <span className="material-symbols-outlined text-lg">send</span>
                    </button>
                  )
                ) : job.url ? (
                  // External job: link to the original source.
                  <a
                    href={job.url}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90"
                  >
                    Apply on {job.sourceName || 'source'}
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
                {!isPreview && (
                  <SaveJobButton
                    saved={savedIds.has(job.id)}
                    onToggle={handleToggleSave}
                    className="w-full"
                  />
                )}
              </div>
            </div>
          </div>

          {/* Body: description (main) + key facts (sticky sidebar) */}
          <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
            {/* Facts first on mobile; right column on desktop */}
            <aside className="space-y-6 lg:order-2 lg:sticky lg:top-20">
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

              {/* Skills chips */}
              {parseSkills(job.skills).length > 0 && (
                <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
                  <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                    Skills
                  </h2>
                  <div className="mt-4 flex flex-wrap gap-2">
                    {parseSkills(job.skills).map((skill) => (
                      <span
                        key={skill}
                        className="inline-flex items-center rounded-full bg-primary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-primary"
                      >
                        {skill}
                      </span>
                    ))}
                  </div>
                </div>
              )}

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

            {/* Main: source-specific description */}
            <div className="min-w-0 lg:order-1">
              <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
                <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                  Job description
                </h2>
                <div className="mt-5">
                  <SourceJobDescription job={job} />
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

      {/* Apply form dialog for Serago jobs */}
      {showApplyForm && job && (
        <>
          <div
            className="fixed inset-0 z-50 bg-inverse-surface/50"
            onClick={() => { if (!submitting) { setShowApplyForm(false); setApplyError(null) } }}
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div
              className="w-full max-w-2xl rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl"
              onClick={(e) => e.stopPropagation()}
            >
              <div className="flex items-center justify-between">
                <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                  Apply to {job.title}
                </h2>
                <button
                  type="button"
                  onClick={() => { if (!submitting) { setShowApplyForm(false); setApplyError(null) } }}
                  className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container"
                >
                  <span className="material-symbols-outlined">close</span>
                </button>
              </div>
              <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                {job.company || 'Company undisclosed'} · {job.location || 'Location not specified'}
              </p>

              {/* Application mode toggle */}
              <div className="mt-5 flex rounded-lg border border-surface-variant bg-surface-container-lowest p-1">
                <button
                  type="button"
                  onClick={() => { setApplyMode('coverletter'); setApplyError(null) }}
                  className={cn(
                    'flex-1 rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
                    applyMode === 'coverletter'
                      ? 'bg-primary font-medium text-on-primary'
                      : 'text-on-surface-variant hover:text-on-surface',
                  )}
                >
                  <span className="material-symbols-outlined mr-1.5 align-middle text-lg">description</span>
                  Upload CV
                </button>
                <button
                  type="button"
                  onClick={() => { setApplyMode('profile'); setApplyError(null) }}
                  className={cn(
                    'flex-1 rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
                    applyMode === 'profile'
                      ? 'bg-primary font-medium text-on-primary'
                      : 'text-on-surface-variant hover:text-on-surface',
                  )}
                >
                  <span className="material-symbols-outlined mr-1.5 align-middle text-lg">person</span>
                  Use my profile
                </button>

              </div>

              {/* Resume upload — only in CV mode */}
              {applyMode === 'coverletter' && (
                <div className="mt-5 space-y-4">
                  <div className="flex flex-col sm:flex-row gap-3 items-stretch">
                    <div className={cn(talent?.resumeUrl && !uploadedResumeUrl ? 'sm:w-1/2' : 'w-full', 'flex flex-col')}
                    >
                      <FileUpload
                        fileType="resume"
                        value={uploadedResumeUrl ?? undefined}
                        onChange={(url) => setUploadedResumeUrl(url)}
                        label="Resume (PDF)"
                        className="flex-1"
                      />
                    </div>
                    {talent?.resumeUrl && !uploadedResumeUrl && (
                      <div className="sm:w-1/2 flex flex-col">
                        <div className="font-label-sm text-label-sm font-medium text-on-surface h-[22px]" />
                        <button
                          type="button"
                          onClick={() => setUploadedResumeUrl(talent.resumeUrl)}
                          className="flex-1 w-full flex flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-primary/40 bg-primary-container/10 p-6 transition-colors hover:bg-primary-container/20 cursor-pointer"
                        >
                          <span className="material-symbols-outlined text-2xl text-primary">upload_file</span>
                          <span className="font-label-sm text-label-sm text-primary font-medium">Use resume from profile</span>
                          <span className="font-label-sm text-label-sm text-on-surface-variant/60 text-center">File name: {talent.resumeUrl.split('/').pop()?.replace(/^\d{8}_\d{6}_/, '').replace(/\.pdf$/i, '') || 'Uploaded resume'}</span>
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Cover letter — shown in both CV and profile mode */}
              <div className="mt-5">
                <label className="font-label-md text-label-md font-medium text-on-surface">
                  Cover letter <span className="text-on-surface-variant">(optional)</span>
                </label>
                <div className="mt-2">
                  <RichTextEditor
                    value={coverLetter}
                    onChange={setCoverLetter}
                    placeholder="Tell the employer why you're a great fit for this role..."
                  />
                </div>
              </div>

              {/* Profile mode — show preview button + summary */}
              {applyMode === 'profile' && talent && (
                <div className="mt-5 rounded-lg border border-primary/30 bg-primary-container/10 p-4">
                  <div className="flex items-start gap-3">
                    <span className="material-symbols-outlined mt-0.5 text-lg text-primary">badge</span>
                    <div className="flex-1">
                      <p className="font-label-md text-label-md font-medium text-on-surface">
                        Your profile will be shared with the employer
                      </p>
                      <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                        {talent.skills.length > 0
                          ? talent.skills.slice(0, 3).join(', ') + (talent.skills.length > 3 ? ` +${talent.skills.length - 3} more` : '')
                          : 'No skills added yet'}
                      </p>
                      <div className="mt-3 flex flex-wrap gap-2">
                        <a
                          href="/dashboard/talent/profile/preview"
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1.5 rounded-lg border border-primary/40 bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-primary transition-colors hover:bg-primary-container/20"
                        >
                          <span className="material-symbols-outlined text-lg">visibility</span>
                          See how my profile looks
                        </a>
                        <button
                          type="button"
                          onClick={() => setShowProfilePreview(true)}
                          className="inline-flex items-center gap-1.5 rounded-lg border border-primary/40 bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-primary transition-colors hover:bg-primary-container/20"
                        >
                          <span className="material-symbols-outlined text-lg">tune</span>
                          Edit what is sent
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              )}
              {applyMode === 'profile' && !talent && (
                <div className="mt-5 rounded-lg border border-amber-200 bg-amber-50 p-4">
                  <p className="font-label-md text-label-md text-amber-900">
                    You need to complete your profile first.
                  </p>
                  <Link
                    to="/dashboard/talent/profile"
                    className="mt-2 inline-flex items-center gap-1 font-label-md text-label-md font-medium text-primary"
                  >
                    Set up profile
                    <span className="material-symbols-outlined text-sm">open_in_new</span>
                  </Link>
                </div>
              )}

              {applyError && (
                <p className="mt-3 font-label-sm text-label-sm text-error">{applyError}</p>
              )}
              <div className="mt-5 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => { if (!submitting) { setShowApplyForm(false); setApplyError(null) } }}
                  disabled={submitting}
                  className="rounded-lg border border-outline-variant bg-surface-container-lowest px-5 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleApply}
                  disabled={submitting || (applyMode === 'profile' && !talent)}
                  className="inline-flex items-center gap-2 rounded-lg bg-accent px-6 py-2.5 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90 disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent" />
                      Submitting...
                    </>
                  ) : (
                    <>
                      Submit application
                      <span className="material-symbols-outlined text-lg">send</span>
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>

          {/* Profile preview dialog */}
          {showProfilePreview && talent && (
            <>
              <div
                className="fixed inset-0 z-[60] bg-inverse-surface/50"
                onClick={() => setShowProfilePreview(false)}
              />
              <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
                <div
                  className="w-full max-w-4xl max-h-[85vh] overflow-y-auto rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl"
                  onClick={(e) => e.stopPropagation()}
                >
                  <div className="flex items-center justify-between">
                    <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                      Profile preview
                    </h2>
                    <button
                      type="button"
                      onClick={() => setShowProfilePreview(false)}
                      className="rounded-lg p-2 text-on-surface-variant transition-colors hover:bg-surface-container"
                    >
                      <span className="material-symbols-outlined">close</span>
                    </button>
                  </div>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    Toggle fields on/off to control what the employer sees.
                  </p>

                  <div className="mt-5 grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Left column */}
                    <div className="space-y-4">
                      <PreviewSection title="Personal details">
                        <PreviewRow label="Name" value={`${profile?.firstName} ${talent.middleName || ''} ${profile?.lastName}`.replace(/\s+/g, ' ').trim()} />
                        <PreviewToggle label="Profile photo" field="avatar" value={profile?.avatarUrl ? 'Uploaded' : null} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Phone" field="phone" value={talent.phoneNumber} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Date of birth" field="dateOfBirth" value={talent.dateOfBirth} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Address" field="address" value={talent.address} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewRow label="City" value={profile?.city} />
                        <PreviewRow label="Country" value={profile?.country} />
                      </PreviewSection>

                      <PreviewSection title="Professional details">
                        <PreviewToggle label="Experience" field="experience" value={talent.experienceLevel ? `${talent.experienceLevel}${talent.yearsOfExperience != null ? ` · ${talent.yearsOfExperience} years` : ''}` : null} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Skills" field="skills" value={talent.skills.length > 0 ? talent.skills.join(', ') : null} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Current industry" field="currentIndustry" value={talent.currentIndustry} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Current profession" field="currentProfession" value={talent.currentProfession} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Work mode" field="workMode" value={talent.workMode} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Availability" field="availability" value={talent.availability} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Desired roles" field="desiredRoles" value={talent.desiredRoles.length > 0 ? talent.desiredRoles.join(', ') : null} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                      </PreviewSection>
                    </div>

                    {/* Right column */}
                    <div className="space-y-4">
                      {/* About with rendered HTML */}
                      {talent.about && (
                        <PreviewSection title="About">
                          <div className="flex items-center justify-between px-4 py-2.5">
                            <div className="min-w-0 flex-1">
                              <div className="font-body-sm text-sm text-on-surface">
                                <RichTextDisplay html={talent.about} />
                              </div>
                            </div>
                            <button
                              type="button"
                              role="switch"
                              aria-checked={previewVisibility['about'] ?? true}
                              onClick={() => setPreviewVisibility(prev => ({ ...prev, about: !(prev['about'] ?? true) }))}
                              className={cn(
                                'relative ml-3 inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors',
                                (previewVisibility['about'] ?? true) ? 'bg-primary' : 'bg-surface-variant',
                              )}
                            >
                              <span
                                className={cn(
                                  'inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
                                  (previewVisibility['about'] ?? true) ? 'translate-x-4' : 'translate-x-0.5',
                                )}
                              />
                            </button>
                          </div>
                        </PreviewSection>
                      )}

                      <PreviewSection title="Education">
                        <PreviewToggle label="Education" field="education" value={talent.educationLevel || (talent.educationHistory !== '[]' ? 'Has entries' : null)} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                      </PreviewSection>

                      <PreviewSection title="Links">
                        <PreviewToggle label="Resume" field="resume" value={talent.resumeUrl ? 'Attached' : null} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="LinkedIn" field="linkedin" value={talent.linkedInUrl} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="GitHub" field="github" value={talent.githubUrl} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Portfolio" field="portfolio" value={talent.portfolioUrl} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                        <PreviewToggle label="Preferred locations" field="preferredLocations" value={talent.preferredLocations || null} visibility={previewVisibility} onToggle={setPreviewVisibility} />
                      </PreviewSection>
                    </div>
                  </div>

                  <div className="mt-6 flex justify-end gap-3">
                    <button
                      type="button"
                      onClick={() => setShowProfilePreview(false)}
                      className="rounded-lg border border-outline-variant bg-surface-container-lowest px-5 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                    >
                      Close
                    </button>
                    <button
                      type="button"
                      onClick={handleSaveVisibility}
                      disabled={savingVisibility}
                      className="inline-flex items-center gap-2 rounded-lg bg-accent px-5 py-2.5 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90 disabled:opacity-50"
                    >
                      {savingVisibility ? (
                        <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent" />
                      ) : (
                        <span className="material-symbols-outlined text-lg">save</span>
                      )}
                      Save visibility
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}
        </>
      )}
    </DashboardShell>
  )
}
