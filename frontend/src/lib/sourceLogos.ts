import afriworkLogo from '../assets/logos/afriwork.png'
import ethiojobsLogo from '../assets/logos/ethiojobs.png'
import ethioreporterjobsLogo from '../assets/logos/ethioreporterjobs.png'
import geezjobsLogo from '../assets/logos/geezjobs.png'
import hahuLogo from '../assets/logos/hahu.png'
import seragoLogo from '../assets/logos/serago.svg'

/**
 * Source website → its brand logo (the same PNGs the landing page shows).
 * Keys are normalized (lowercase, spaces stripped) so "Ethiopian Reporter
 * Jobs", "Ethio Reporter Jobs", "ethiojobs" etc. all resolve.
 */
const SOURCE_LOGOS: Record<string, string> = {
  afriwork: afriworkLogo,
  ethiojobs: ethiojobsLogo,
  ethioreporterjobs: ethioreporterjobsLogo,
  ethiopianreporterjobs: ethioreporterjobsLogo,
  geezjobs: geezjobsLogo,
  hahu: hahuLogo,
  hahujobs: hahuLogo,
  serago: seragoLogo,
}

/**
 * The job board's brand logo for a job's source website, or null when the
 * source isn't one of the known boards (or the job has no source).
 */
export function sourceLogo(
  sourceName: string | null | undefined,
): string | null {
  if (!sourceName) return null
  return SOURCE_LOGOS[sourceName.toLowerCase().replace(/\s+/g, '')] ?? null
}
