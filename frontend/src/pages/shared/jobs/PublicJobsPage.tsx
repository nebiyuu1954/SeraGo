import { useState, useId, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  useJobsQuery,
  useSectorsQuery,
  useJobLocationsQuery,
  useSavedJobsQuery,
} from '../../../hooks/query.ts'
import { useAuthUser } from '../../../hooks'
import { saveJob, unsaveJob, getStoredAuthTokens } from '../../../api'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import GuestJobAuthModal from '../../../components/jobs/GuestJobAuthModal.tsx'
import JobCard from '../../../components/jobs/JobCard.tsx'
import type { JobResponse } from '../../../types'

export default function PublicJobsPage() {
  const navigate = useNavigate()
  const auth = useAuthUser()
  const { savedIds, refresh: refreshSaved } = useSavedJobsQuery(auth.status === 'authenticated')
  const { showToast } = useToast()

  const [searchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(searchParams.get('q') || '')
  const [sectorId, setSectorId] = useState(searchParams.get('sectorId') || '')
  const [location, setLocation] = useState(searchParams.get('location') || '')
  const [jobType, setJobType] = useState(searchParams.get('type') || searchParams.get('jobType') || '')
  const [sort, setSort] = useState(searchParams.get('sort') || 'newest')
  const [page, setPage] = useState(1)
  const pageSize = 10

  const handleToggleSave = async (job: JobResponse) => {
    if (auth.status === 'authenticated') {
      const tokens = getStoredAuthTokens()
      if (!tokens) return
      const wasSaved = savedIds.has(job.id)
      try {
        if (wasSaved) await unsaveJob(job.id, tokens.accessToken)
        else await saveJob(job.id, tokens.accessToken)
        showToast(wasSaved ? 'Removed from saved jobs' : 'Job saved for later')
        refreshSaved()
      } catch {
        showToast('Could not update saved jobs', 'error')
      }
    } else {
      try {
        sessionStorage.setItem('serago_pending_save_job_id', job.id)
        localStorage.setItem('serago_pending_save_job_id', job.id)
      } catch {}
      navigate('/login?redirect=%2Fdashboard%2Ftalent%2Fsaved')
    }
  }

  const searchInputId = useId()
  const sectorSelectId = useId()
  const locationSelectId = useId()
  const sortSelectId = useId()

  const [modalJob, setModalJob] = useState<JobResponse | null>(null)
  const [modalMode, setModalMode] = useState<'apply' | 'preview'>('preview')

  const { sectors, isLoading: sectorsLoading } = useSectorsQuery()
  const { locations, isLoading: locationsLoading } = useJobLocationsQuery()

  // Sync state when URL search params change (e.g., clicking taxonomy filters)
  useEffect(() => {
    const qParam = searchParams.get('q') || ''
    const sectorParam = searchParams.get('sectorId') || searchParams.get('sector') || ''
    const locParam = searchParams.get('location') || ''
    const typeParam = searchParams.get('type') || searchParams.get('jobType') || ''
    const sortParam = searchParams.get('sort') || 'newest'

    let resolvedSectorId = sectorParam
    if (sectorParam && sectors.length > 0) {
      const matched = sectors.find(
        (s) =>
          s.id.toLowerCase() === sectorParam.toLowerCase() ||
          s.name.toLowerCase() === sectorParam.toLowerCase() ||
          s.slug.toLowerCase() === sectorParam.toLowerCase(),
      )
      if (matched) resolvedSectorId = matched.id
    }

    setSearchInput(qParam)
    setSectorId(resolvedSectorId)
    setLocation(locParam)
    setJobType(typeParam)
    setSort(sortParam)
    setPage(1)
  }, [searchParams, sectors])

  // Sort sectors from highest job count to lowest
  const sortedSectors = [...sectors].sort(
    (a, b) => (b.jobCount ?? 0) - (a.jobCount ?? 0) || a.name.localeCompare(b.name),
  )

  const { jobs, pagination, isLoading: jobsLoading, error } = useJobsQuery({
    q: searchInput.trim() || undefined,
    sectorId: sectorId || undefined,
    location: location || undefined,
    jobType: jobType || undefined,
    sort,
    page,
    pageSize,
  })

  const hasActiveFilters =
    Boolean(searchInput.trim()) || Boolean(sectorId) || Boolean(location) || Boolean(jobType)

  const clearFilters = () => {
    setSearchInput('')
    setSectorId('')
    setLocation('')
    setJobType('')
    setPage(1)
  }

  const selectedSectorName = sectors.find((s) => s.id === sectorId)?.name

  return (
    <div className="min-h-screen bg-surface">
      {/* Hero Header */}
      <div className="border-b border-surface-variant/40 bg-surface-container-lowest py-10 md:py-14">
        <div className="mx-auto w-full max-w-6xl px-margin-mobile md:px-margin-desktop">
          <div className="flex flex-col gap-3">
            <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-primary/10 px-3 py-1 font-label-sm text-label-sm font-semibold text-primary">
              <span className="material-symbols-outlined text-[16px]">travel_explore</span>
              Live Opportunities
            </span>
            <h1 className="font-display-lg-mobile text-display-lg-mobile text-on-surface md:font-display-lg md:text-display-lg">
              Explore All <span className="text-primary">Verified Jobs</span>
            </h1>
            <p className="max-w-2xl font-body-lg text-body-lg text-secondary">
              Browse current vacancies aggregated and verified across Ethiopia&apos;s leading job boards. Search by role, filter by sector, and find your next career step.
            </p>
          </div>
        </div>
      </div>

      {/* Main Content Area */}
      <main className="mx-auto w-full max-w-6xl px-margin-mobile py-8 md:px-margin-desktop md:py-10">
        {/* Minimal Filters Bar: Search + Sector + Location + Sort */}
        <div className="flex flex-col gap-3 rounded-2xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm md:flex-row md:items-center">
          {/* Search Input */}
          <div className="relative flex-1">
            <label htmlFor={searchInputId} className="sr-only">
              Search jobs
            </label>
            <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-on-surface-variant">
              search
            </span>
            <input
              id={searchInputId}
              type="search"
              value={searchInput}
              onChange={(e) => {
                setSearchInput(e.target.value)
                setPage(1)
              }}
              placeholder="Search job titles, companies, or keywords…"
              className="w-full rounded-xl border border-outline-variant bg-surface-container-lowest py-2.5 pl-11 pr-4 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {/* Sector Selector */}
            <label htmlFor={sectorSelectId} className="sr-only">
              Filter by Sector
            </label>
            <select
              id={sectorSelectId}
              value={sectorId}
              onChange={(e) => {
                setSectorId(e.target.value)
                setPage(1)
              }}
              aria-label="Filter by Sector"
              className="rounded-xl border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">All Sectors</option>
              {!sectorsLoading &&
                sortedSectors.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name} ({s.jobCount ?? 0})
                  </option>
                ))}
            </select>

            {/* Location Selector */}
            <label htmlFor={locationSelectId} className="sr-only">
              Filter by Location
            </label>
            <select
              id={locationSelectId}
              value={location}
              onChange={(e) => {
                setLocation(e.target.value)
                setPage(1)
              }}
              aria-label="Filter by Location"
              className="rounded-xl border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">All Locations</option>
              {!locationsLoading &&
                locations.map((loc) => (
                  <option key={loc} value={loc}>
                    {loc}
                  </option>
                ))}
            </select>

            {/* Sort Selector */}
            <label htmlFor={sortSelectId} className="sr-only">
              Sort jobs
            </label>
            <select
              id={sortSelectId}
              value={sort}
              onChange={(e) => {
                setSort(e.target.value)
                setPage(1)
              }}
              aria-label="Sort jobs"
              className="rounded-xl border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="newest">Newest First</option>
              <option value="oldest">Oldest First</option>
              <option value="deadline">Closing Soonest</option>
            </select>

            {/* Clear Filters */}
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="flex items-center gap-1 rounded-lg px-2.5 py-2 font-label-md text-label-md font-medium text-secondary transition-colors hover:bg-surface-container-low hover:text-on-surface"
              >
                <span className="material-symbols-outlined text-[16px]">close</span>
                <span>Reset</span>
              </button>
            )}
          </div>
        </div>

        {/* Active Filter Chips */}
        {hasActiveFilters && (
          <div className="mt-4 flex flex-wrap items-center gap-2">
            <span className="font-label-sm text-label-sm text-secondary">
              Active filters:
            </span>
            {searchInput.trim() && (
              <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 font-label-sm text-label-sm font-medium text-primary">
                Search: &quot;{searchInput}&quot;
                <button
                  type="button"
                  onClick={() => setSearchInput('')}
                  className="hover:opacity-75"
                >
                  <span className="material-symbols-outlined text-[14px]">close</span>
                </button>
              </span>
            )}
            {selectedSectorName && (
              <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 font-label-sm text-label-sm font-medium text-primary">
                Sector: {selectedSectorName}
                <button
                  type="button"
                  onClick={() => setSectorId('')}
                  className="hover:opacity-75"
                >
                  <span className="material-symbols-outlined text-[14px]">close</span>
                </button>
              </span>
            )}
            {location && (
              <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 font-label-sm text-label-sm font-medium text-primary">
                Location: {location}
                <button
                  type="button"
                  onClick={() => setLocation('')}
                  className="hover:opacity-75"
                >
                  <span className="material-symbols-outlined text-[14px]">close</span>
                </button>
              </span>
            )}
          </div>
        )}

        {/* Results Bar */}
        <div className="mt-6 flex items-center justify-between">
          <p className="font-label-md text-label-md text-secondary">
            {jobsLoading
              ? 'Loading live vacancies…'
              : pagination
                ? `Showing ${jobs.length} of ${pagination.totalCount} vacancies`
                : `${jobs.length} vacancies found`}
          </p>
        </div>

        {/* Error state */}
        {error && (
          <div className="mt-6 rounded-xl border border-error/30 bg-error-container p-4 font-body-md text-body-md text-on-error-container">
            Could not load jobs right now. Please refresh the page or try again in a few moments.
          </div>
        )}

        {/* Jobs Feed Grid */}
        {jobsLoading ? (
          <div className="mt-6 grid grid-cols-1 gap-6 md:grid-cols-2">
            {[1, 2, 3, 4, 5, 6].map((i) => (
              <div
                key={i}
                className="flex h-64 animate-pulse flex-col rounded-xl border border-surface-variant bg-surface-container-lowest p-6"
              >
                <div className="flex gap-4">
                  <div className="h-12 w-24 rounded bg-surface-container-low" />
                  <div className="flex-1 space-y-2">
                    <div className="h-5 w-3/4 rounded bg-surface-container-low" />
                    <div className="h-4 w-1/2 rounded bg-surface-container-low" />
                  </div>
                </div>
                <div className="mt-8 grid grid-cols-2 gap-3">
                  <div className="h-4 rounded bg-surface-container-low" />
                  <div className="h-4 rounded bg-surface-container-low" />
                  <div className="h-4 rounded bg-surface-container-low" />
                  <div className="h-4 rounded bg-surface-container-low" />
                </div>
              </div>
            ))}
          </div>
        ) : jobs.length === 0 ? (
          <div className="mt-8 flex flex-col items-center justify-center rounded-2xl border border-dashed border-surface-variant p-12 text-center">
            <span className="material-symbols-outlined text-[48px] text-outline">
              search_off
            </span>
            <h3 className="mt-3 font-headline-md text-headline-md text-on-surface">
              No matching vacancies found
            </h3>
            <p className="mt-1 max-w-md font-body-sm text-body-sm text-secondary">
              Try adjusting your search terms or clearing the sector and location filters to see more opportunities.
            </p>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="mt-5 rounded-lg border border-surface-variant px-5 py-2 font-label-md text-label-md font-semibold text-primary hover:bg-surface-container-low"
              >
                Clear All Filters
              </button>
            )}
          </div>
        ) : (
          <div className="mt-6 grid grid-cols-1 gap-5 xl:grid-cols-2">
            {jobs.map((job) => (
              <JobCard
                key={job.id}
                job={job}
                saved={savedIds.has(job.id)}
                detailPath={`/jobs/${job.id}`}
                onApply={(j) => {
                  setModalJob(j)
                  setModalMode('apply')
                }}
                onToggleSave={() => handleToggleSave(job)}
              />
            ))}
          </div>
        )}

        {/* Pagination Bar */}
        {pagination && pagination.totalPages > 1 && (
          <div className="mt-10 flex items-center justify-between border-t border-surface-variant pt-6">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="inline-flex items-center gap-1 rounded-lg border border-surface-variant px-4 py-2 font-label-md text-label-md font-medium text-primary transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40"
            >
              <span className="material-symbols-outlined text-[18px]">chevron_left</span>
              <span>Previous</span>
            </button>
            <span className="font-label-md text-label-md text-secondary">
              Page {pagination.page} of {pagination.totalPages}
            </span>
            <button
              type="button"
              disabled={page >= pagination.totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="inline-flex items-center gap-1 rounded-lg border border-surface-variant px-4 py-2 font-label-md text-label-md font-medium text-primary transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40"
            >
              <span>Next</span>
              <span className="material-symbols-outlined text-[18px]">chevron_right</span>
            </button>
          </div>
        )}
      </main>

      {/* Guest Auth / Preview Modal */}
      <GuestJobAuthModal
        isOpen={modalJob !== null}
        onClose={() => setModalJob(null)}
        job={modalJob}
        mode={modalMode}
      />
    </div>
  )
}
