import { useState, useId } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import {
  useJobsQuery,
  useSectorsQuery,
  useJobLocationsQuery,
} from '../../../hooks/query.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'
import seragoLogo from '../../../assets/logos/serago.svg'
import GuestJobAuthModal from '../../../components/jobs/GuestJobAuthModal.tsx'
import type { JobResponse } from '../../../types'

export default function PublicJobsPage() {
  const [searchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(searchParams.get('q') || '')
  const [sectorId, setSectorId] = useState(searchParams.get('sectorId') || '')
  const [location, setLocation] = useState(searchParams.get('location') || '')
  const [sort, setSort] = useState(searchParams.get('sort') || 'newest')
  const [page, setPage] = useState(1)
  const pageSize = 10

  const searchInputId = useId()
  const sectorSelectId = useId()
  const locationSelectId = useId()
  const sortSelectId = useId()

  const [modalJob, setModalJob] = useState<JobResponse | null>(null)
  const [modalMode, setModalMode] = useState<'apply' | 'preview'>('preview')

  const { sectors, isLoading: sectorsLoading } = useSectorsQuery()
  const { locations, isLoading: locationsLoading } = useJobLocationsQuery()

  const { jobs, pagination, isLoading: jobsLoading, error } = useJobsQuery({
    q: searchInput.trim() || undefined,
    sectorId: sectorId || undefined,
    location: location || undefined,
    sort,
    page,
    pageSize,
  })

  const hasActiveFilters =
    Boolean(searchInput.trim()) || Boolean(sectorId) || Boolean(location)

  const clearFilters = () => {
    setSearchInput('')
    setSectorId('')
    setLocation('')
    setPage(1)
  }

  const formatEnum = (val?: string) => {
    if (!val) return 'Not Specified'
    return val
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim()
  }

  const formatDate = (isoString?: string) => {
    if (!isoString) return ''
    try {
      const d = new Date(isoString)
      return d.toLocaleDateString('en-GB', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      })
    } catch {
      return ''
    }
  }

  const selectedSectorName = sectors.find((s) => s.id === sectorId)?.name

  return (
    <div className="min-h-screen bg-surface">
      {/* Hero Header */}
      <div className="border-b border-surface-variant/40 bg-surface-container-lowest py-10 md:py-14">
        <div className="mx-auto w-full max-w-container-max px-margin-mobile md:px-margin-desktop">
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

          {/* AI Teaser Banner */}
          <div className="mt-8 flex flex-col items-start justify-between gap-4 rounded-2xl border border-primary/20 bg-gradient-to-r from-primary/10 via-surface-container-low to-accent/10 p-5 shadow-sm sm:flex-row sm:items-center sm:p-6">
            <div className="flex items-start gap-3.5">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary text-on-primary shadow-sm">
                <span className="material-symbols-outlined text-[22px]">psychology</span>
              </div>
              <div>
                <h4 className="font-headline-md text-headline-md text-on-surface">
                  Want automatic AI compatibility scoring?
                </h4>
                <p className="font-body-sm text-body-sm text-secondary">
                  Create a free profile with your CV to see instant match percentages and skill gaps on every job.
                </p>
              </div>
            </div>
            <Link
              to="/signup"
              className="inline-flex shrink-0 items-center gap-1.5 rounded-lg bg-primary px-5 py-2.5 font-label-md text-label-md font-semibold text-on-primary transition-all hover:bg-primary/90 hover:shadow"
            >
              <span>Unlock AI Matches</span>
              <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Main Content Area */}
      <main className="mx-auto w-full max-w-container-max px-margin-mobile py-8 md:px-margin-desktop md:py-10">
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
                sectors.map((s) => (
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
          <div className="mt-6 grid grid-cols-1 gap-6 md:grid-cols-2">
            {jobs.map((job) => {
              const logo =
                job.companyLogoUrl ||
                sourceLogo(job.sourceName) ||
                seragoLogo

              const metaItems = [
                { icon: 'location_on', label: job.location || 'Addis Ababa' },
                { icon: 'payments', label: job.salary || 'Competitive' },
                { icon: 'schedule', label: formatEnum(job.jobType) },
                {
                  icon: 'work',
                  label:
                    job.experienceLevel ||
                    (job.experienceMinYears
                      ? `${job.experienceMinYears}+ years`
                      : 'Experienced'),
                },
                { icon: 'domain', label: job.sectorName || 'General' },
                { icon: 'home_work', label: formatEnum(job.workMode) },
              ]

              return (
                <article
                  key={job.id}
                  className="group flex flex-col overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm transition-all duration-300 hover:border-outline-variant hover:shadow-md"
                >
                  {/* Top Bar: Logo + Title + Company */}
                  <div className="flex flex-grow flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
                    <div className="flex h-12 w-28 shrink-0 items-center justify-center rounded-lg bg-surface-container-low px-2">
                      <img
                        className="max-h-8 w-auto max-w-full object-contain"
                        src={logo}
                        alt=""
                      />
                    </div>
                    <div className="min-w-0 flex-1">
                      <h3 className="mb-1 truncate font-headline-md text-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint">
                        {job.title}
                      </h3>
                      <p className="truncate font-body-md text-body-md text-secondary">
                        {job.company || 'Confidential'}
                      </p>
                    </div>
                  </div>

                  {/* Body: Meta chips + Deadline */}
                  <div className="flex flex-col gap-5 bg-surface/30 p-6">
                    <div className="grid grid-cols-2 gap-3 font-label-md text-label-md text-on-surface-variant">
                      {metaItems.map((item) => (
                        <div
                          key={item.icon + item.label}
                          className="flex items-center gap-2 truncate"
                        >
                          <span className="material-symbols-outlined shrink-0 text-[18px]">
                            {item.icon}
                          </span>
                          <span className="truncate">{item.label}</span>
                        </div>
                      ))}
                    </div>

                    {job.deadline && (
                      <div className="flex items-center gap-1.5 font-label-sm text-label-sm text-secondary">
                        <span className="material-symbols-outlined text-[16px]">
                          hourglass_bottom
                        </span>
                        <span>Closes: {formatDate(job.deadline)}</span>
                      </div>
                    )}

                    {/* Actions */}
                    <div className="mt-auto flex gap-2.5 pt-1">
                      <button
                        type="button"
                        onClick={() => {
                          setModalJob(job)
                          setModalMode('apply')
                        }}
                        className="flex-1 rounded-lg bg-accent px-4 py-2.5 font-label-md text-label-md font-semibold text-on-accent transition-all hover:opacity-90"
                      >
                        Apply Now
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          setModalJob(job)
                          setModalMode('preview')
                        }}
                        className="flex-1 rounded-lg border border-surface-variant px-4 py-2.5 font-label-md text-label-md font-semibold text-primary transition-colors hover:bg-surface-container-low"
                      >
                        See description
                      </button>
                    </div>
                  </div>
                </article>
              )
            })}
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
