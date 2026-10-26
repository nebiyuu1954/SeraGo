import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { cn } from '../../../lib/cn.ts'
import { useSectorsQuery, useJobsQuery, useSavedJobsQuery } from '../../../hooks/query.ts'
import { useAuthUser } from '../../../hooks'
import { saveJob, unsaveJob, getStoredAuthTokens } from '../../../api'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import GuestJobAuthModal from '../../../components/jobs/GuestJobAuthModal.tsx'
import JobCard from '../../../components/jobs/JobCard.tsx'
import type { JobResponse } from '../../../types'

export default function JobListings() {
  const navigate = useNavigate()
  const auth = useAuthUser()
  const { savedIds, refresh: refreshSaved } = useSavedJobsQuery(auth.status === 'authenticated')
  const { showToast } = useToast()

  const [selectedSectorId, setSelectedSectorId] = useState<string | null>(null)
  const [modalJob, setModalJob] = useState<JobResponse | null>(null)
  const [modalMode, setModalMode] = useState<'apply' | 'preview'>('preview')

  const { sectors, isLoading: sectorsLoading } = useSectorsQuery()
  const { jobs, isLoading: jobsLoading } = useJobsQuery({
    page: 1,
    pageSize: 6,
    sort: 'newest',
    sectorId: selectedSectorId || undefined,
  })

  // Calculate live total jobs across all active sectors
  const totalJobsCount = sectors.reduce((sum, s) => sum + (s.jobCount ?? 0), 0)

  // Sort sectors from highest job count to lowest
  const sortedSectors = [...sectors].sort(
    (a, b) => (b.jobCount ?? 0) - (a.jobCount ?? 0) || a.name.localeCompare(b.name),
  )

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


  return (
    <section className="bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile py-12 md:px-margin-desktop md:py-16">
        {/* Section header */}
        <div className="mb-12">
          <h2 className="mb-4 font-display-lg-mobile text-display-lg-mobile text-on-surface md:font-display-lg md:text-display-lg">
            Discover Your <span className="text-primary">Next Role</span>
          </h2>
          <p className="max-w-2xl font-body-lg text-body-lg text-secondary">
            Explore live opportunities verified across leading job boards. Filter by sector or browse the latest openings.
          </p>
        </div>

        <div className="flex flex-col gap-gutter md:flex-row">
          {/* Mobile: Sectors dropdown */}
          <div className="w-full md:hidden mb-4">
            <label htmlFor="landing-mobile-sector-select" className="mb-2 block font-label-md text-label-md font-semibold text-primary">
              Filter by Sector
            </label>
            <div className="relative">
              <select
                id="landing-mobile-sector-select"
                value={selectedSectorId ?? ''}
                onChange={(e) => setSelectedSectorId(e.target.value ? e.target.value : null)}
                className="w-full appearance-none rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 pr-10 font-body-md text-body-md text-on-surface shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary cursor-pointer"
              >
                <option value="">All Sectors ({totalJobsCount})</option>
                {sortedSectors.map((sector) => (
                  <option key={sector.id} value={sector.id}>
                    {sector.name} ({sector.jobCount ?? 0})
                  </option>
                ))}
              </select>
              <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-on-surface-variant">
                arrow_drop_down
              </span>
            </div>
          </div>

          {/* Desktop Left: Sectors sidebar (30%) */}
          <aside className="hidden w-full shrink-0 md:block md:w-[30%]">
            <h3 className="mb-6 border-b border-surface-variant pb-2 font-headline-md text-headline-md text-primary">
              Sectors
            </h3>

            {sectorsLoading ? (
              <div className="flex flex-col gap-2">
                {[1, 2, 3, 4, 5].map((i) => (
                  <div
                    key={i}
                    className="h-9 w-full animate-pulse rounded bg-surface-container-low"
                  />
                ))}
              </div>
            ) : (
              <ul className="flex flex-col gap-1.5 font-body-md text-body-md">
                <li>
                  <button
                    type="button"
                    onClick={() => setSelectedSectorId(null)}
                    className={cn(
                      'flex w-full items-center justify-between rounded-lg py-2.5 pl-4 pr-4 text-left transition-colors',
                      selectedSectorId === null
                        ? 'bg-surface-container-low font-semibold text-primary'
                        : 'text-secondary hover:bg-surface-container-low hover:text-primary',
                    )}
                  >
                    <span>All Sectors</span>
                    <span
                      className={cn(
                        'text-sm font-medium',
                        selectedSectorId === null
                          ? 'text-primary'
                          : 'text-outline',
                      )}
                    >
                      {totalJobsCount}
                    </span>
                  </button>
                </li>
                {sortedSectors.map((sector) => {
                  const isActive = selectedSectorId === sector.id
                  return (
                    <li key={sector.id}>
                      <button
                        type="button"
                        onClick={() => setSelectedSectorId(isActive ? null : sector.id)}
                        className={cn(
                          'flex w-full items-center justify-between rounded-lg py-2.5 pl-4 pr-4 text-left transition-colors',
                          isActive
                            ? 'bg-surface-container-low font-semibold text-primary'
                            : 'text-secondary hover:bg-surface-container-low hover:text-primary',
                        )}
                      >
                        <span className="truncate pr-2">{sector.name}</span>
                        <span
                          className={cn(
                            'text-sm',
                            isActive ? 'font-medium text-primary' : 'text-outline',
                          )}
                        >
                          {sector.jobCount ?? 0}
                        </span>
                      </button>
                    </li>
                  )
                })}
              </ul>
            )}
          </aside>

          {/* Right: Job feed (70%) */}
          <div className="flex w-full flex-col gap-8 md:w-[70%]">
            {jobsLoading ? (
              <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
                {[1, 2, 3, 4, 5, 6].map((i) => (
                  <div
                    key={i}
                    className="flex h-56 animate-pulse flex-col rounded-lg border border-surface-variant bg-surface-container-lowest p-4"
                  >
                    <div className="flex gap-3">
                      <div className="h-10 w-20 rounded bg-surface-container-low" />
                      <div className="flex-1 space-y-1.5">
                        <div className="h-4 w-3/4 rounded bg-surface-container-low" />
                        <div className="h-3.5 w-1/2 rounded bg-surface-container-low" />
                      </div>
                    </div>
                    <div className="mt-6 grid grid-cols-2 gap-2">
                      <div className="h-3.5 rounded bg-surface-container-low" />
                      <div className="h-3.5 rounded bg-surface-container-low" />
                      <div className="h-3.5 rounded bg-surface-container-low" />
                      <div className="h-3.5 rounded bg-surface-container-low" />
                    </div>
                  </div>
                ))}
              </div>
            ) : jobs.length === 0 ? (
              <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-surface-variant p-12 text-center">
                <span className="material-symbols-outlined text-[48px] text-outline">
                  work_off
                </span>
                <p className="mt-3 font-headline-md text-headline-md text-on-surface">
                  No live jobs in this sector yet
                </p>
                <p className="mt-1 font-body-sm text-body-sm text-secondary">
                  Try viewing All Sectors to explore other verified opportunities.
                </p>
                <button
                  type="button"
                  onClick={() => setSelectedSectorId(null)}
                  className="mt-5 rounded-lg border border-surface-variant px-5 py-2 font-label-md text-label-md font-medium text-primary hover:bg-surface-container-low"
                >
                  View All Sectors
                </button>
              </div>
            ) : (
              <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
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

            <div className="mt-2 flex justify-center">
              <Link
                to={selectedSectorId ? `/jobs?sectorId=${selectedSectorId}` : '/jobs'}
                className="inline-flex items-center gap-2 rounded-lg border border-surface-variant px-8 py-3 font-label-md text-label-md font-semibold text-primary transition-colors duration-200 hover:bg-surface-container-low"
              >
                <span>View all jobs</span>
                <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
              </Link>
            </div>
          </div>
        </div>
      </div>

      {/* Guest Auth / Preview Modal */}
      <GuestJobAuthModal
        isOpen={modalJob !== null}
        onClose={() => setModalJob(null)}
        job={modalJob}
        mode={modalMode}
      />
    </section>
  )
}
