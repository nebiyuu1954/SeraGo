import type { JobStatus, JobType } from '../../types'

/** Display labels for every job type (lowerCamel API values). */
export const JOB_TYPE_LABELS: Record<JobType, string> = {
  fullTime: 'Full-time',
  partTime: 'Part-time',
  contract: 'Contract',
  contractual: 'Contractual',
  remote: 'Remote',
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
  'remote',
  'internship',
  'freelance',
  'temporary',
  'other',
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
