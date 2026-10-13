import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, getStoredAuthTokens } from '../../../api'
import {
  useJobsQuery,
  useSectorsQuery,
  useJobLocationsQuery,
  useJobTypesQuery,
  useSavedJobsQuery,
  useMyApplicationsQuery,
  useProfileQuery,
} from '../../../hooks/query.ts'
import { useSettingsQuery } from '../../../hooks'
import { prefetchJob } from '../../../hooks/useJobDetailQuery.ts'
import { useRequireRole } from '../../../hooks'
import type { JobResponse, JobType } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import SaveJobButton from '../../../components/dashboard/SaveJobButton.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import TalentOnboardingModal from '../../../components/dashboard/TalentOnboardingModal.tsx'
import {
  CLOSING_WITHIN_OPTIONS,
  EXPERIENCE_OPTIONS,
  JOB_TYPE_LABELS,
  JOB_TYPE_OPTIONS,
  PAGE_SIZE_OPTIONS,
  POSTED_WITHIN_OPTIONS,
  SORT_OPTIONS,
  SOURCE_OPTIONS,
  WORK_ARRANGEMENT_OPTIONS,
} from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'
import { formatDate, postedLabel } from '../../../lib/date.ts'
import { initialsOf } from '../../../lib/initials.ts'

import { sourceLogo } from '../../../lib/sourceLogos.ts'
import { parseSkills } from '../../../lib/sourceCapabilities.ts'
import { saveJob, unsaveJob, runForYouMatching } from '../../../api'
import { trackEvent } from '../../../lib/analytics'

function jobTypeLabel(job: JobResponse): string {
  return JOB_TYPE_LABELS[job.jobType as JobType] ?? 'Other'
}

/** The talent's detail page for a job. */
function jobDetailPath(job: JobResponse): string {
  return `/dashboard/talent/jobs/${job.id}`
}

/**
 * Talent's post-login landing: browse every published job on the platform
 * with search, filters, sorting, and pagination — wired to SWR-cached queries.
 */
interface ViewFilterState {
  searchInput: string
  appliedQ: string
  jobType: JobType | ''
  location: string
  sectorId: string
  sources: string[]
  experience: string
  workArrangement: string
  postedWithin: string
  closingWithin: string
  salaryMin: string
  salaryMax: string
  sort: string
  page: number
  draftJobType: JobType | ''
  draftLocation: string
  draftSector: string
  draftSources: string[]
  draftExperience: string
  draftWorkArrangement: string
  draftPostedWithin: string
  draftClosingWithin: string
  draftSalaryMin: string
  draftSalaryMax: string
}

const DEFAULT_FILTER_STATE: ViewFilterState = {
  searchInput: '',
  appliedQ: '',
  jobType: '',
  location: '',
  sectorId: '',
  sources: [],
  experience: '',
  workArrangement: '',
  postedWithin: '',
  closingWithin: '',
  salaryMin: '',
  salaryMax: '',
  sort: 'newest',
  page: 1,
  draftJobType: '',
  draftLocation: '',
  draftSector: '',
  draftSources: [],
  draftExperience: '',
  draftWorkArrangement: '',
  draftPostedWithin: '',
  draftClosingWithin: '',
  draftSalaryMin: '',
  draftSalaryMax: '',
}

export default function JobsPage() {
  const auth = useRequireRole('Talent')

  // View toggle, persisted in the URL: "For you" (preference-filtered feed,
  // the default) vs. "All jobs" (the classic browse).
  const [searchParams, setSearchParams] = useSearchParams()
  const view = searchParams.get('view') === 'all' ? 'all' : 'forYou'

  // Independent filter state for "For you" and "All jobs" views:
  const [forYouFilters, setForYouFilters] = useState<ViewFilterState>(() => ({
    ...DEFAULT_FILTER_STATE,
    sort: 'match',
  }))

  const [allJobsFilters, setAllJobsFilters] = useState<ViewFilterState>(() => ({
    ...DEFAULT_FILTER_STATE,
    sort: 'newest',
  }))

  const currentFilters = view === 'forYou' ? forYouFilters : allJobsFilters
  const setCurrentFilters = (updater: (prev: ViewFilterState) => ViewFilterState) => {
    if (view === 'forYou') {
      setForYouFilters(updater)
    } else {
      setAllJobsFilters(updater)
    }
  }

  const [pageSize, setPageSize] = useState(10)
  // The right-hand filter panel is a collapsible sidebar, expanded by default.
  const [filtersOpen, setFiltersOpen] = useState(true)

  // --- SWR-cached data (shared across all mounted instances) ---

  // The "For you" sectors live in settings (they power the feed + job alerts).
  const { settings, refresh: refreshSettings } = useSettingsQuery()
  const rawPrefSectorIds = settings.forYou?.sectorIds ?? []
  const prefSectorIds = rawPrefSectorIds.map((id) => id.toLowerCase())
  const { profile } = useProfileQuery(auth.status === 'authenticated')

  const [onboardingOpen, setOnboardingOpen] = useState(false)

  const currentUserId = auth.status === 'authenticated' ? auth.user.id : ''

  // Trigger onboarding modal for brand-new talent without resume or sectors configured
  useEffect(() => {
    if (!currentUserId || !profile) return
    const dismissedKey = `serago_onboarding_dismissed_${currentUserId}`
    const isDismissed = localStorage.getItem(dismissedKey) === 'true'

    if (
      !isDismissed &&
      !settings.forYou?.onboardingCompleted &&
      !profile.talent?.resumeUrl &&
      prefSectorIds.length === 0 &&
      !profile.talent?.about?.trim() &&
      (profile.talent?.skills?.length ?? 0) === 0
    ) {
      setOnboardingOpen(true)
    }
  }, [
    currentUserId,
    profile,
    settings.forYou?.onboardingCompleted,
    prefSectorIds.length,
  ])

  const handleSectorsSaved = async () => {
    await refreshSettings()
    refreshJobs()
    setView('forYou')
  }

  const { sectors } = useSectorsQuery(auth.status === 'authenticated')
  const { locations } = useJobLocationsQuery(auth.status === 'authenticated')
  const { counts: forYouJobTypeCounts } = useJobTypesQuery(
    true,
    auth.status === 'authenticated',
  )
  const { counts: allJobTypeCounts } = useJobTypesQuery(
    false,
    auth.status === 'authenticated',
  )
  const activeJobTypeCounts =
    view === 'forYou' ? forYouJobTypeCounts : allJobTypeCounts

  const forYouSectors = sectors.filter((s) =>
    prefSectorIds.includes(s.id.toLowerCase()),
  )

  // "Best match" ranks the feed by AI match score — the For You view's
  // default sort (falls back to newest until scores exist).
  const sortOptions =
    view === 'forYou'
      ? [{ value: 'match', label: 'Best match' }, ...SORT_OPTIONS]
      : SORT_OPTIONS

  // Debounced search input → applied query for "For you" feed
  useEffect(() => {
    const t = setTimeout(() => {
      setForYouFilters((prev) => {
        const trimmed = prev.searchInput.trim()
        if (prev.appliedQ === trimmed) return prev
        return { ...prev, appliedQ: trimmed, page: 1 }
      })
    }, 350)
    return () => clearTimeout(t)
  }, [forYouFilters.searchInput])

  // Debounced search input → applied query for "All jobs" feed
  useEffect(() => {
    const t = setTimeout(() => {
      setAllJobsFilters((prev) => {
        const trimmed = prev.searchInput.trim()
        if (prev.appliedQ === trimmed) return prev
        return { ...prev, appliedQ: trimmed, page: 1 }
      })
    }, 350)
    return () => clearTimeout(t)
  }, [allJobsFilters.searchInput])

  // GA4: report the applied (debounced) search term once per query
  useEffect(() => {
    if (!currentFilters.appliedQ) return
    trackEvent('search', { search_term: currentFilters.appliedQ, view })
  }, [currentFilters.appliedQ, view])

  // --- SWR job list queries ---
  // Both views fire as soon as auth is ready, using their own independent parameters.
  const authReady = auth.status === 'authenticated'

  const forYouParams = {
    sort: forYouFilters.sort,
    page: forYouFilters.page,
    pageSize,
    q: forYouFilters.appliedQ || undefined,
    jobType: forYouFilters.jobType || undefined,
    location: forYouFilters.location.trim() || undefined,
    sectorId: forYouFilters.sectorId || undefined,
    source: forYouFilters.sources.length > 0 ? forYouFilters.sources.join(',') : undefined,
    experienceLevel: forYouFilters.experience || undefined,
    workMode: forYouFilters.workArrangement || undefined,
    postedWithin: forYouFilters.postedWithin ? Number(forYouFilters.postedWithin) : undefined,
    closingWithin: forYouFilters.closingWithin ? Number(forYouFilters.closingWithin) : undefined,
    salaryMin: forYouFilters.salaryMin ? Number(forYouFilters.salaryMin) : undefined,
    salaryMax: forYouFilters.salaryMax ? Number(forYouFilters.salaryMax) : undefined,
  }

  const allJobsParams = {
    sort: allJobsFilters.sort,
    page: allJobsFilters.page,
    pageSize,
    q: allJobsFilters.appliedQ || undefined,
    jobType: allJobsFilters.jobType || undefined,
    location: allJobsFilters.location.trim() || undefined,
    sectorId: allJobsFilters.sectorId || undefined,
    source: allJobsFilters.sources.length > 0 ? allJobsFilters.sources.join(',') : undefined,
    experienceLevel: allJobsFilters.experience || undefined,
    workMode: allJobsFilters.workArrangement || undefined,
    postedWithin: allJobsFilters.postedWithin ? Number(allJobsFilters.postedWithin) : undefined,
    closingWithin: allJobsFilters.closingWithin ? Number(allJobsFilters.closingWithin) : undefined,
    salaryMin: allJobsFilters.salaryMin ? Number(allJobsFilters.salaryMin) : undefined,
    salaryMax: allJobsFilters.salaryMax ? Number(allJobsFilters.salaryMax) : undefined,
  }

  // "For you" — preference-filtered feed (enabled once profile loads)
  const forYouQuery = useJobsQuery(
    { ...forYouParams, forMe: true },
    authReady && prefSectorIds.length > 0,
  )
  // "All jobs" — always fires on login so tab switching is instant
  const allJobsQuery = useJobsQuery(
    { ...allJobsParams, forMe: undefined },
    authReady,
  )

  // Pick the active view's data
  const activeQuery = view === 'forYou' ? forYouQuery : allJobsQuery
  const { jobs, pagination, isLoading, error, refresh: refreshJobs } = activeQuery

  // --- Saved jobs (shared cache with bookmark buttons) ---
  const { savedIds, toggleSaved } = useSavedJobsToggle()

  // --- Applications (for showing "Applied" on cards) ---
  const { applications: myApplications } = useMyApplicationsQuery(auth.status === 'authenticated')
  const appliedIds = new Set(myApplications.map((a) => a.jobId))

  const { showToast } = useToast()

  const filtersDirty =
    currentFilters.appliedQ !== '' ||
    currentFilters.jobType !== '' ||
    currentFilters.location.trim() !== '' ||
    currentFilters.sectorId !== '' ||
    currentFilters.sources.length > 0 ||
    currentFilters.experience !== '' ||
    currentFilters.workArrangement !== '' ||
    currentFilters.postedWithin !== '' ||
    currentFilters.closingWithin !== '' ||
    currentFilters.salaryMin !== '' ||
    currentFilters.salaryMax !== '' ||
    currentFilters.draftJobType !== '' ||
    currentFilters.draftLocation.trim() !== '' ||
    currentFilters.draftSector !== '' ||
    currentFilters.draftSources.length > 0 ||
    currentFilters.draftExperience !== '' ||
    currentFilters.draftWorkArrangement !== '' ||
    currentFilters.draftPostedWithin !== '' ||
    currentFilters.draftClosingWithin !== '' ||
    currentFilters.draftSalaryMin !== '' ||
    currentFilters.draftSalaryMax !== ''

  const setView = (next: 'forYou' | 'all') => {
    setSearchParams(next === 'forYou' ? {} : { view: 'all' }, { replace: true })
  }

  const applyFilters = () => {
    setCurrentFilters((prev) => ({
      ...prev,
      jobType: prev.draftJobType,
      location: prev.draftLocation,
      sectorId: prev.draftSector,
      sources: prev.draftSources,
      experience: prev.draftExperience,
      workArrangement: prev.draftWorkArrangement,
      postedWithin: prev.draftPostedWithin,
      closingWithin: prev.draftClosingWithin,
      salaryMin: prev.draftSalaryMin,
      salaryMax: prev.draftSalaryMax,
      page: 1,
    }))
  }

  const clearFilters = () => {
    setCurrentFilters((prev) => ({
      ...prev,
      searchInput: '',
      appliedQ: '',
      jobType: '',
      location: '',
      sectorId: '',
      sources: [],
      experience: '',
      workArrangement: '',
      postedWithin: '',
      closingWithin: '',
      salaryMin: '',
      salaryMax: '',
      draftJobType: '',
      draftLocation: '',
      draftSector: '',
      draftSources: [],
      draftExperience: '',
      draftWorkArrangement: '',
      draftPostedWithin: '',
      draftClosingWithin: '',
      draftSalaryMin: '',
      draftSalaryMax: '',
      page: 1,
    }))
  }

  // --- "Run AI matching" (For You view) ---
  // Calls the AI service to score the caller's feed jobs against their
  // profile right now, then re-fetches so each card shows its match %.
  const [matching, setMatching] = useState(false)

  const handleRunMatching = async () => {
    if (matching) return
    const tokens = getStoredAuthTokens()
    if (!tokens?.accessToken) return
    setMatching(true)
    try {
      const result = await runForYouMatching(tokens.accessToken)
      const upToDate = result.scored === 0 && (result.cached ?? 0) > 0
      if (result.scored > 0 || upToDate) {
        showToast(
          result.scored > 0
            ? `AI matched ${result.scored} job${result.scored === 1 ? '' : 's'} to your profile`
            : (result.message ?? 'Your matches are already up to date.'),
        )
        // Rank the feed best-match-first: the list endpoint annotates each
        // job with its now-stored score and orders by it (highest first).
        if (currentFilters.sort === 'match') {
          await refreshJobs()
        } else {
          setCurrentFilters((prev) => ({ ...prev, sort: 'match', page: 1 }))
        }
      } else {
        showToast(
          result.message ??
            'No scores were computed — make sure the AI service is running.',
          'error',
        )
      }
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error')
    } finally {
      setMatching(false)
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

  const from =
    pagination && pagination.totalCount > 0
      ? (currentFilters.page - 1) * pagination.pageSize + 1
      : 0
  const to = pagination
    ? Math.min(currentFilters.page * pagination.pageSize, pagination.totalCount)
    : 0

  return (
    <DashboardShell role="Talent" authUser={auth.user}>
      {/* Page header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
            Find jobs
          </h1>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            {view === 'forYou'
              ? 'Jobs in the sectors you follow — nothing you\'d scroll past.'
              : 'Browse every open position on the platform — search, filter, and apply.'}
          </p>
        </div>
      </div>

      {/* For you / All jobs toggle */}
      <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex rounded-lg border border-surface-variant bg-surface-container-lowest p-1 shadow-sm">
          <button
            type="button"
            onClick={() => setView('forYou')}
            aria-pressed={view === 'forYou'}
            className={cn(
              'flex items-center gap-1.5 rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
              view === 'forYou'
                ? 'bg-primary font-medium text-on-primary'
                : 'text-on-surface-variant hover:text-on-surface',
            )}
          >
            <span className="material-symbols-outlined text-lg">
              auto_awesome
            </span>
            For you
          </button>
          <button
            type="button"
            onClick={() => setView('all')}
            aria-pressed={view === 'all'}
            className={cn(
              'flex items-center gap-1.5 rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
              view === 'all'
                ? 'bg-primary font-medium text-on-primary'
                : 'text-on-surface-variant hover:text-on-surface',
            )}
          >
            <span className="material-symbols-outlined text-lg">
              format_list_bulleted
            </span>
            All jobs
          </button>
        </div>
        {view === 'forYou' && prefSectorIds.length > 0 && (
          <p className="font-label-sm text-label-sm text-on-surface-variant">
            Showing jobs from {prefSectorIds.length} For You sector
            {prefSectorIds.length === 1 ? '' : 's'} —{' '}
            <Link
              to="/dashboard/talent/settings"
              className="text-primary transition-colors hover:text-surface-tint"
            >
              manage sectors
            </Link>
          </p>
        )}
      </div>

      {/* Error banner */}
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
            onClick={() => refreshJobs()}
            className="rounded-lg border border-on-error-container/30 px-3 py-1.5 font-label-md text-label-md font-medium transition-colors hover:bg-on-error-container/10"
          >
            Retry
          </button>
        </div>
      )}

      {/* Search + filter toolbar */}
      <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center">
        <div className="relative w-full flex-1">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">
            search
          </span>
          <input
            type="search"
            value={currentFilters.searchInput}
            onChange={(e) =>
              setCurrentFilters((prev) => ({
                ...prev,
                searchInput: e.target.value,
              }))
            }
            placeholder={
              view === 'forYou'
                ? 'Search within your For You feed...'
                : 'Search all job titles, companies, or keywords...'
            }
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 pl-10 pr-4 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex items-center gap-3">
          {filtersDirty && (
            <button
              type="button"
              onClick={clearFilters}
              className="font-label-md text-label-md text-on-surface-variant transition-colors hover:text-on-surface"
            >
              Clear filters
            </button>
          )}
          <label className="flex items-center gap-2 font-label-md text-label-md text-on-surface-variant">
            Sort by
            <select
              value={currentFilters.sort}
              onChange={(e) => {
                setCurrentFilters((prev) => ({
                  ...prev,
                  sort: e.target.value,
                  page: 1,
                }))
              }}
              aria-label="Sort jobs"
              className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            >
              {sortOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          {currentFilters.sort !== (view === 'forYou' ? 'match' : 'newest') && (
            <button
              type="button"
              onClick={() => {
                setCurrentFilters((prev) => ({
                  ...prev,
                  sort: view === 'forYou' ? 'match' : 'newest',
                  page: 1,
                }))
              }}
              className="font-label-md text-label-md text-on-surface-variant transition-colors hover:text-on-surface"
            >
              Clear sort
            </button>
          )}
        </div>
      </div>

      {/* Results column + right-side filter panel */}
      <div className="mt-6 flex flex-col gap-6 lg:flex-row lg:items-start">
        {/* Left: result count + cards + pagination */}
        <div className="min-w-0 flex-1">
          {!isLoading && !error && pagination && (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="font-label-md text-label-md text-on-surface-variant">
                {pagination.totalCount === 0
                  ? 'No jobs found'
                  : `${pagination.totalCount} ${
                      pagination.totalCount === 1 ? 'job' : 'jobs'
                    } ${view === 'forYou' ? 'matched to your preferences' : 'available'}`}
              </p>
              {view === 'forYou' && pagination.totalCount > 0 && (
                <button
                  type="button"
                  onClick={handleRunMatching}
                  disabled={matching}
                  title="Score every job in your feed against your profile and preferences with AI"
                  className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 px-4 py-1.5 font-label-md text-label-md font-medium text-primary transition-colors hover:bg-primary-container/40 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  <span
                    className={`material-symbols-outlined text-base ${matching ? 'animate-spin' : ''}`}
                  >
                    {matching ? 'progress_activity' : 'auto_awesome'}
                  </span>
                  {matching ? 'Matching jobs…' : 'Run AI matching'}
                </button>
              )}
            </div>
          )}

          {view === 'forYou' && prefSectorIds.length === 0 ? (
            <div className="mt-5">
              <PreferencesPrompt
                onChooseSectors={() => setOnboardingOpen(true)}
                onBrowseAll={() => setView('all')}
              />
            </div>
          ) : isLoading ? (
            <div className="mt-5">
              <SkeletonGrid count={pageSize} />
            </div>
          ) : error ? null : jobs.length === 0 ? (
            <div className="mt-5">
              <EmptyState
                hasFilters={filtersDirty}
                onClear={clearFilters}
                forYou={view === 'forYou'}
              />
            </div>
          ) : (
            <div className="mt-5 grid grid-cols-1 gap-5 xl:grid-cols-2">
              {jobs.map((job) => (
                <JobCard
                  key={job.id}
                  job={job}
                  saved={savedIds.has(job.id)}
                  applied={appliedIds.has(job.id)}
                  onToggleSave={() => {
                    toggleSaved(job.id).then((result) => {
                      if (result === 'saved') showToast('Job saved for later')
                      else if (result === 'removed')
                        showToast('Removed from saved jobs')
                      else showToast('Could not update saved jobs', 'error')
                    })
                  }}
                />
              ))}
            </div>
          )}

          {pagination && pagination.totalCount > 0 && (
            <div className="mt-6 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
              <span className="font-label-md text-label-md text-on-surface-variant">
                Showing {from} to {to} of {pagination.totalCount} results
              </span>
              <div className="flex flex-wrap items-center gap-3">
                <label className="flex items-center gap-2 font-label-md text-label-md text-on-surface-variant">
                  Per page
                  <select
                    value={pageSize}
                    onChange={(e) => {
                      setPageSize(Number(e.target.value))
                      setCurrentFilters((prev) => ({ ...prev, page: 1 }))
                    }}
                    aria-label="Jobs per page"
                    className="rounded-lg border border-outline-variant bg-surface-container-lowest px-2 py-1.5 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    {PAGE_SIZE_OPTIONS.map((size) => (
                      <option key={size} value={size}>
                        {size}
                      </option>
                    ))}
                  </select>
                </label>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    disabled={currentFilters.page <= 1}
                    onClick={() =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        page: Math.max(1, prev.page - 1),
                      }))
                    }
                    aria-label="Previous page"
                    className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40"
                  >
                    <span className="material-symbols-outlined text-lg">
                      chevron_left
                    </span>
                  </button>
                  <span className="font-label-md text-label-md text-on-surface-variant">
                    Page {pagination.page} of {Math.max(pagination.totalPages, 1)}
                  </span>
                  <button
                    type="button"
                    disabled={!pagination.hasNextPage}
                    onClick={() =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        page: prev.page + 1,
                      }))
                    }
                    aria-label="Next page"
                    className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40"
                  >
                    <span className="material-symbols-outlined text-lg">
                      chevron_right
                    </span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Right: collapsible filter panel — expanded by default */}
        <aside className="w-full shrink-0 lg:sticky lg:top-20 lg:w-64">
          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
            <button
              type="button"
              onClick={() => setFiltersOpen((o) => !o)}
              aria-expanded={filtersOpen}
              className="flex w-full items-center justify-between gap-2 px-4 py-3.5 font-label-md text-label-md font-semibold text-on-surface"
            >
              <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-lg text-primary">
                  tune
                </span>
                Filters
                {filtersDirty && (
                  <span
                    aria-hidden="true"
                    className="h-2 w-2 rounded-full bg-accent"
                  />
                )}
              </span>
              <span className="material-symbols-outlined text-lg text-on-surface-variant transition-transform">
                {filtersOpen ? 'expand_less' : 'expand_more'}
              </span>
            </button>
            {filtersOpen && (
              <div className="space-y-4 border-t border-surface-variant px-4 py-4">
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Website
                  </label>
                  <div className="mt-2 space-y-1.5">
                    {SOURCE_OPTIONS.map((option) => (
                      <label
                        key={option.value}
                        className="flex cursor-pointer items-center gap-2 font-body-md text-body-md text-on-surface"
                      >
                        <input
                          type="checkbox"
                          checked={currentFilters.draftSources.includes(option.value)}
                          onChange={(e) => {
                            setCurrentFilters((prev) => ({
                              ...prev,
                              draftSources: e.target.checked
                                ? [...prev.draftSources, option.value]
                                : prev.draftSources.filter((v) => v !== option.value),
                            }))
                          }}
                          className="h-4 w-4 rounded border-outline-variant text-primary focus:ring-primary"
                        />
                        {option.label}
                      </label>
                    ))}
                  </div>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Job type
                  </label>
                  <select
                    value={currentFilters.draftJobType}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftJobType: e.target.value as JobType | '',
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    {JOB_TYPE_OPTIONS.map((option) => {
                      if (!option.value) {
                        return (
                          <option key="" value="">
                            All types
                          </option>
                        )
                      }
                      const count = activeJobTypeCounts
                        ? activeJobTypeCounts[option.value]
                        : undefined
                      const hasCountData = count !== undefined
                      // Only show job types that have jobs in this feed (or currently selected)
                      if (
                        hasCountData &&
                        count === 0 &&
                        currentFilters.draftJobType !== option.value
                      ) {
                        return null
                      }
                      return (
                        <option key={option.value} value={option.value}>
                          {option.label}
                          {hasCountData ? ` (${count})` : ''}
                        </option>
                      )
                    })}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Work arrangement
                  </label>
                  <select
                    value={currentFilters.draftWorkArrangement}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftWorkArrangement: e.target.value,
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    {WORK_ARRANGEMENT_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Location
                  </label>
                  <select
                    value={currentFilters.draftLocation}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftLocation: e.target.value,
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    <option value="">All locations</option>
                    {locations.map((location) => (
                      <option key={location} value={location}>
                        {location}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Sector
                  </label>
                  <select
                    value={currentFilters.draftSector}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftSector: e.target.value,
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    <option value="">
                      {view === 'forYou' ? 'All followed sectors' : 'All sectors'}
                    </option>
                    {(view === 'forYou'
                      ? (forYouSectors.length > 0 ? forYouSectors : sectors)
                      : sectors
                    ).map((sector) => (
                      <option key={sector.id} value={sector.id}>
                        {sector.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Experience level
                  </label>
                  <select
                    value={currentFilters.draftExperience}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftExperience: e.target.value,
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    {EXPERIENCE_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Posted within
                  </label>
                  <select
                    value={currentFilters.draftPostedWithin}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftPostedWithin: e.target.value,
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    {POSTED_WITHIN_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Closing within
                  </label>
                  <select
                    value={currentFilters.draftClosingWithin}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftClosingWithin: e.target.value,
                      }))
                    }
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    {CLOSING_WITHIN_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Min salary (ETB)
                  </label>
                  <input
                    type="number"
                    value={currentFilters.draftSalaryMin}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftSalaryMin: e.target.value,
                      }))
                    }
                    placeholder="e.g. 20000"
                    min="0"
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
                <div>
                  <label className="font-label-sm text-label-sm font-medium text-on-surface">
                    Max salary (ETB)
                  </label>
                  <input
                    type="number"
                    value={currentFilters.draftSalaryMax}
                    onChange={(e) =>
                      setCurrentFilters((prev) => ({
                        ...prev,
                        draftSalaryMax: e.target.value,
                      }))
                    }
                    placeholder="e.g. 80000"
                    min="0"
                    className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
                <div className="flex gap-2 pt-1">
                  <button
                    type="button"
                    onClick={clearFilters}
                    className="flex-1 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                  >
                    Clear filters
                  </button>
                  <button
                    type="button"
                    onClick={applyFilters}
                    className="flex-1 rounded-lg bg-primary px-4 py-2 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
                  >
                    Apply
                  </button>
                </div>
              </div>
            )}
          </div>
        </aside>
      </div>

      <TalentOnboardingModal
        open={onboardingOpen}
        userId={auth.user.id}
        onClose={() => setOnboardingOpen(false)}
        onSectorsSaved={handleSectorsSaved}
      />
    </DashboardShell>
  )
}

/* --------------------------------------------------------------- Card */

function JobCard({
  job,
  saved,
  applied,
  onToggleSave,
}: {
  job: JobResponse
  saved: boolean
  applied: boolean
  onToggleSave: () => void
}) {
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
    ...(experienceLabel
      ? [{ icon: 'work', label: experienceLabel }]
      : []),
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

  const skills = parseSkills(job.skills).slice(0, 4)  // show max 4 chips on card

  return (
    <article
      className="group flex flex-col overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest transition-all duration-300 hover:border-outline-variant hover:shadow-sm"
      onMouseEnter={() => prefetchJob(job.id)}
    >
      {/* Header: company mark + title (mirrors the landing page cards) */}
      <div className="flex flex-grow flex-row items-center gap-4 border-b border-surface-variant/50 p-5">
        <div className="flex h-11 w-24 shrink-0 items-center justify-center overflow-hidden rounded bg-surface-container-low px-2">
          {brandLogo ? (
            <img
              className="max-h-8 w-auto max-w-full object-contain"
              src={brandLogo}
              alt={job.sourceName ?? ''}
            />
          ) : job.companyLogoUrl ? (
            <img
              className="max-h-8 w-auto max-w-full object-contain"
              src={job.companyLogoUrl}
              alt=""
            />
          ) : job.company ? (
            <span className="font-headline-md text-headline-md font-semibold text-primary">
              {initialsOf(job.company)}
            </span>
          ) : (
            <span className="material-symbols-outlined text-2xl text-on-surface-variant">
              work
            </span>
          )}
        </div>
        <div className="min-w-0">
          <h4 className="mb-1 truncate font-headline-md text-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint">
            {job.title}
          </h4>
          <p className="truncate font-body-md text-body-md text-secondary">
            {job.company || 'Company undisclosed'}
          </p>
          {job.sourceName &&
            job.sourceName.toLowerCase() !== 'serago' &&
            (job.sourceUrl ? (
              <a
                href={job.sourceUrl}
                target="_blank"
                rel="noreferrer"
                className="mt-0.5 inline-flex items-center gap-1 font-label-sm text-label-sm text-primary transition-colors hover:text-surface-tint"
              >
                via {job.sourceName}
                <span className="material-symbols-outlined text-sm">
                  open_in_new
                </span>
              </a>
            ) : (
              <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                via {job.sourceName}
              </p>
            ))}
        </div>
        {job.matchScore != null && (
          <MatchScoreChip
            score={job.matchScore}
            matched={job.matchedSkills?.length ?? 0}
            missing={job.missingSkills?.length ?? 0}
          />
        )}
      </div>

      {/* Meta grid + actions */}
      <div className="flex flex-col gap-4 bg-surface/30 p-5">
        <div className="grid grid-cols-2 gap-3 font-label-md text-label-md text-on-surface-variant">
          {meta.map((item) => (
            <div key={item.icon} className="flex items-center gap-2">
              <span className="material-symbols-outlined text-[18px]">
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
                className="inline-flex items-center rounded-full bg-primary-container/40 px-2.5 py-0.5 font-label-sm text-label-sm text-primary"
              >
                {skill}
              </span>
            ))}
          </div>
        )}
        <div className="mt-auto flex gap-2">
          {applied ? (
            <span
              className="flex-1 rounded border border-success/30 bg-success/10 px-4 py-2.5 text-center font-label-md text-label-md text-success"
            >
              Applied
            </span>
          ) : job.url ? (
            <a
              href={job.url}
              target="_blank"
              rel="noreferrer"
              className="flex-1 rounded bg-accent px-4 py-2.5 text-center font-label-md text-label-md text-on-accent transition-colors hover:opacity-90"
            >
              Apply Now
            </a>
          ) : (
            <span
              title="No application link provided"
              className="flex-1 cursor-not-allowed rounded bg-accent px-4 py-2.5 text-center font-label-md text-label-md text-on-accent opacity-60"
            >
              Apply Now
            </span>
          )}
          <Link
            to={jobDetailPath(job)}
            className="flex-1 rounded border border-surface-variant px-4 py-2.5 text-center font-label-md text-label-md text-primary transition-colors hover:bg-surface-container-low"
          >
            See description
          </Link>
          <SaveJobButton saved={saved} onToggle={onToggleSave} />
        </div>
      </div>
    </article>
  )
}

/* -------------------------------------------------------- Match chip */

/**
 * "72% match" pill shown on the For You feed when the AI service scored the
 * job against the viewer's profile + For You preferences.
 */
function MatchScoreChip({ score, matched = 0, missing = 0 }: { score: number; matched?: number; missing?: number }) {
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
      className={`ml-auto inline-flex shrink-0 items-center gap-1 self-start rounded-full px-3 py-1 font-label-sm text-label-sm font-semibold ${tone}`}
    >
      <span className="material-symbols-outlined text-sm">auto_awesome</span>
      {score}% match
    </span>
  )
}

/* ------------------------------------------------------------- States */

function SkeletonGrid({ count }: { count: number }) {
  return (
    <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
      {Array.from({ length: count }).map((_, i) => (
        <div
          key={i}
          className="overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest"
        >
          <div className="flex flex-row items-center gap-4 border-b border-surface-variant/50 p-5">
            <div className="h-11 w-24 shrink-0 animate-pulse rounded bg-surface-container-low" />
            <div className="flex-1 space-y-2">
              <div className="h-4 w-3/4 animate-pulse rounded bg-surface-container" />
              <div className="h-3 w-1/3 animate-pulse rounded bg-surface-container-low" />
            </div>
          </div>
          <div className="space-y-4 bg-surface/30 p-5">
            <div className="grid grid-cols-2 gap-3">
              {Array.from({ length: 4 }).map((_, j) => (
                <div
                  key={j}
                  className="h-3 animate-pulse rounded bg-surface-container-low"
                />
              ))}
            </div>
            <div className="flex gap-2">
              <div className="h-10 flex-1 animate-pulse rounded bg-surface-container" />
              <div className="h-10 flex-1 animate-pulse rounded bg-surface-container" />
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

function EmptyState({
  hasFilters,
  onClear,
  forYou = false,
}: {
  hasFilters: boolean
  onClear: () => void
  /** Personalized-feed variant — suggests tuning preferences instead of filters. */
  forYou?: boolean
}) {
  return (
    <div className="mt-5 flex flex-col items-center justify-center rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-16 text-center shadow-sm">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant">
        <span className="material-symbols-outlined text-3xl">
          {hasFilters ? 'filter_alt_off' : forYou ? 'auto_awesome' : 'search_off'}
        </span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        {hasFilters
          ? 'No matching jobs'
          : forYou
            ? 'No jobs match your preferences yet'
            : 'No jobs yet'}
      </h2>
      <p className="mt-1 max-w-sm font-body-md text-body-md text-on-surface-variant">
        {hasFilters
          ? forYou
            ? 'No jobs in your For You feed match your current filters. Try adjusting or clearing your filters.'
            : 'Try adjusting your search or filters.'
          : forYou
            ? 'Try adding a For You sector in settings, or browse everything.'
            : 'There are no open positions right now — check back soon.'}
      </p>
      {hasFilters ? (
        <button
          type="button"
          onClick={onClear}
          className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
        >
          Clear filters
        </button>
      ) : forYou ? (
        <Link
          to="/dashboard/talent/settings"
          className="mt-6 rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
        >
          Open settings
        </Link>
      ) : null}
    </div>
  )
}

/** Shown on "For you" when the talent hasn't set any preferences yet. */
function PreferencesPrompt({
  onChooseSectors,
  onBrowseAll,
}: {
  onChooseSectors: () => void
  onBrowseAll: () => void
}) {
  return (
    <div className="mt-5 flex flex-col items-center justify-center rounded-xl border border-primary/20 bg-primary-container/20 px-6 py-16 text-center shadow-sm">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary">
        <span className="material-symbols-outlined text-3xl">auto_awesome</span>
      </span>
      <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
        Shape the jobs you see
      </h2>
      <p className="mt-1 max-w-md font-body-md text-body-md text-on-surface-variant">
        Pick up to 2 sectors in settings and the For You feed will only show
        jobs in those sectors — no more scrolling past roles you'd never apply for.
      </p>
      <div className="mt-6 flex flex-wrap justify-center gap-3">
        <button
          type="button"
          onClick={onChooseSectors}
          className="rounded-lg bg-primary px-6 py-3 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
        >
          Choose my sectors
        </button>
        <button
          type="button"
          onClick={onBrowseAll}
          className="rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
        >
          Browse all jobs
        </button>
      </div>
    </div>
  )
}

/* ------------------------------------------------------------- Saved jobs toggle (SWR-backed) */

/**
 * Optimistic save/unsave toggle that uses the SWR-cached saved jobs list.
 * After a toggle it mutates the cache so bookmark buttons across all pages
 * update instantly.
 */
function useSavedJobsToggle() {
  const { savedIds, refresh } = useSavedJobsQuery()

  const toggleSaved = async (jobId: string): Promise<'saved' | 'removed' | null> => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return null
    const wasSaved = savedIds.has(jobId)
    // Optimistic update — SWR will revalidate anyway.
    try {
      if (wasSaved) await unsaveJob(jobId, tokens.accessToken)
      else await saveJob(jobId, tokens.accessToken)
      // Revalidate the saved jobs cache so all bookmark buttons reflect the change.
      refresh()
      trackEvent(wasSaved ? 'unsave_job' : 'save_job', { job_id: jobId })
      return wasSaved ? 'removed' : 'saved'
    } catch {
      return null
    }
  }

  return { savedIds, toggleSaved }
}
