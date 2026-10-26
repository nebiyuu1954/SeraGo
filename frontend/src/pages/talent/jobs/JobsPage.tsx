import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { fetchJob, getApiErrorMessage, getStoredAuthTokens } from '../../../api'
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
import { useRequireRole } from '../../../hooks'
import type { JobType, SectorResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import JobCard from '../../../components/jobs/JobCard.tsx'
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
import { saveJob, unsaveJob, runForYouMatching } from '../../../api'
import { trackEvent } from '../../../lib/analytics'

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
  draftSalaryMin: '',
  draftSalaryMax: '',
}

export default function JobsPage() {
  const auth = useRequireRole('Talent')

  // View toggle, persisted in the URL: "For you" (preference-filtered feed,
  // the default) vs. "All jobs" (the classic browse).
  const [searchParams, setSearchParams] = useSearchParams()
  const view = searchParams.get('view') === 'all' ? 'all' : 'forYou'
  const initialSearch = searchParams.get('search') || searchParams.get('q') || ''

  // Independent filter state for "For you" and "All jobs" views:
  const [forYouFilters, setForYouFilters] = useState<ViewFilterState>(() => ({
    ...DEFAULT_FILTER_STATE,
    searchInput: view === 'forYou' ? initialSearch : '',
    appliedQ: view === 'forYou' ? initialSearch : '',
    sort: 'match',
  }))

  const [allJobsFilters, setAllJobsFilters] = useState<ViewFilterState>(() => ({
    ...DEFAULT_FILTER_STATE,
    searchInput: view === 'all' ? initialSearch : '',
    appliedQ: view === 'all' ? initialSearch : '',
    sort: 'newest',
  }))

  // Sync search parameter from URL if query param updates
  useEffect(() => {
    const qParam = searchParams.get('search') || searchParams.get('q') || ''
    if (qParam) {
      if (view === 'all') {
        setAllJobsFilters((prev) => (prev.appliedQ === qParam ? prev : {
          ...prev,
          searchInput: qParam,
          appliedQ: qParam,
          page: 1,
        }))
      } else {
        setForYouFilters((prev) => (prev.appliedQ === qParam ? prev : {
          ...prev,
          searchInput: qParam,
          appliedQ: qParam,
          page: 1,
        }))
      }
    }
  }, [searchParams, view])

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
  const [mobileFilterOpen, setMobileFilterOpen] = useState(false)

  // --- SWR-cached data (shared across all mounted instances) ---

  // The "For you" sectors live in settings (they power the feed + job alerts).
  const { settings, isLoading: settingsLoading, refresh: refreshSettings } = useSettingsQuery()
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

  const { sectors, isLoading: sectorsLoading } = useSectorsQuery(auth.status === 'authenticated')
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

  // Debounced salary inputs for "For you" feed
  useEffect(() => {
    const t = setTimeout(() => {
      setForYouFilters((prev) => {
        if (
          prev.salaryMin === prev.draftSalaryMin &&
          prev.salaryMax === prev.draftSalaryMax
        ) {
          return prev
        }
        return {
          ...prev,
          salaryMin: prev.draftSalaryMin,
          salaryMax: prev.draftSalaryMax,
          page: 1,
        }
      })
    }, 400)
    return () => clearTimeout(t)
  }, [forYouFilters.draftSalaryMin, forYouFilters.draftSalaryMax])

  // Debounced salary inputs for "All jobs" feed
  useEffect(() => {
    const t = setTimeout(() => {
      setAllJobsFilters((prev) => {
        if (
          prev.salaryMin === prev.draftSalaryMin &&
          prev.salaryMax === prev.draftSalaryMax
        ) {
          return prev
        }
        return {
          ...prev,
          salaryMin: prev.draftSalaryMin,
          salaryMax: prev.draftSalaryMax,
          page: 1,
        }
      })
    }, 400)
    return () => clearTimeout(t)
  }, [allJobsFilters.draftSalaryMin, allJobsFilters.draftSalaryMax])

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

  const activeFilterCount =
    (currentFilters.jobType !== '' ? 1 : 0) +
    (currentFilters.location.trim() !== '' ? 1 : 0) +
    (currentFilters.sectorId !== '' ? 1 : 0) +
    currentFilters.sources.length +
    (currentFilters.experience !== '' ? 1 : 0) +
    (currentFilters.workArrangement !== '' ? 1 : 0) +
    (currentFilters.postedWithin !== '' ? 1 : 0) +
    (currentFilters.closingWithin !== '' ? 1 : 0) +
    (currentFilters.salaryMin ||
    currentFilters.salaryMax ||
    currentFilters.draftSalaryMin ||
    currentFilters.draftSalaryMax
      ? 1
      : 0)

  const filtersDirty = currentFilters.appliedQ !== '' || activeFilterCount > 0

  const targetJobProcessedRef = useRef(false)

  const setView = (next: 'forYou' | 'all') => {
    // If switching to all jobs and allJobsFilters search is empty while forYouFilters has a search term, carry it over
    if (next === 'all' && forYouFilters.searchInput.trim() && !allJobsFilters.searchInput.trim()) {
      setAllJobsFilters((prev) => ({
        ...prev,
        searchInput: forYouFilters.searchInput,
        appliedQ: forYouFilters.appliedQ,
        page: 1,
      }))
    }
    setSearchParams(next === 'forYou' ? {} : { view: 'all' }, { replace: true })
  }

  // Handle incoming target job remembered from Telegram entry link
  useEffect(() => {
    if (auth.status !== 'authenticated' || settingsLoading || !profile) return
    if (prefSectorIds.length > 0 && sectorsLoading) return // Wait until sectors load for accurate sector matching
    if (onboardingOpen) return // Wait until user completes or skips onboarding
    if (targetJobProcessedRef.current) return

    const pendingJobId =
      sessionStorage.getItem('serago_pending_target_job_id') ||
      localStorage.getItem('serago_pending_target_job_id')
    const pendingJobTitle =
      sessionStorage.getItem('serago_pending_target_job_title') ||
      localStorage.getItem('serago_pending_target_job_title')
    const pendingJobSectorId =
      sessionStorage.getItem('serago_pending_target_job_sector_id') ||
      localStorage.getItem('serago_pending_target_job_sector_id')
    const pendingJobSectorName =
      sessionStorage.getItem('serago_pending_target_job_sector_name') ||
      localStorage.getItem('serago_pending_target_job_sector_name')

    if (!pendingJobId && !pendingJobTitle) return

    targetJobProcessedRef.current = true

    const clearPendingStorage = () => {
      try {
        sessionStorage.removeItem('serago_pending_target_job_id')
        sessionStorage.removeItem('serago_pending_target_job_title')
        sessionStorage.removeItem('serago_pending_target_job_sector_id')
        sessionStorage.removeItem('serago_pending_target_job_sector_name')
        localStorage.removeItem('serago_pending_target_job_id')
        localStorage.removeItem('serago_pending_target_job_title')
        localStorage.removeItem('serago_pending_target_job_sector_id')
        localStorage.removeItem('serago_pending_target_job_sector_name')
      } catch {}
    }

    const applySearch = (title: string, sectorId?: string | null, sectorName?: string | null) => {
      // Check if user has preferred sectors AND if the clicked job's sector matches one of the user's preferred sectors
      let isSectorMatch = false
      if (prefSectorIds.length > 0) {
        const normJobSectorId = sectorId ? sectorId.toLowerCase().trim() : null
        const normJobSectorName = sectorName ? sectorName.toLowerCase().trim() : null

        // 1. Direct ID match
        if (normJobSectorId && prefSectorIds.includes(normJobSectorId)) {
          isSectorMatch = true
        }

        // 2. Direct Name match against user's preferred sector names
        if (!isSectorMatch && normJobSectorName) {
          const prefNames = forYouSectors.map((s) => s.name.toLowerCase().trim())
          if (prefNames.includes(normJobSectorName)) {
            isSectorMatch = true
          } else {
            // Loose matching for sector variations (e.g. "Technology & IT" vs "Technology")
            isSectorMatch = forYouSectors.some((s) => {
              const sName = s.name.toLowerCase().trim()
              return (
                sName === normJobSectorName ||
                normJobSectorName.includes(sName) ||
                sName.includes(normJobSectorName)
              )
            })
          }
        }
      }

      if (isSectorMatch) {
        // Job sector matches user's sector -> search in "For you"
        setView('forYou')
        setForYouFilters((prev) => ({
          ...prev,
          searchInput: title,
          appliedQ: title,
          page: 1,
        }))
      } else {
        // Job sector does NOT match user's sector (or user has no sectors) -> search in "All jobs"
        setView('all')
        setAllJobsFilters((prev) => ({
          ...prev,
          searchInput: title,
          appliedQ: title,
          page: 1,
        }))
      }

      clearPendingStorage()
    }

    if (pendingJobId && (!pendingJobTitle || (!pendingJobSectorId && !pendingJobSectorName))) {
      fetchJob(pendingJobId)
        .then((j) => {
          if (j?.title) {
            applySearch(
              j.title,
              j.sectorId ?? pendingJobSectorId,
              j.sectorName ?? pendingJobSectorName,
            )
          } else {
            clearPendingStorage()
          }
        })
        .catch(() => {
          clearPendingStorage()
        })
    } else if (pendingJobTitle) {
      applySearch(pendingJobTitle, pendingJobSectorId, pendingJobSectorName)
    }
  }, [
    auth.status,
    settingsLoading,
    sectorsLoading,
    profile,
    onboardingOpen,
    prefSectorIds.length,
    forYouSectors,
  ])

  // Handle background auto-save of pending saved job after registration
  useEffect(() => {
    if (auth.status !== 'authenticated') return
    const pendingSaveId =
      sessionStorage.getItem('serago_pending_save_job_id') ||
      localStorage.getItem('serago_pending_save_job_id')
    if (!pendingSaveId) return

    try {
      sessionStorage.removeItem('serago_pending_save_job_id')
      localStorage.removeItem('serago_pending_save_job_id')
    } catch {}

    toggleSaved(pendingSaveId).then((result) => {
      if (result === 'saved') {
        showToast('Job saved to your bookmarks')
      }
    })
  }, [auth.status, showToast, toggleSaved])

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
      draftSalaryMin: '',
      draftSalaryMax: '',
      page: 1,
    }))
  }

  const removeFilter = (key: keyof ViewFilterState, valueToRemove?: string) => {
    setCurrentFilters((prev) => {
      if (key === 'sources' && valueToRemove) {
        return {
          ...prev,
          sources: prev.sources.filter((s) => s !== valueToRemove),
          page: 1,
        }
      }
      if (key === 'salaryMin' || key === 'salaryMax') {
        return {
          ...prev,
          salaryMin: '',
          salaryMax: '',
          draftSalaryMin: '',
          draftSalaryMax: '',
          page: 1,
        }
      }
      return {
        ...prev,
        [key]: '',
        page: 1,
      }
    })
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
        <div className="flex flex-wrap items-center gap-3">
          {/* Mobile Filter Button (visible only on < lg) */}
          <button
            type="button"
            onClick={() => setMobileFilterOpen(true)}
            className="flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low lg:hidden"
          >
            <span className="material-symbols-outlined text-lg text-primary">
              tune
            </span>
            <span>Filters</span>
            {activeFilterCount > 0 && (
              <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-bold text-on-primary">
                {activeFilterCount}
              </span>
            )}
          </button>

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

          {/* Active filter chips */}
          {activeFilterCount > 0 && (
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <span className="font-label-sm text-label-sm font-medium text-on-surface-variant">
                Active filters:
              </span>

              {/* Job Type chip */}
              {currentFilters.jobType && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    Type:{' '}
                    {JOB_TYPE_LABELS[currentFilters.jobType as JobType] ??
                      currentFilters.jobType}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('jobType')}
                    aria-label="Remove job type filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Location chip */}
              {currentFilters.location.trim() && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>Location: {currentFilters.location}</span>
                  <button
                    type="button"
                    onClick={() => removeFilter('location')}
                    aria-label="Remove location filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Sector chip */}
              {currentFilters.sectorId && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    Sector:{' '}
                    {sectors.find((s) => s.id === currentFilters.sectorId)
                      ?.name ?? 'Selected sector'}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('sectorId')}
                    aria-label="Remove sector filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Work Arrangement chip */}
              {currentFilters.workArrangement && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    Work:{' '}
                    {WORK_ARRANGEMENT_OPTIONS.find(
                      (w) => w.value === currentFilters.workArrangement,
                    )?.label ?? currentFilters.workArrangement}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('workArrangement')}
                    aria-label="Remove work arrangement filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Experience chip */}
              {currentFilters.experience && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    Exp:{' '}
                    {EXPERIENCE_OPTIONS.find(
                      (e) => e.value === currentFilters.experience,
                    )?.label ?? currentFilters.experience}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('experience')}
                    aria-label="Remove experience filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Sources chips */}
              {currentFilters.sources.map((src) => {
                const label =
                  SOURCE_OPTIONS.find((s) => s.value === src)?.label ?? src
                return (
                  <span
                    key={src}
                    className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary"
                  >
                    <span>Source: {label}</span>
                    <button
                      type="button"
                      onClick={() => removeFilter('sources', src)}
                      aria-label={`Remove ${label} source filter`}
                      className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                    >
                      <span className="material-symbols-outlined text-xs">
                        close
                      </span>
                    </button>
                  </span>
                )
              })}

              {/* Posted within chip */}
              {currentFilters.postedWithin && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    {POSTED_WITHIN_OPTIONS.find(
                      (p) => p.value === currentFilters.postedWithin,
                    )?.label ?? 'Recent'}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('postedWithin')}
                    aria-label="Remove posted within filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Closing within chip */}
              {currentFilters.closingWithin && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    {CLOSING_WITHIN_OPTIONS.find(
                      (c) => c.value === currentFilters.closingWithin,
                    )?.label ?? 'Closing'}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('closingWithin')}
                    aria-label="Remove closing within filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Salary chip */}
              {(currentFilters.salaryMin || currentFilters.salaryMax) && (
                <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary-container/20 py-1 pl-3 pr-1.5 font-label-sm text-label-sm font-medium text-primary">
                  <span>
                    Salary:{' '}
                    {currentFilters.salaryMin && currentFilters.salaryMax
                      ? `${Number(currentFilters.salaryMin).toLocaleString()} – ${Number(currentFilters.salaryMax).toLocaleString()} ETB`
                      : currentFilters.salaryMin
                        ? `≥ ${Number(currentFilters.salaryMin).toLocaleString()} ETB`
                        : `≤ ${Number(currentFilters.salaryMax).toLocaleString()} ETB`}
                  </span>
                  <button
                    type="button"
                    onClick={() => removeFilter('salaryMin')}
                    aria-label="Remove salary filter"
                    className="flex h-4 w-4 items-center justify-center rounded-full hover:bg-primary/20"
                  >
                    <span className="material-symbols-outlined text-xs">
                      close
                    </span>
                  </button>
                </span>
              )}

              {/* Clear all text button */}
              <button
                type="button"
                onClick={clearFilters}
                className="ml-1 font-label-sm text-label-sm text-on-surface-variant transition-colors hover:text-primary hover:underline"
              >
                Clear all
              </button>
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
                onBrowseAll={() => setView('all')}
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

        {/* Right: desktop sticky filter panel with independent scroll containment */}
        <aside className="hidden lg:block w-72 shrink-0 lg:sticky lg:top-20">
          <div className="flex flex-col rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm lg:max-h-[calc(100vh-6rem)]">
            {/* Pinned Header */}
            <div className="flex items-center justify-between border-b border-surface-variant px-4 py-3 shrink-0">
              <button
                type="button"
                onClick={() => setFiltersOpen((o) => !o)}
                aria-expanded={filtersOpen}
                className="flex items-center gap-2 font-label-md text-label-md font-semibold text-on-surface hover:text-primary transition-colors"
              >
                <span className="material-symbols-outlined text-lg text-primary">
                  tune
                </span>
                <span>Filters</span>
                {activeFilterCount > 0 && (
                  <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary/15 px-1.5 text-xs font-bold text-primary">
                    {activeFilterCount}
                  </span>
                )}
                <span className="material-symbols-outlined text-base text-on-surface-variant">
                  {filtersOpen ? 'expand_less' : 'expand_more'}
                </span>
              </button>
              {activeFilterCount > 0 && (
                <button
                  type="button"
                  onClick={clearFilters}
                  className="font-label-sm text-label-sm font-medium text-primary transition-colors hover:underline"
                >
                  Clear all
                </button>
              )}
            </div>

            {/* Scrollable Filter List - Isolated Scroll with overscroll-contain */}
            {filtersOpen && (
              <div className="flex-1 overflow-y-auto overscroll-contain px-4 py-4 space-y-4">
                <FilterFields
                  filters={currentFilters}
                  onFilterChange={(key, val) => {
                    setCurrentFilters((prev) => ({
                      ...prev,
                      [key]: val,
                      page: 1,
                    }))
                  }}
                  activeJobTypeCounts={activeJobTypeCounts}
                  locations={locations}
                  sectors={sectors}
                  forYouSectors={forYouSectors}
                  view={view}
                />
              </div>
            )}

            {/* Pinned Footer if any filters are active */}
            {filtersOpen && activeFilterCount > 0 && (
              <div className="border-t border-surface-variant p-3 shrink-0 bg-surface-container-lowest">
                <button
                  type="button"
                  onClick={clearFilters}
                  className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
                >
                  Reset all filters
                </button>
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

      {/* Mobile / Tablet Filter Drawer */}
      {mobileFilterOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-black/50 backdrop-blur-xs transition-opacity"
            onClick={() => setMobileFilterOpen(false)}
            aria-hidden="true"
          />

          {/* Drawer container */}
          <div className="fixed inset-y-0 right-0 flex w-full max-w-sm flex-col bg-surface-container-lowest shadow-2xl">
            {/* Header */}
            <div className="flex items-center justify-between border-b border-surface-variant px-5 py-4 shrink-0">
              <div className="flex items-center gap-2 font-title-medium font-semibold text-on-surface">
                <span className="material-symbols-outlined text-xl text-primary">
                  tune
                </span>
                <span>Filter jobs</span>
                {activeFilterCount > 0 && (
                  <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary/15 px-1.5 text-xs font-bold text-primary">
                    {activeFilterCount}
                  </span>
                )}
              </div>
              <button
                type="button"
                onClick={() => setMobileFilterOpen(false)}
                className="flex h-8 w-8 items-center justify-center rounded-lg text-on-surface-variant hover:bg-surface-container-low"
                aria-label="Close filters"
              >
                <span className="material-symbols-outlined text-xl">close</span>
              </button>
            </div>

            {/* Scrollable Body */}
            <div className="flex-1 overflow-y-auto overscroll-contain px-5 py-5 space-y-5">
              <FilterFields
                filters={currentFilters}
                onFilterChange={(key, val) => {
                  setCurrentFilters((prev) => ({
                    ...prev,
                    [key]: val,
                    page: 1,
                  }))
                }}
                activeJobTypeCounts={activeJobTypeCounts}
                locations={locations}
                sectors={sectors}
                forYouSectors={forYouSectors}
                view={view}
              />
            </div>

            {/* Footer */}
            <div className="flex items-center gap-3 border-t border-surface-variant p-4 shrink-0 bg-surface-container-lowest">
              {activeFilterCount > 0 && (
                <button
                  type="button"
                  onClick={clearFilters}
                  className="flex-1 rounded-lg border border-outline-variant py-2.5 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
                >
                  Clear all
                </button>
              )}
              <button
                type="button"
                onClick={() => setMobileFilterOpen(false)}
                className="flex-1 rounded-lg bg-primary py-2.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90 shadow-sm"
              >
                {pagination ? `Show ${pagination.totalCount} results` : 'Done'}
              </button>
            </div>
          </div>
        </div>
      )}
    </DashboardShell>
  )
}

/* ------------------------------------------------------------- Filter Fields */

interface FilterFieldsProps {
  filters: ViewFilterState
  onFilterChange: <K extends keyof ViewFilterState>(
    key: K,
    value: ViewFilterState[K],
  ) => void
  activeJobTypeCounts?: Record<JobType, number> | null
  locations: string[]
  sectors: SectorResponse[]
  forYouSectors: SectorResponse[]
  view: 'forYou' | 'all'
}

function FilterFields({
  filters,
  onFilterChange,
  activeJobTypeCounts,
  locations,
  sectors,
  forYouSectors,
  view,
}: FilterFieldsProps) {
  return (
    <>
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
                checked={filters.sources.includes(option.value)}
                onChange={(e) => {
                  const newSources = e.target.checked
                    ? [...filters.sources, option.value]
                    : filters.sources.filter((v) => v !== option.value)
                  onFilterChange('sources', newSources)
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
          value={filters.jobType}
          onChange={(e) =>
            onFilterChange('jobType', (e.target.value as JobType) || '')
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
              ? activeJobTypeCounts[option.value as JobType]
              : undefined
            const hasCountData = count !== undefined
            if (
              hasCountData &&
              count === 0 &&
              filters.jobType !== option.value
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
          value={filters.workArrangement}
          onChange={(e) => onFilterChange('workArrangement', e.target.value)}
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
          value={filters.location}
          onChange={(e) => onFilterChange('location', e.target.value)}
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
          value={filters.sectorId}
          onChange={(e) => onFilterChange('sectorId', e.target.value)}
          className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        >
          <option value="">
            {view === 'forYou' ? 'All followed sectors' : 'All sectors'}
          </option>
          {(view === 'forYou'
            ? forYouSectors.length > 0
              ? forYouSectors
              : sectors
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
          value={filters.experience}
          onChange={(e) => onFilterChange('experience', e.target.value)}
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
          value={filters.postedWithin}
          onChange={(e) => onFilterChange('postedWithin', e.target.value)}
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
          value={filters.closingWithin}
          onChange={(e) => onFilterChange('closingWithin', e.target.value)}
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
          value={filters.draftSalaryMin}
          onChange={(e) => onFilterChange('draftSalaryMin', e.target.value)}
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
          value={filters.draftSalaryMax}
          onChange={(e) => onFilterChange('draftSalaryMax', e.target.value)}
          placeholder="e.g. 80000"
          min="0"
          className="mt-1.5 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>
    </>
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
  onBrowseAll,
}: {
  hasFilters: boolean
  onClear: () => void
  /** Personalized-feed variant — suggests tuning preferences instead of filters. */
  forYou?: boolean
  onBrowseAll?: () => void
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
        <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
          <button
            type="button"
            onClick={onClear}
            className="rounded-lg bg-primary px-6 py-2.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
          >
            Clear filters
          </button>
          {forYou && onBrowseAll && (
            <button
              type="button"
              onClick={onBrowseAll}
              className="rounded-lg border border-outline-variant bg-surface-container-lowest px-5 py-2.5 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
            >
              Search across All Jobs
            </button>
          )}
        </div>
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
