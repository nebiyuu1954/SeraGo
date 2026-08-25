import type { JobResponse } from '../../types'
import AfriworkDescription from './AfriworkDescription.tsx'
import EthioJobsDescription from './EthioJobsDescription.tsx'
import HaHuJobsDescription from './HaHuJobsDescription.tsx'
import GeezJobsDescription from './GeezJobsDescription.tsx'
import ReporterJobsDescription from './ReporterJobsDescription.tsx'
import GenericDescription from './GenericDescription.tsx'

/**
 * Dispatcher that renders the correct source-specific description component
 * based on the job's sourceName. Each source has its own component that knows
 * how to render that source's unique data (skills, sectors, compensation,
 * application methods, employment text, etc.).
 *
 * Unknown sources and SeraGo-posted jobs fall back to GenericDescription.
 */
export default function SourceJobDescription({
  job,
}: {
  job: JobResponse
}) {
  const source = (job.sourceName ?? '').toLowerCase()

  if (source.includes('afriwork')) {
    return <AfriworkDescription job={job} />
  }
  if (source.includes('ethiojobs')) {
    return <EthioJobsDescription job={job} />
  }
  if (source.includes('hahu')) {
    return <HaHuJobsDescription job={job} />
  }
  if (source.includes('geez')) {
    return <GeezJobsDescription job={job} />
  }
  if (source.includes('reporter')) {
    return <ReporterJobsDescription job={job} />
  }

  return <GenericDescription job={job} />
}
