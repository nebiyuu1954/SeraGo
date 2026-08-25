/**
 * Source capabilities — what data each source website typically provides.
 *
 * Used by the job card and detail page to decide what to show/hide and
 * what fallback content to display when data is missing. The actual field
 * values on each job still drive rendering (a null salary hides the row
 * regardless of this registry), but this powers:
 * - Smart empty states ("Description available on GeezJobs →")
 * - Data completeness indicators
 * - Filter availability hints
 */

export interface SourceCapabilities {
  /** Does this source provide job descriptions? */
  hasDescription: boolean
  /** Does this source provide salary data? */
  hasSalary: boolean
  /** Does this source provide remote/hybrid/onsite work mode? */
  hasWorkMode: boolean
  /** Does this source provide experience level bands? */
  hasExperienceLevel: boolean
  /** Does this source provide sector/category data? */
  hasSector: boolean
  /** Does this source provide deadlines? */
  hasDeadline: boolean
  /** Does this source provide company logos? */
  hasCompanyLogo: boolean
  /** Does this source provide structured skill tags? */
  hasSkills: boolean
  /** Does this source provide application method details? */
  hasApplyMethods: boolean
}

const SOURCE_CAPABILITIES: Record<string, SourceCapabilities> = {
  Afriwork: {
    hasDescription: true,
    hasSalary: true,
    hasWorkMode: true,
    hasExperienceLevel: true,
    hasSector: true,
    hasDeadline: true,
    hasCompanyLogo: false,
    hasSkills: true,
    hasApplyMethods: false,
  },
  EthioJobs: {
    hasDescription: true,
    hasSalary: false,
    hasWorkMode: true,
    hasExperienceLevel: false,
    hasSector: true,
    hasDeadline: true,
    hasCompanyLogo: true,
    hasSkills: false,
    hasApplyMethods: true,
  },
  HaHuJobs: {
    hasDescription: true,
    hasSalary: true,
    hasWorkMode: false,
    hasExperienceLevel: true,
    hasSector: true,
    hasDeadline: true,
    hasCompanyLogo: true,
    hasSkills: false,
    hasApplyMethods: true,
  },
  GeezJobs: {
    hasDescription: false,
    hasSalary: false,
    hasWorkMode: false,
    hasExperienceLevel: true,
    hasSector: false,
    hasDeadline: true,
    hasCompanyLogo: true,
    hasSkills: false,
    hasApplyMethods: false,
  },
  'Ethiopian Reporter Jobs': {
    hasDescription: false,
    hasSalary: false,
    hasWorkMode: false,
    hasExperienceLevel: false,
    hasSector: false,
    hasDeadline: false,
    hasCompanyLogo: false,
    hasSkills: false,
    hasApplyMethods: false,
  },
}

/** Default capabilities for unknown sources (assume minimal data). */
const DEFAULT_CAPABILITIES: SourceCapabilities = {
  hasDescription: true,
  hasSalary: false,
  hasWorkMode: false,
  hasExperienceLevel: false,
  hasSector: false,
  hasDeadline: true,
  hasCompanyLogo: false,
  hasSkills: false,
  hasApplyMethods: false,
}

/**
 * Get the capabilities for a source website.
 * Returns defaults for unknown sources.
 */
export function getSourceCapabilities(
  sourceName: string | null | undefined,
): SourceCapabilities {
  if (!sourceName) return DEFAULT_CAPABILITIES
  return SOURCE_CAPABILITIES[sourceName] ?? DEFAULT_CAPABILITIES
}

/**
 * Compute a data completeness score (0–1) for a job based on what its
 * source typically provides vs what this specific job actually has.
 */
export function getDataCompleteness(
  job: {
    sourceName: string | null
    description: string
    salary: string
    workMode: string
    experienceLevel: string | null
    sectorName: string | null
    deadline: string | null
    companyLogoUrl: string | null
    skills: string | null
  },
): number {
  const caps = getSourceCapabilities(job.sourceName)
  let available = 0
  let present = 0

  if (caps.hasDescription) { available++; if (job.description) present++ }
  if (caps.hasSalary) { available++; if (job.salary) present++ }
  if (caps.hasWorkMode) { available++; if (job.workMode && job.workMode !== 'onsite') present++ }
  if (caps.hasExperienceLevel) { available++; if (job.experienceLevel) present++ }
  if (caps.hasSector) { available++; if (job.sectorName) present++ }
  if (caps.hasDeadline) { available++; if (job.deadline) present++ }
  if (caps.hasCompanyLogo) { available++; if (job.companyLogoUrl) present++ }
  if (caps.hasSkills) { available++; if (job.skills) present++ }

  return available === 0 ? 1 : present / available
}

/**
 * Parse a skills JSON string into an array of skill names.
 * Returns [] for null, empty, or malformed input.
 */
export function parseSkills(skills: string | null | undefined): string[] {
  if (!skills) return []
  try {
    const parsed = JSON.parse(skills)
    if (Array.isArray(parsed)) {
      return parsed.filter((s): s is string => typeof s === 'string' && s.length > 0)
    }
  } catch {
    // Not JSON — treat as comma-separated.
    return skills.split(',').map(s => s.trim()).filter(Boolean)
  }
  return []
}
