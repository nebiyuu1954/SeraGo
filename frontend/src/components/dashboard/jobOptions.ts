import type { JobStatus, JobType, WorkMode } from '../../types'

/** Display labels for every job type (lowerCamel API values). */
export const JOB_TYPE_LABELS: Record<JobType, string> = {
  fullTime: 'Full-time',
  partTime: 'Part-time',
  contract: 'Contract',
  contractual: 'Contractual',
  internship: 'Internship',
  freelance: 'Freelance',
  temporary: 'Temporary',
  other: 'Other',
}

/** Selectable job types (no blank "any" option — the form adds its own). */
export const FORM_JOB_TYPE_OPTIONS: JobType[] = [
  'fullTime',
  'partTime',
  'contract',
  'contractual',
  'internship',
  'freelance',
  'temporary',
  'other',
]

/** Display labels for work arrangements (where the work happens). */
export const WORK_ARRANGEMENT_LABELS: Record<WorkMode, string> = {
  onsite: 'Onsite',
  remote: 'Remote',
  hybrid: 'Hybrid',
}

/** Selectable work arrangements for the recruiter form. */
export const FORM_WORK_ARRANGEMENT_OPTIONS: WorkMode[] = [
  'onsite',
  'remote',
  'hybrid',
]

/** Filter dropdown options — blank value means "all arrangements". */
export const WORK_ARRANGEMENT_OPTIONS: { value: WorkMode | ''; label: string }[] = [
  { value: '', label: 'All arrangements' },
  ...FORM_WORK_ARRANGEMENT_OPTIONS.map((value) => ({
    value,
    label: WORK_ARRANGEMENT_LABELS[value],
  })),
]

/** Filter dropdown options — blank value means "all types". */
export const JOB_TYPE_OPTIONS: { value: JobType | ''; label: string }[] = [
  { value: '', label: 'All types' },
  ...FORM_JOB_TYPE_OPTIONS.map((value) => ({
    value,
    label: JOB_TYPE_LABELS[value],
  })),
]

/** Status filter options — blank value means "all statuses". */
export const STATUS_OPTIONS: { value: JobStatus | ''; label: string }[] = [
  { value: '', label: 'All statuses' },
  { value: 'draft', label: 'Draft' },
  { value: 'pendingApproval', label: 'Pending review' },
  { value: 'published', label: 'Published' },
  { value: 'rejected', label: 'Rejected' },
]

/** Sort options for job listings — values match the API's JobListQuery.Sort. */
export const SORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'newest', label: 'Newest first' },
  { value: 'oldest', label: 'Oldest first' },
  { value: 'title_asc', label: 'Title A–Z' },
  { value: 'title_desc', label: 'Title Z–A' },
  { value: 'deadline', label: 'Closing soon' },
]

/** Source-website filter — values match the API's Job.SourceName display names. */
export const SOURCE_OPTIONS: { value: string; label: string }[] = [
  { value: 'Afriwork', label: 'Afriwork' },
  { value: 'EthioJobs', label: 'EthioJobs' },
  { value: 'GeezJobs', label: 'GeezJobs' },
  { value: 'HaHuJobs', label: 'HaHuJobs' },
  { value: 'Ethiopian Reporter Jobs', label: 'Ethiopian Reporter Jobs' },
  { value: 'SeraGo', label: 'SeraGo' },
]

/** Per-page options for job listings — values match the API's MaxPageSize (50). */
export const PAGE_SIZE_OPTIONS = [10, 15, 20, 30, 40, 50]

/** Experience-level filter options — values match the sync's normalized bands. */
export const EXPERIENCE_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'All levels' },
  { value: 'Entry', label: 'Entry' },
  { value: 'Junior', label: 'Junior' },
  { value: 'Mid', label: 'Mid' },
  { value: 'Senior', label: 'Senior' },
]

/** Posted-within filter — value is the number of days ('' = any time). */
export const POSTED_WITHIN_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'Any time' },
  { value: '1', label: 'Today' },
  { value: '7', label: 'Last 7 days' },
  { value: '14', label: 'Last 14 days' },
  { value: '30', label: 'Last 30 days' },
]

/** Closing-within filter — value is the number of days ('' = any deadline). */
export const CLOSING_WITHIN_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'Any deadline' },
  { value: '7', label: 'Next 7 days' },
  { value: '14', label: 'Next 14 days' },
  { value: '30', label: 'Next 30 days' },
]

/** Salary currency options for the recruiter form. */
export const SALARY_CURRENCY_OPTIONS: { value: string; label: string }[] = [
  { value: 'ETB', label: 'ETB (Ethiopian Birr)' },
  { value: 'USD', label: 'USD (US Dollar)' },
]

/** Salary period options for the recruiter form. */
export const SALARY_PERIOD_OPTIONS: { value: string; label: string }[] = [
  { value: 'monthly', label: 'Monthly' },
  { value: 'annual', label: 'Annual' },
  { value: 'fixed', label: 'Fixed amount' },
]

/** Experience year options for recruiter form (min/max years). */
export const EXPERIENCE_YEAR_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'Not specified' },
  { value: '0', label: '0 years (Entry)' },
  { value: '1', label: '1 year' },
  { value: '2', label: '2 years' },
  { value: '3', label: '3 years' },
  { value: '4', label: '4 years' },
  { value: '5', label: '5 years' },
  { value: '7', label: '7 years' },
  { value: '10', label: '10 years' },
  { value: '15', label: '15+ years' },
]
