import { cn } from '../../lib/cn.ts'
import afriworkLogo from '../../assets/logos/afriwork.png'
import ethiojobsLogo from '../../assets/logos/ethiojobs.png'
import ethioreporterjobsLogo from '../../assets/logos/ethioreporterjobs.png'
import geezjobsLogo from '../../assets/logos/geezjobs.png'

interface Job {
  title: string
  team: string
  logo: string
  /** Six metadata rows: icon (Material Symbol) + label. */
  meta: { icon: string; label: string }[]
}

const jobs: Job[] = [
  {
    title: 'Senior Protocol Engineer',
    team: 'Core Infrastructure Team',
    logo: afriworkLogo,
    meta: [
      { icon: 'location_on', label: 'San Francisco, CA' },
      { icon: 'payments', label: '$160k - $210k' },
      { icon: 'schedule', label: 'Full-time' },
      { icon: 'work', label: '5+ years' },
      { icon: 'domain', label: 'Engineering' },
      { icon: 'home_work', label: 'Hybrid' },
    ],
  },
  {
    title: 'Product Designer, Systems',
    team: 'Design & Experience',
    logo: ethiojobsLogo,
    meta: [
      { icon: 'location_on', label: 'Remote (US)' },
      { icon: 'payments', label: '$130k - $175k' },
      { icon: 'schedule', label: 'Full-time' },
      { icon: 'work', label: '4+ years' },
      { icon: 'domain', label: 'Design' },
      { icon: 'home_work', label: 'Remote' },
    ],
  },
  {
    title: 'Machine Learning Researcher',
    team: 'Advanced AI Lab',
    logo: ethioreporterjobsLogo,
    meta: [
      { icon: 'location_on', label: 'New York, NY' },
      { icon: 'payments', label: '$180k - $240k' },
      { icon: 'schedule', label: 'Full-time' },
      { icon: 'work', label: '3+ years' },
      { icon: 'domain', label: 'Research' },
      { icon: 'home_work', label: 'On-site' },
    ],
  },
  {
    title: 'Developer Advocate',
    team: 'Community & Growth',
    logo: geezjobsLogo,
    meta: [
      { icon: 'location_on', label: 'London, UK' },
      { icon: 'payments', label: '£90k - £120k' },
      { icon: 'schedule', label: 'Full-time' },
      { icon: 'work', label: '4+ years' },
      { icon: 'domain', label: 'Marketing' },
      { icon: 'home_work', label: 'Hybrid' },
    ],
  },
]

const sectors = [
  { name: 'Technology', count: 42, active: true },
  { name: 'Engineering', count: 18 },
  { name: 'Sales', count: 24 },
  { name: 'Marketing', count: 12 },
  { name: 'Finance', count: 8 },
  { name: 'Healthcare', count: 5 },
  { name: 'Education', count: 3 },
]

export default function JobListings() {
  return (
    <section className="bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile py-12 md:px-margin-desktop md:py-16">
        {/* Section header */}
        <div className="mb-12">
          <h2 className="mb-4 font-display-lg-mobile text-display-lg-mobile text-on-surface md:font-display-lg md:text-display-lg">
            Discover Your <span className="text-primary">Next Role</span>
          </h2>
          <p className="max-w-2xl font-body-lg text-body-lg text-secondary">
            Explore opportunities that shape the future of connectivity. Find
            where your skills meet our vision.
          </p>
        </div>

        <div className="flex flex-col gap-gutter md:flex-row">
          {/* Left: Sectors sidebar (30%) */}
          <aside className="w-full shrink-0 md:w-[30%]">
            <h3 className="mb-6 border-b border-surface-variant pb-2 font-headline-md text-headline-md text-primary">
              Sectors
            </h3>
            <ul className="flex flex-col gap-2 font-body-md text-body-md">
              {sectors.map((sector) => (
                <li key={sector.name}>
                  <a
                    href="#"
                    className={cn(
                      'block rounded py-2 pl-4 pr-4 transition-colors',
                      sector.active
                        ? 'bg-surface-container-low font-medium text-primary'
                        : 'text-secondary hover:bg-surface-container-low hover:text-primary',
                    )}
                  >
                    {sector.name}
                    <span
                      className={cn(
                        'float-right text-sm',
                        sector.active ? 'text-secondary' : 'text-outline',
                      )}
                    >
                      {sector.count}
                    </span>
                  </a>
                </li>
              ))}
            </ul>
          </aside>

          {/* Right: Job feed (70%) */}
          <div className="flex w-full flex-col gap-8 md:w-[70%]">
            <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
              {jobs.map((job) => (
                <article
                  key={job.title}
                  className="group flex flex-col overflow-hidden rounded-lg border border-surface-variant bg-surface-container-lowest transition-all duration-300 hover:border-outline-variant hover:shadow-sm"
                >
                  <div className="flex flex-grow flex-row items-center gap-4 border-b border-surface-variant/50 p-6">
                    {/* Wide brand logos get a wider slot, sized by height */}
                    <div className="flex h-12 w-28 shrink-0 items-center justify-center rounded bg-surface-container-low px-2">
                      <img
                        className="max-h-8 w-auto max-w-full object-contain"
                        src={job.logo}
                        alt=""
                      />
                    </div>
                    <div>
                      <h4 className="mb-1 font-headline-md text-headline-md leading-tight text-on-surface transition-colors group-hover:text-surface-tint">
                        {job.title}
                      </h4>
                      <p className="font-body-md text-body-md text-secondary">
                        {job.team}
                      </p>
                    </div>
                  </div>
                  <div className="flex flex-col gap-5 bg-surface/30 p-6">
                    <div className="grid grid-cols-2 gap-3 font-label-md text-label-md text-on-surface-variant">
                      {job.meta.map((item) => (
                        <div
                          key={item.label}
                          className="flex items-center gap-2"
                        >
                          <span className="material-symbols-outlined text-[18px]">
                            {item.icon}
                          </span>
                          {item.label}
                        </div>
                      ))}
                    </div>
                    <div className="mt-auto flex gap-2">
                      <button
                        type="button"
                        className="flex-1 rounded bg-accent px-4 py-2.5 font-label-md text-label-md text-on-accent transition-colors hover:opacity-90"
                      >
                        Apply Now
                      </button>
                      <button
                        type="button"
                        className="flex-1 rounded border border-surface-variant px-4 py-2.5 font-label-md text-label-md text-primary transition-colors hover:bg-surface-container-low"
                      >
                        See description
                      </button>
                    </div>
                  </div>
                </article>
              ))}
            </div>

            <div className="mt-2 flex justify-center">
              <button
                type="button"
                className="rounded border border-surface-variant px-8 py-3 font-label-md text-label-md text-primary transition-colors duration-200 hover:bg-surface-container-low"
              >
                View all jobs
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
