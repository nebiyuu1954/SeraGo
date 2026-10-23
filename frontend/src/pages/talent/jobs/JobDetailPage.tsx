import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { Helmet } from 'react-helmet-async'
import ReactGA from 'react-ga4'
import {
  applyToJob,
  fetchJob,
  getApiErrorMessage,
  getStoredAuthTokens,
  updateProfile,
} from '../../../api'
import {
  useJobDetailQuery,
  useJobsQuery,
  useMyApplicationsQuery,
  useProfileQuery,
  useSavedJobsQuery,
} from '../../../hooks/query.ts'
import { useRequireRoleAny } from '../../../hooks'
import type { JobResponse, JobType } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
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
import { saveJob, unsaveJob } from '../../../api'
import { trackEvent } from '../../../lib/analytics'

const DAY_MS = 86_400_000

export interface SimilarJobDisplayItem {
  id: string
  title: string
  company: string
  location: string
  salary: string
  sourceName: string
  workMode: string
  jobType: string
  postedAgo: string
  initials: string
  companyLogoUrl?: string | null
}

const PLACEHOLDER_SIMILAR_JOBS: SimilarJobDisplayItem[] = [
  {
    id: 'placeholder-1',
    title: 'Lead Backend Engineer (.NET & Azure)',
    company: 'Commercial Bank of Ethiopia',
    location: 'Addis Ababa',
    salary: 'ETB 60k – 80k',
    sourceName: 'EthioJobs',
    workMode: 'hybrid',
    jobType: 'Full-time',
    postedAgo: '1d ago',
    initials: 'CBE',
  },
  {
    id: 'placeholder-2',
    title: 'Senior Frontend Developer (React & TypeScript)',
    company: 'Chapa Financial Technologies',
    location: 'Bole, Addis Ababa',
    salary: 'ETB 50k – 70k',
    sourceName: 'Afriwork',
    workMode: 'remote',
    jobType: 'Full-time',
    postedAgo: '2d ago',
    initials: 'CP',
  },
  {
    id: 'placeholder-3',
    title: 'Mobile App Developer (Flutter & Dart)',
    company: 'RIDE Mobility Tech',
    location: 'Kazanchis, Addis Ababa',
    salary: 'ETB 45k – 60k',
    sourceName: 'HaHuJobs',
    workMode: 'onsite',
    jobType: 'Full-time',
    postedAgo: '3d ago',
    initials: 'RD',
  },
  {
    id: 'placeholder-4',
    title: 'Cloud Solutions Architect',
    company: 'SunPay Systems Ethiopia',
    location: 'Addis Ababa',
    salary: 'ETB 75k – 110k',
    sourceName: 'GeezJobs',
    workMode: 'hybrid',
    jobType: 'Full-time',
    postedAgo: '4d ago',
    initials: 'SP',
  },
  {
    id: 'placeholder-5',
    title: 'DevOps & Site Reliability Engineer',
    company: 'Eaglelion System Technology',
    location: 'Addis Ababa',
    salary: 'ETB 55k – 75k',
    sourceName: 'SeraGo',
    workMode: 'remote',
    jobType: 'Full-time',
    postedAgo: 'Just now',
    initials: 'EL',
  },
  {
    id: 'placeholder-6',
    title: 'Full-Stack Software Engineer',
    company: 'Iceaddis Tech Incubator',
    location: 'Addis Ababa',
    salary: 'ETB 40k – 55k',
    sourceName: 'Ethiopian Reporter Jobs',
    workMode: 'onsite',
    jobType: 'Full-time',
    postedAgo: '5d ago',
    initials: 'IC',
  },
]

function formatSalaryDisplay(j: JobResponse): { main: string; sub: string } {
  if (j.salaryMin != null || j.salaryMax != null) {
    const currency = j.salaryCurrency || 'ETB'
    const period = j.salaryPeriod ? ` / ${j.salaryPeriod}` : ' / month'
    if (j.salaryMin != null && j.salaryMax != null) {
      return {
        main: `${currency} ${j.salaryMin.toLocaleString()} – ${j.salaryMax.toLocaleString()}`,
        sub: `Base pay${period}`,
      }
    }
    if (j.salaryMin != null) {
      return {
        main: `From ${currency} ${j.salaryMin.toLocaleString()}`,
        sub: `Base pay${period}`,
      }
    }
    return {
      main: `Up to ${currency} ${j.salaryMax!.toLocaleString()}`,
      sub: `Base pay${period}`,
    }
  }
  if (j.salary?.trim()) {
    return {
      main: j.salary,
      sub: 'Specified by employer',
    }
  }
  return {
    main: 'Competitive',
    sub: 'Undisclosed / Negotiable',
  }
}

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
  const navigate = useNavigate()
  const isPreview = location.pathname.startsWith('/dashboard/admin/')
    || (location.state as { fromAdmin?: boolean })?.fromAdmin === true
  const auth = useRequireRoleAny(['Talent', 'Admin'])

  const searchParams = new URLSearchParams(location.search)
  const isFromTelegram = searchParams.get('from') === 'telegram'
  const urlTitle = searchParams.get('title')
  const urlSectorId = searchParams.get('sectorId')
  const urlSectorName = searchParams.get('sector')

  useEffect(() => {
    if (!jobId || isPreview) return

    if (auth.status === 'unauthenticated') {
      try {
        sessionStorage.setItem('serago_pending_target_job_id', jobId)
        localStorage.setItem('serago_pending_target_job_id', jobId)
        if (urlTitle) {
          sessionStorage.setItem('serago_pending_target_job_title', urlTitle)
          localStorage.setItem('serago_pending_target_job_title', urlTitle)
        }
        if (urlSectorId) {
          sessionStorage.setItem('serago_pending_target_job_sector_id', urlSectorId)
          localStorage.setItem('serago_pending_target_job_sector_id', urlSectorId)
        }
        if (urlSectorName) {
          sessionStorage.setItem('serago_pending_target_job_sector_name', urlSectorName)
          localStorage.setItem('serago_pending_target_job_sector_name', urlSectorName)
        }
      } catch {}

      if (!urlTitle || (!urlSectorId && !urlSectorName)) {
        fetchJob(jobId)
          .then((j) => {
            if (j?.title) {
              sessionStorage.setItem('serago_pending_target_job_title', j.title)
              localStorage.setItem('serago_pending_target_job_title', j.title)
            }
            if (j?.sectorId) {
              sessionStorage.setItem('serago_pending_target_job_sector_id', j.sectorId)
              localStorage.setItem('serago_pending_target_job_sector_id', j.sectorId)
            }
            if (j?.sectorName) {
              sessionStorage.setItem('serago_pending_target_job_sector_name', j.sectorName)
              localStorage.setItem('serago_pending_target_job_sector_name', j.sectorName)
            }
          })
          .catch(() => {})
      }
      navigate('/login', { replace: true })
      return
    }

    if (auth.status === 'authenticated' && isFromTelegram) {
      try {
        sessionStorage.setItem('serago_pending_target_job_id', jobId)
        localStorage.setItem('serago_pending_target_job_id', jobId)
        if (urlTitle) {
          sessionStorage.setItem('serago_pending_target_job_title', urlTitle)
          localStorage.setItem('serago_pending_target_job_title', urlTitle)
        }
        if (urlSectorId) {
          sessionStorage.setItem('serago_pending_target_job_sector_id', urlSectorId)
          localStorage.setItem('serago_pending_target_job_sector_id', urlSectorId)
        }
        if (urlSectorName) {
          sessionStorage.setItem('serago_pending_target_job_sector_name', urlSectorName)
          localStorage.setItem('serago_pending_target_job_sector_name', urlSectorName)
        }
      } catch {}

      if (!urlTitle || (!urlSectorId && !urlSectorName)) {
        fetchJob(jobId)
          .then((j) => {
            if (j?.title) {
              sessionStorage.setItem('serago_pending_target_job_title', j.title)
              localStorage.setItem('serago_pending_target_job_title', j.title)
            }
            if (j?.sectorId) {
              sessionStorage.setItem('serago_pending_target_job_sector_id', j.sectorId)
              localStorage.setItem('serago_pending_target_job_sector_id', j.sectorId)
            }
            if (j?.sectorName) {
              sessionStorage.setItem('serago_pending_target_job_sector_name', j.sectorName)
              localStorage.setItem('serago_pending_target_job_sector_name', j.sectorName)
            }
            navigate('/dashboard/talent', { replace: true })
          })
          .catch(() => {
            navigate('/dashboard/talent', { replace: true })
          })
      } else {
        navigate('/dashboard/talent', { replace: true })
      }
    }
  }, [jobId, auth.status, isFromTelegram, urlTitle, urlSectorId, urlSectorName, isPreview, navigate])

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

  // Similar jobs query: fetches live jobs by sector if available
  const { jobs: rawSimilarJobs } = useJobsQuery(
    job?.sectorId ? { sectorId: job.sectorId, pageSize: 8 } : { pageSize: 8 },
    auth.status === 'authenticated' && !!job,
  )

  const liveSimilarJobs: SimilarJobDisplayItem[] = (rawSimilarJobs || [])
    .filter((j) => j.id !== job?.id)
    .slice(0, 6)
    .map((j) => {
      const sal =
        j.salaryMin != null || j.salaryMax != null
          ? `${j.salaryCurrency || 'ETB'} ${j.salaryMin ? j.salaryMin.toLocaleString() : ''}${j.salaryMin && j.salaryMax ? ' – ' : ''}${j.salaryMax ? j.salaryMax.toLocaleString() : ''}`
          : j.salary || 'Competitive'
      return {
        id: j.id,
        title: j.title,
        company: j.company || 'Undisclosed',
        location: j.location || 'Ethiopia',
        salary: sal,
        sourceName: j.sourceName || 'SeraGo',
        workMode: j.workMode || 'onsite',
        jobType: jobTypeLabel(j),
        postedAgo: postedLabel(j.publishedAt, j.refreshedAt) || 'Recently',
        initials: initialsOf(j.company || 'SG'),
        companyLogoUrl: j.companyLogoUrl,
      }
    })

  // Fallback to rich curated placeholders representing all sources while dedicated API is built
  const displayedSimilarJobs =
    liveSimilarJobs.length > 0 ? liveSimilarJobs : PLACEHOLDER_SIMILAR_JOBS

  const copyRoleShareLink = () => {
    if (typeof window !== 'undefined') {
      navigator.clipboard?.writeText(window.location.href)
      showToast('Sharable job link copied to clipboard!')
    }
  }

  const handleSelectSimilarJob = (id: string) => {
    if (id.startsWith('placeholder-')) {
      showToast('Similar jobs API is currently in development. Displaying preview vacancy.', 'info')
      return
    }
    navigate(`/dashboard/talent/jobs/${id}`)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  // Engagement Tracking & Similar Jobs Scroll Observer
  const similarJobsRef = useRef<HTMLDivElement | null>(null)
  const hasTrackedScrollRef = useRef(false)

  const handleApplyClick = (source: string) => {
    if (!job?.id) return
    ReactGA.event({
      category: 'Engagement',
      action: 'Click_Apply',
      label: `${job.id} (${source})`,
    })
  }

  useEffect(() => {
    hasTrackedScrollRef.current = false
    if (!job?.id) return

    const target = similarJobsRef.current
    if (!target) return

    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries
        if (entry.isIntersecting && !hasTrackedScrollRef.current) {
          hasTrackedScrollRef.current = true
          ReactGA.event({
            category: 'Engagement',
            action: 'Scroll_To_Bottom',
            label: job.id,
          })
        }
      },
      { threshold: 0.1 },
    )

    observer.observe(target)
    return () => observer.disconnect()
  }, [job?.id])

  // Dynamic SEO metadata
  const pageTitle = job
    ? `${job.title} at ${job.company || 'SeraGo'} - SeraGo`
    : 'Job Details · SeraGo'

  const metaDescription = job?.description
    ? job.description
        .replace(/<[^>]*>?/gm, '')
        .replace(/\s+/g, ' ')
        .trim()
        .slice(0, 160) + '...'
    : 'Find and apply for verified jobs in Ethiopia on SeraGo.'

  const canonicalUrl =
    typeof window !== 'undefined'
      ? `${window.location.origin}/dashboard/talent/jobs/${job?.id ?? ''}`
      : `https://serago.pro.et/dashboard/talent/jobs/${job?.id ?? ''}`
  const ogImage =
    job?.companyLogoUrl ||
    (typeof window !== 'undefined'
      ? `${window.location.origin}/favicon.svg`
      : 'https://serago.pro.et/favicon.svg')

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
    <>
      <Helmet>
        <title>{pageTitle}</title>
        <meta name="description" content={metaDescription} />
        <link rel="canonical" href={canonicalUrl} />

        {/* OpenGraph */}
        <meta property="og:type" content="article" />
        <meta property="og:site_name" content="SeraGo" />
        <meta property="og:title" content={pageTitle} />
        <meta property="og:description" content={metaDescription} />
        <meta property="og:url" content={canonicalUrl} />
        <meta property="og:image" content={ogImage} />

        {/* Twitter Card */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content={pageTitle} />
        <meta name="twitter:description" content={metaDescription} />
        <meta name="twitter:image" content={ogImage} />
      </Helmet>

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

      {!isLoading && !error && job && (() => {
        const salaryInfo = formatSalaryDisplay(job)
        return (
          <>
            <div className="w-full mt-4">
              <div className="w-full flex flex-col lg:flex-row gap-6 lg:gap-8 items-start relative pb-20 lg:pb-10">
                {/* ================= LEFT PANE (Primary Details: fills space) ================= */}
                <div className="flex-1 min-w-0 flex flex-col gap-6">
                  {/* 1. Elevated Hero Header Card */}
                  <div className="relative overflow-hidden rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
                    {/* Subtle Gradient Top Accent Edge */}
                    <div className="absolute top-0 left-0 right-0 h-1.5 bg-gradient-to-r from-primary via-primary-container to-accent" />

                    <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
                      {/* Left Column: Avatar + Company Name + Title + Meta Chips */}
                      <div className="flex-1 min-w-0 space-y-4">
                        <div className="flex items-center gap-3.5">
                          {/* Company Avatar / Logo */}
                          {job.companyLogoUrl ? (
                            <div className="relative flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-surface-variant bg-surface-container-low p-2 shadow-xs">
                              <img
                                src={job.companyLogoUrl}
                                alt={job.company ?? ''}
                                className="max-h-full max-w-full object-contain"
                              />
                            </div>
                          ) : (
                            <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-primary-container/60 font-headline-md text-headline-md font-bold text-primary shadow-xs">
                              {initialsOf(job.company || 'SG')}
                            </div>
                          )}

                          <div className="min-w-0">
                            <h2 className="font-headline-md text-headline-md font-semibold text-on-surface tracking-tight">
                              {job.company || 'Company Undisclosed'}
                            </h2>
                            {job.location && (
                              <div className="mt-1 flex items-center gap-1.5 font-body-md text-body-md text-on-surface-variant">
                                <span className="material-symbols-outlined text-[18px] text-on-surface-variant">
                                  location_on
                                </span>
                                <span className="truncate">{job.location}</span>
                              </div>
                            )}
                          </div>
                        </div>

                        {/* Job Title */}
                        <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-on-surface leading-snug">
                          {job.title}
                        </h1>

                        {/* Meta Chips */}
                        <div className="flex flex-wrap items-center gap-2 pt-1">
                          <span className="inline-flex items-center gap-1.5 rounded-lg bg-surface-container px-3 py-1 font-label-sm text-label-sm text-on-surface">
                            <span className="material-symbols-outlined text-[16px] text-on-surface-variant">
                              schedule
                            </span>
                            {jobTypeLabel(job)}
                          </span>

                          <span className="inline-flex items-center gap-1.5 rounded-lg bg-surface-container px-3 py-1 font-label-sm text-label-sm text-on-surface">
                            <span className="material-symbols-outlined text-[16px] text-on-surface-variant">
                              home_work
                            </span>
                            {workModeLabel(job.workMode)}
                          </span>

                          {closesLabel(job.deadline) && (
                            <span
                              className={cn(
                                'inline-flex items-center gap-1.5 rounded-lg px-3 py-1 font-label-sm text-label-sm font-semibold',
                                new Date(job.deadline!).getTime() - Date.now() <= 7 * DAY_MS
                                  ? 'bg-error-container/80 text-on-error-container'
                                  : 'bg-primary-container/60 text-primary',
                              )}
                            >
                              <span className="material-symbols-outlined text-[16px]">alarm</span>
                              {closesLabel(job.deadline)}
                            </span>
                          )}

                          {job.sourceName && job.sourceName.toLowerCase() !== 'serago' && (
                            <span className="inline-flex items-center gap-1.5 rounded-lg bg-surface-container-low px-3 py-1 font-label-sm text-label-sm text-on-surface-variant">
                              <span className="material-symbols-outlined text-[16px] text-primary">language</span>
                              Aggregated vacancy
                            </span>
                          )}
                        </div>
                      </div>

                      {/* Right Column: Action Buttons (Apply CTA, Save Job, Share) */}
                      <div className="flex flex-wrap sm:flex-nowrap lg:flex-wrap items-center lg:items-start lg:justify-end gap-3 shrink-0 pt-2 lg:pt-1">
                        {/* Apply CTA Button */}
                        {isPreview ? (
                          <span className="inline-flex items-center justify-center gap-1.5 rounded-lg border border-outline-variant px-6 py-3 font-label-md text-label-md text-on-surface-variant">
                            <span className="material-symbols-outlined text-lg">visibility</span>
                            Read-only preview
                          </span>
                        ) : isSerago ? (
                          hasApplied || applySuccess ? (
                            <span className="inline-flex items-center justify-center gap-2 rounded-lg border border-primary/30 bg-primary-container/30 px-7 py-3.5 font-label-md text-label-md font-semibold text-primary">
                              <span className="material-symbols-outlined text-[20px]">check_circle</span>
                              Application Submitted
                            </span>
                          ) : (
                            <button
                              type="button"
                              onClick={() => {
                                handleApplyClick('header_serago_modal')
                                setShowApplyForm(true)
                              }}
                              className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-8 py-3.5 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-all hover:opacity-90 active:scale-95 cursor-pointer"
                            >
                              <span>Apply on SeraGo</span>
                              <span className="material-symbols-outlined text-[18px]">north_east</span>
                            </button>
                          )
                        ) : job.url ? (
                          <a
                            href={job.url}
                            target="_blank"
                            rel="noreferrer"
                            onClick={() => handleApplyClick('header_external_link')}
                            className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-8 py-3.5 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-all hover:opacity-90 active:scale-95 cursor-pointer"
                          >
                            <span>Apply on {job.sourceName || 'Source'}</span>
                            <span className="material-symbols-outlined text-[18px]">open_in_new</span>
                          </a>
                        ) : (
                          <span className="inline-flex cursor-not-allowed items-center justify-center gap-1.5 rounded-lg border border-outline-variant px-6 py-3 font-label-md text-label-md text-on-surface-variant/60">
                            Apply
                          </span>
                        )}

                        {/* Save Button */}
                        {!isPreview && (
                          <button
                            type="button"
                            onClick={handleToggleSave}
                            className="inline-flex items-center justify-center gap-2 rounded-lg border border-surface-variant bg-surface-container-low px-5 py-3.5 font-label-md text-label-md font-semibold text-on-surface transition-colors hover:bg-surface-container cursor-pointer"
                          >
                            <span
                              className={cn(
                                'material-symbols-outlined text-[20px]',
                                savedIds.has(job.id) ? 'text-accent' : 'text-on-surface-variant',
                              )}
                              style={{ fontVariationSettings: savedIds.has(job.id) ? "'FILL' 1" : "'FILL' 0" }}
                            >
                              {savedIds.has(job.id) ? 'bookmark' : 'bookmark_border'}
                            </span>
                            <span>{savedIds.has(job.id) ? 'Saved' : 'Save Job'}</span>
                          </button>
                        )}

                        {/* Share Button */}
                        <button
                          type="button"
                          onClick={copyRoleShareLink}
                          className="inline-flex items-center justify-center gap-2 rounded-lg border border-surface-variant bg-surface-container-low px-5 py-3.5 font-label-md text-label-md font-semibold text-on-surface transition-colors hover:bg-surface-container cursor-pointer"
                        >
                          <span className="material-symbols-outlined text-[20px] text-on-surface-variant">share</span>
                          <span>Share</span>
                        </button>
                      </div>
                    </div>
                  </div>

                  {/* 2. Quick Facts Ribbon (Horizontal 5-Column Grid) */}
                  <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm">
                    <div className="grid grid-cols-2 md:grid-cols-5 gap-4 lg:gap-6 divide-y md:divide-y-0 md:divide-x divide-surface-variant/50">
                      {/* Col 1: Compensation */}
                      <div className="flex flex-col gap-1 p-2">
                        <div className="flex items-center gap-1.5 font-label-sm text-label-sm text-on-surface-variant">
                          <span className="material-symbols-outlined text-[18px] text-primary">payments</span>
                          <span>Compensation</span>
                        </div>
                        <div className="font-headline-md text-body-lg font-bold text-on-surface tracking-tight">
                          {salaryInfo.main}
                        </div>
                        <div className="font-label-sm text-label-sm text-on-surface-variant/70">
                          {salaryInfo.sub}
                        </div>
                      </div>

                      {/* Col 2: Experience */}
                      <div className="flex flex-col gap-1 p-2 md:pl-6">
                        <div className="flex items-center gap-1.5 font-label-sm text-label-sm text-on-surface-variant">
                          <span className="material-symbols-outlined text-[18px] text-primary">psychology</span>
                          <span>Experience</span>
                        </div>
                        <div className="font-headline-md text-body-lg font-bold text-on-surface">
                          {job.experienceLevel || job.experienceText || 'Not specified'}
                        </div>
                        <div className="font-label-sm text-label-sm text-on-surface-variant/70">
                          {job.maxExperienceYears ? `Up to ${job.maxExperienceYears} years` : 'Required level'}
                        </div>
                      </div>

                      {/* Col 3: Sector */}
                      <div className="flex flex-col gap-1 p-2 md:pl-6">
                        <div className="flex items-center gap-1.5 font-label-sm text-label-sm text-on-surface-variant">
                          <span className="material-symbols-outlined text-[18px] text-primary">hub</span>
                          <span>Sector</span>
                        </div>
                        <div className="font-headline-md text-body-lg font-bold text-on-surface truncate" title={job.sectorName || 'General'}>
                          {job.sectorName || 'General'}
                        </div>
                        <div className="font-label-sm text-label-sm text-on-surface-variant/70 truncate">
                          {job.subSectorName || 'Industry Category'}
                        </div>
                      </div>

                      {/* Col 4: Work Mode */}
                      <div className="flex flex-col gap-1 p-2 md:pl-6">
                        <div className="flex items-center gap-1.5 font-label-sm text-label-sm text-on-surface-variant">
                          <span className="material-symbols-outlined text-[18px] text-primary">apartment</span>
                          <span>Work Mode</span>
                        </div>
                        <div className="font-headline-md text-body-lg font-bold text-on-surface">
                          {workModeLabel(job.workMode)}
                        </div>
                        <div className="font-label-sm text-label-sm text-on-surface-variant/70 truncate">
                          {job.location || 'Ethiopia'}
                        </div>
                      </div>

                      {/* Col 5: Deadline */}
                      <div className="flex flex-col gap-1 p-2 md:pl-6">
                        <div className="flex items-center gap-1.5 font-label-sm text-label-sm text-accent">
                          <span className="material-symbols-outlined text-[18px] text-accent">event_upcoming</span>
                          <span>Application Due</span>
                        </div>
                        <div className="font-headline-md text-body-lg font-bold text-on-surface">
                          {job.deadline ? formatDate(job.deadline) : (job.deadlineText || 'Open')}
                        </div>
                        <div className="font-label-sm text-label-sm font-medium text-accent">
                          {closesLabel(job.deadline) || 'Actively hiring'}
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* 3. Universal Description Content Card */}
                  <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8">
                    <div className="flex items-center gap-2 pb-5 border-b border-surface-variant/50">
                      <div className="h-6 w-1.5 rounded-full bg-primary" />
                      <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                        Job description
                      </h2>
                    </div>
                    <div className="mt-6">
                      <SourceJobDescription job={job} />
                    </div>
                  </div>

                  {/* 4. How to Apply Instructions (when present) */}
                  {applySections.length > 0 && (
                    <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-sm sm:p-8 space-y-5">
                      <div className="flex items-center gap-2">
                        <div className="h-6 w-1.5 rounded-full bg-primary" />
                        <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                          How to apply
                        </h2>
                      </div>
                      <div className="space-y-4">
                        {applySections.map((section, i) => (
                          <SectionContent key={i} text={section.content} />
                        ))}
                      </div>
                      {job.url && (
                        <div className="pt-2">
                          <a
                            href={job.url}
                            target="_blank"
                            rel="noreferrer"
                            onClick={() => handleApplyClick('bottom_instructions_cta')}
                            className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-8 py-3.5 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-opacity hover:opacity-90 cursor-pointer"
                          >
                            <span>Apply now</span>
                            <span className="material-symbols-outlined text-[18px]">open_in_new</span>
                          </a>
                        </div>
                      )}
                    </div>
                  )}
                </div>

                {/* ================= RIGHT RAIL (Similar Jobs: wider & bigger) ================= */}
                <aside ref={similarJobsRef} className="w-full shrink-0 flex flex-col gap-4 lg:w-[380px] xl:w-[420px] 2xl:w-[450px] lg:sticky lg:top-20">
                  <div className="rounded-2xl border border-surface-variant bg-surface-container-lowest p-5 sm:p-6 shadow-sm">
                    <div className="flex items-center justify-between pb-4">
                      <div className="flex items-center gap-2">
                        <h2 className="font-headline-md text-headline-md font-bold text-on-surface">Similar Jobs</h2>
                        <span className="rounded-full bg-primary-container/60 px-2.5 py-0.5 font-label-sm text-label-sm font-semibold text-primary">
                          {displayedSimilarJobs.length} found
                        </span>
                      </div>
                      <Link
                        to={job.sectorId ? `/dashboard/talent?sectorId=${job.sectorId}` : '/dashboard/talent'}
                        className="font-label-sm text-label-sm font-semibold text-primary hover:underline flex items-center gap-0.5"
                      >
                        <span>Browse all</span>
                        <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
                      </Link>
                    </div>

                    {/* Vertical List of Similar Job Cards (1 per row, wider and bigger) */}
                    <div className="flex flex-col gap-3.5">
                      {displayedSimilarJobs.map((item) => {
                        const itemSourceLogo = sourceLogo(item.sourceName)
                        const isCurrent = item.id === job.id

                        return (
                          <div
                            key={item.id}
                            onClick={() => handleSelectSimilarJob(item.id)}
                            className={cn(
                              'group relative rounded-xl border p-4 sm:p-4.5 transition-all cursor-pointer flex flex-col gap-3',
                              isCurrent
                                ? 'border-primary bg-primary-container/10 shadow-xs'
                                : 'border-surface-variant bg-surface-container-lowest hover:border-primary/50 hover:bg-surface-container-low/40 hover:shadow-xs',
                            )}
                          >
                            {/* Row 1: Logo/Avatar + Title & Company */}
                            <div className="flex items-start gap-3 min-w-0">
                              {item.companyLogoUrl ? (
                                <div className="h-11 w-11 shrink-0 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest p-1 shadow-2xs flex items-center justify-center">
                                  <img src={item.companyLogoUrl} alt="" className="max-h-full max-w-full object-contain" />
                                </div>
                              ) : (
                                <div className="h-11 w-11 shrink-0 rounded-xl bg-primary-container/50 text-primary font-bold font-headline-md text-sm flex items-center justify-center shadow-2xs">
                                  {item.initials || initialsOf(item.company || 'SG')}
                                </div>
                              )}

                              <div className="min-w-0 flex-1">
                                <h3 className="font-headline-md text-[15px] sm:text-[16px] font-bold text-on-surface group-hover:text-primary transition-colors line-clamp-2 leading-snug">
                                  {item.title}
                                </h3>
                                <p className="mt-0.5 font-body-md text-sm font-medium text-on-surface-variant truncate">
                                  {item.company}
                                </p>
                              </div>
                            </div>

                            {/* Row 2: Location & Job Type Chips */}
                            <div className="flex flex-wrap items-center gap-2 pt-0.5">
                              {/* Location */}
                              <span className="inline-flex items-center gap-1 text-xs text-on-surface-variant font-medium">
                                <span className="material-symbols-outlined text-[15px] text-on-surface-variant/80">location_on</span>
                                <span className="truncate max-w-[150px]">{item.location}</span>
                              </span>

                              <span className="text-surface-variant">·</span>

                              {/* Job Type */}
                              <span className="inline-flex items-center gap-1 rounded-md bg-surface-container px-2 py-0.5 text-[11px] font-label-sm font-medium text-on-surface">
                                <span className="material-symbols-outlined text-[13px] text-on-surface-variant">schedule</span>
                                {item.jobType || 'Full-time'}
                              </span>

                              {/* Work Mode */}
                              {item.workMode && (
                                <span className="rounded-md bg-primary-container/40 px-2 py-0.5 text-[11px] font-label-sm font-medium text-primary">
                                  {workModeLabel(item.workMode)}
                                </span>
                              )}
                            </div>

                            {/* Row 3: Salary & Source branding */}
                            <div className="flex items-center justify-between pt-2.5 border-t border-surface-variant/50">
                              <span className="font-headline-md text-sm font-bold text-on-surface tracking-tight">
                                {item.salary || 'Competitive'}
                              </span>

                              <div className="flex items-center">
                                {itemSourceLogo ? (
                                  <img
                                    src={itemSourceLogo}
                                    alt={item.sourceName || ''}
                                    title={item.sourceName || ''}
                                    className="h-5 sm:h-5.5 w-auto max-w-[65px] object-contain drop-shadow-2xs"
                                  />
                                ) : (
                                  <span className="rounded-full bg-surface-container-low border border-surface-variant px-2.5 py-0.5 text-[11px] font-label-sm font-medium text-on-surface-variant">
                                    {item.sourceName || 'SeraGo'}
                                  </span>
                                )}
                              </div>
                            </div>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                </aside>
              </div>
            </div>

            {/* ================= STICKY MOBILE BOTTOM ACTION BAR ================= */}
            <div className="fixed bottom-0 left-0 right-0 z-40 lg:hidden border-t border-surface-variant bg-surface-container-lowest/95 backdrop-blur-md px-4 py-3 shadow-[0_-4px_16px_rgba(0,0,0,0.06)]">
              <div className="mx-auto flex max-w-lg items-center justify-between gap-3">
                <div className="min-w-0 flex-1">
                  <div className="truncate font-label-sm text-label-sm text-on-surface-variant">{job.title}</div>
                  <div className="font-label-md text-label-md font-bold text-on-surface">
                    {salaryInfo.main !== 'Competitive' ? salaryInfo.main : (job.company || 'SeraGo')}
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  {!isPreview && (
                    <button
                      type="button"
                      onClick={handleToggleSave}
                      className="rounded-lg border border-surface-variant bg-surface-container-low p-2.5 text-on-surface transition-colors hover:bg-surface-container"
                      aria-label="Save Job"
                    >
                      <span
                        className={cn(
                          'material-symbols-outlined text-[20px]',
                          savedIds.has(job.id) ? 'text-accent' : 'text-on-surface-variant',
                        )}
                        style={{ fontVariationSettings: savedIds.has(job.id) ? "'FILL' 1" : "'FILL' 0" }}
                      >
                        {savedIds.has(job.id) ? 'bookmark' : 'bookmark_border'}
                      </span>
                    </button>
                  )}

                  {isPreview ? (
                    <span className="rounded-lg border border-outline-variant px-4 py-2 font-label-sm text-label-sm text-on-surface-variant">
                      Preview
                    </span>
                  ) : isSerago ? (
                    hasApplied || applySuccess ? (
                      <span className="rounded-lg bg-primary-container/40 px-5 py-2.5 font-label-md text-label-md font-semibold text-primary">
                        Applied
                      </span>
                    ) : (
                      <button
                        type="button"
                        onClick={() => {
                          handleApplyClick('mobile_bar_serago_modal')
                          setShowApplyForm(true)
                        }}
                        className="flex items-center gap-1.5 rounded-lg bg-accent px-6 py-2.5 font-label-md text-label-md font-bold text-on-accent shadow-sm active:scale-95 transition-all"
                      >
                        <span>Apply Now</span>
                        <span className="material-symbols-outlined text-[18px]">north_east</span>
                      </button>
                    )
                  ) : job.url ? (
                    <a
                      href={job.url}
                      target="_blank"
                      rel="noreferrer"
                      onClick={() => handleApplyClick('mobile_bar_external_link')}
                      className="flex items-center gap-1.5 rounded-lg bg-accent px-6 py-2.5 font-label-md text-label-md font-bold text-on-accent shadow-sm active:scale-95 transition-all"
                    >
                      <span>Apply Now</span>
                      <span className="material-symbols-outlined text-[18px]">open_in_new</span>
                    </a>
                  ) : null}
                </div>
              </div>
            </div>
          </>
        )
      })()}

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
    </>
  )
}
