import { useState } from 'react'
import { Link } from 'react-router-dom'
import { cn } from '../../../lib/cn.ts'
import { useSectorsQuery, useJobsQuery } from '../../../hooks/query.ts'
import { sourceLogo } from '../../../lib/sourceLogos.ts'
import seragoLogo from '../../../assets/logos/serago.svg'
import GuestJobAuthModal from '../../../components/jobs/GuestJobAuthModal.tsx'
import type { JobResponse } from '../../../types'

export default function JobListings() {
  const [selectedSectorId, setSelectedSectorId] = useState<string | null>(null)
  const [modalJob, setModalJob] = useState<JobResponse | null>(null)
  const [modalMode, setModalMode] = useState<'apply' | 'preview'>('preview')

  const { sectors, isLoading: sectorsLoading } = useSectorsQuery()
  const { jobs, isLoading: jobsLoading } = useJobsQuery({
    page: 1,
    pageSize: 4,
    sort: 'newest',
    sectorId: selectedSectorId || undefined,
  })

  // Calculate live total jobs across all active sectors
  const totalJobsCount = sectors.reduce((sum, s) => sum + (s.jobCount ?? 0), 0)

  // Format enum values like 'fullTime' → 'Full Time'
  const formatEnum = (val?: string) => {
    if (!val) return 'Not Specified'
    return val
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim()
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
          {/* Left: Sectors sidebar (30%) */}
          <aside className="w-full shrink-0 md:w-[30%]">
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
                {sectors.map((sector) => {
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
              <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
                {[1, 2, 3, 4].map((i) => (
                  <div
                    key={i}
                    className="flex h-64 animate-pulse flex-col rounded-lg border border-surface-variant bg-surface-container-lowest p-6"
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
              <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
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
                      className="group flex flex-col overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest transition-all duration-300 hover:border-outline-variant hover:shadow-sm"
                    >
                      <div className="flex flex-grow flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
                        <div className="flex h-12 w-28 shrink-0 items-center justify-center rounded bg-surface-container-low px-2">
                          <img
                            className="max-h-8 w-auto max-w-full object-contain"
                            src={logo}
                            alt=""
                          />
                        </div>
                        <div className="min-w-0 flex-1">
                          <h4 className="mb-1 truncate font-headline-md text-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint">
                            {job.title}
                          </h4>
                          <p className="truncate font-body-md text-body-md text-secondary">
                            {job.company || 'Confidential'}
                          </p>
                        </div>
                      </div>
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
                        <div className="mt-auto flex gap-2">
                          <button
                            type="button"
                            onClick={() => {
                              setModalJob(job)
                              setModalMode('apply')
                            }}
                            className="flex-1 rounded-lg bg-accent px-4 py-2.5 font-label-md text-label-md font-semibold text-on-accent transition-colors hover:opacity-90"
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
