import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
  approveJob,
  fetchJob,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
  rejectJob,
  updateJob,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type {
  JobResponse,
  JobType,
  JobWriteRequest,
  SectorResponse,
  WorkMode,
} from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import {
  FORM_JOB_TYPE_OPTIONS,
  FORM_WORK_ARRANGEMENT_OPTIONS,
  JOB_TYPE_LABELS,
  WORK_ARRANGEMENT_LABELS,
  SALARY_CURRENCY_OPTIONS,
  SALARY_PERIOD_OPTIONS,
  EXPERIENCE_YEAR_OPTIONS,
} from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'
import RichTextEditor from '../../../components/ui/RichTextEditor.tsx'

const inputClass =
  'w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:cursor-not-allowed disabled:opacity-60'

const initialValues = {
  title: '',
  description: '',
  company: '',
  location: '',
  jobType: '' as JobType | '',
  workArrangement: '' as WorkMode | '',
  url: '',
  salary: '',
  salaryMin: '',
  salaryMax: '',
  salaryCurrency: '' as string | '',
  salaryPeriod: '' as string | '',
  experienceMinYears: '' as string | '',
  experienceMaxYears: '' as string | '',
  numberOfPositions: '1',
  deadline: '',
  sectorId: '' as string | '',
}

const validationSchema = object({
  title: string()
    .required('Title is required.')
    .max(500, 'Title must be 500 characters or fewer.'),
  company: string().max(255, 'Company must be 255 characters or fewer.'),
  location: string().max(255, 'Location must be 255 characters or fewer.'),
  url: string().test(
    'absolute-url',
    'Enter a valid URL (e.g. https://example.com).',
    (value) => !value || /^https?:\/\/[^\s]+$/i.test(value),
  ),
})

/** "2026-09-15T14:00:00Z" → local "2026-09-15T17:00" for datetime-local inputs. */
function toLocalDateTimeInput(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function Field({
  label,
  full,
  error,
  children,
}: {
  label: string
  full?: boolean
  error?: string
  children: React.ReactNode
}) {
  return (
    <div className={cn(full && 'md:col-span-2')}>
      <label className="font-label-sm text-label-sm font-medium text-on-surface">
        {label}
      </label>
      <div className="mt-1.5">{children}</div>
      {error && <p className="mt-1.5 font-label-sm text-label-sm text-error">{error}</p>}
    </div>
  )
}

/**
 * Admin-only job editor. Jobs are sourced from the scraper pipeline; admins
 * review, edit, publish/unpublish and moderate them here. Job creation is not
 * available — the scraper is the only source of new jobs.
 */
export default function JobFormPage() {
  const { jobId } = useParams<{ jobId: string }>()
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  const [loadedJob, setLoadedJob] = useState<JobResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [rejection, setRejection] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const [sectors, setSectors] = useState<SectorResponse[]>([])

  const formik = useFormik({
    initialValues,
    validationSchema,
    enableReinitialize: true,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: (values) => handleSubmit(values),
  })

  // Editing requires a job id — without one there is nothing to do here.
  useEffect(() => {
    if (!jobId) navigate('/dashboard/admin/jobs', { replace: true })
  }, [jobId, navigate])

  // Fetch available sectors for the picker
  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    fetchSectors(tokens.accessToken)
      .then(setSectors)
      .catch(() => {})
  }, [])

  // Auto-dismiss the success toast.
  useEffect(() => {
    if (!toast) return
    const t = setTimeout(() => setToast(null), 4000)
    return () => clearTimeout(t)
  }, [toast])

  // Load the job (prefill + status banners).
  useEffect(() => {
    if (!jobId || auth.status !== 'authenticated') return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    fetchJob(jobId, tokens.accessToken)
      .then((job) => {
        if (cancelled) return
        setLoadedJob(job)
        if (job.status === 'rejected') {
          setRejection(job.rejectionReason ?? 'No reason was provided.')
        }
        formik.setValues({
          title: job.title,
          description: job.description || '',
          company: job.company || '',
          location: job.location || '',
          jobType: (job.jobType as JobType) || 'other',
          workArrangement: (job.workMode as WorkMode) || '',
          url: job.url || '',
          salary: job.salary || '',
          salaryMin: job.salaryMin != null ? String(job.salaryMin) : '',
          salaryMax: job.salaryMax != null ? String(job.salaryMax) : '',
          salaryCurrency: job.salaryCurrency || '',
          salaryPeriod: job.salaryPeriod || '',
          experienceMinYears: job.experienceMinYears != null ? String(job.experienceMinYears) : '',
          experienceMaxYears: job.experienceMaxYears != null ? String(job.experienceMaxYears) : '',
          numberOfPositions: String(job.numberOfPositions || 1),
          deadline: job.deadline ? toLocalDateTimeInput(job.deadline) : '',
          sectorId: job.sectorId || '',
        })
      })
      .catch((err) => {
        if (!cancelled) setLoadError(getApiErrorMessage(err))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
    // formik.setValues is stable enough here; the load only needs jobId/auth.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [jobId, auth.status])

  const handleSubmit = async (values: typeof initialValues) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setSubmitError('You are not signed in.')
      return
    }
    if (!jobId) return
    setSubmitError(null)

    const payload: JobWriteRequest = {
      title: values.title,
      description: values.description || undefined,
      company: values.company || undefined,
      location: values.location || undefined,
      jobType: values.jobType || undefined,
      workMode: values.workArrangement || undefined,
      url: values.url || undefined,
      salary: values.salary || undefined,
      salaryMin: values.salaryMin ? Number(values.salaryMin) : undefined,
      salaryMax: values.salaryMax ? Number(values.salaryMax) : undefined,
      salaryCurrency: values.salaryCurrency || undefined,
      salaryPeriod: values.salaryPeriod || undefined,
      experienceMinYears: values.experienceMinYears ? Number(values.experienceMinYears) : undefined,
      experienceMaxYears: values.experienceMaxYears ? Number(values.experienceMaxYears) : undefined,
      numberOfPositions: values.numberOfPositions ? Number(values.numberOfPositions) : undefined,
      deadline: values.deadline
        ? new Date(values.deadline).toISOString()
        : undefined,
      sectorId: values.sectorId || undefined,
      saveAsDraft: true,
    }

    try {
      await updateJob(jobId, payload, tokens.accessToken)
      navigate('/dashboard/admin/jobs', {
        state: { toast: 'Changes saved.' },
      })
    } catch (err) {
      setSubmitError(getApiErrorMessage(err))
    }
  }

  // Publish / unpublish the job without leaving the edit page.
  const [adminActionLoading, setAdminActionLoading] = useState(false)

  const handleApprove = async () => {
    if (!jobId) return
    setAdminActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await approveJob(jobId, tokens.accessToken)
      setToast('Job published.')
      navigate('/dashboard/admin/jobs', {
        state: { toast: 'Job published.' },
      })
    } catch (err) {
      setSubmitError(getApiErrorMessage(err))
    } finally {
      setAdminActionLoading(false)
    }
  }

  const handleUnpublish = async () => {
    if (!jobId) return
    setAdminActionLoading(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await rejectJob(jobId, 'Unpublished by admin', tokens.accessToken)
      setToast('Job unpublished.')
      navigate('/dashboard/admin/jobs', {
        state: { toast: 'Job unpublished.' },
      })
    } catch (err) {
      setSubmitError(getApiErrorMessage(err))
    } finally {
      setAdminActionLoading(false)
    }
  }

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span
          aria-hidden="true"
          className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
        />
      </div>
    )
  }

  const canEdit = !loading && !loadError
  const submitting = formik.isSubmitting

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <Link
        to="/dashboard/admin/jobs"
        className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to jobs
      </Link>

      {toast && (
        <div
          role="status"
          className="mt-4 flex items-center gap-2.5 rounded-xl border border-success/30 bg-surface-container-lowest px-4 py-3 font-label-md text-label-md text-success"
        >
          <span className="material-symbols-outlined text-lg">check_circle</span>
          {toast}
        </div>
      )}

      <h1 className="mt-4 font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
        Edit job
      </h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Update the details below, then save. Use the status controls to publish
        or unpublish the job.
      </p>

      {loading && (
        <div className="mt-10 flex items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      )}

      {loadError && (
        <div
          role="alert"
          className="mt-6 rounded-xl border border-error/30 bg-error-container px-4 py-3.5 font-label-md text-label-md text-on-error-container"
        >
          {loadError}
        </div>
      )}

      {!loading && !loadError && (
        <form onSubmit={formik.handleSubmit} noValidate className="mt-8">
          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
            <div className="border-b border-surface-variant px-6 py-4">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">
                Job details
              </h2>
            </div>

            {rejection && (
              <div className="mx-6 mt-6 flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3.5 font-label-md text-label-md text-amber-900">
                <span className="material-symbols-outlined mt-0.5 text-lg">
                  warning
                </span>
                <div>
                  <p className="font-semibold">This job was rejected</p>
                  <p className="mt-0.5">{rejection}</p>
                </div>
              </div>
            )}

            {submitError && (
              <div
                role="alert"
                className="mx-6 mt-6 flex items-start gap-2.5 rounded-xl border border-error/30 bg-error-container px-4 py-3.5 font-label-md text-label-md text-on-error-container"
              >
                <span className="material-symbols-outlined mt-0.5 text-lg">
                  error
                </span>
                <span>{submitError}</span>
              </div>
            )}

            <div className="grid gap-6 p-6 md:grid-cols-2">
              <Field
                label="Job title *"
                error={formik.touched.title ? formik.errors.title : undefined}
              >
                <input
                  name="title"
                  value={formik.values.title}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="e.g. Senior Flutter Developer"
                  className={inputClass}
                />
              </Field>

              <Field label="Sector *" error={formik.touched.sectorId ? formik.errors.sectorId : undefined}>
                <select
                  name="sectorId"
                  value={formik.values.sectorId}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  <option value="">Select a sector</option>
                  {sectors.map((sector) => (
                    <option key={sector.id} value={sector.id}>
                      {sector.name}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Job type">
                <select
                  name="jobType"
                  value={formik.values.jobType}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  <option value="">Other</option>
                  {FORM_JOB_TYPE_OPTIONS.map((value) => (
                    <option key={value} value={value}>
                      {JOB_TYPE_LABELS[value]}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Work arrangement">
                <select
                  name="workArrangement"
                  value={formik.values.workArrangement}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  <option value="">Onsite</option>
                  {FORM_WORK_ARRANGEMENT_OPTIONS.map((value) => (
                    <option key={value} value={value}>
                      {WORK_ARRANGEMENT_LABELS[value]}
                    </option>
                  ))}
                </select>
              </Field>

              <Field
                label="Company"
                error={formik.touched.company ? formik.errors.company : undefined}
              >
                <input
                  name="company"
                  value={formik.values.company}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="Company name"
                  className={inputClass}
                />
              </Field>

              <Field
                label="Location"
                error={formik.touched.location ? formik.errors.location : undefined}
              >
                <input
                  name="location"
                  value={formik.values.location}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="City, region (e.g. Addis Ababa)"
                  className={inputClass}
                />
              </Field>

              <Field label="Salary (free text)">
                <input
                  name="salary"
                  value={formik.values.salary}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="e.g. ETB 60,000 – 80,000 / month"
                  className={inputClass}
                />
              </Field>

              <Field label="Minimum salary">
                <input
                  type="number"
                  name="salaryMin"
                  value={formik.values.salaryMin}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="e.g. 60000"
                  min="0"
                  className={inputClass}
                />
              </Field>

              <Field label="Maximum salary">
                <input
                  type="number"
                  name="salaryMax"
                  value={formik.values.salaryMax}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="e.g. 80000"
                  min="0"
                  className={inputClass}
                />
              </Field>

              <Field label="Salary currency">
                <select
                  name="salaryCurrency"
                  value={formik.values.salaryCurrency}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  <option value="">Not specified</option>
                  {SALARY_CURRENCY_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Salary period">
                <select
                  name="salaryPeriod"
                  value={formik.values.salaryPeriod}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  <option value="">Not specified</option>
                  {SALARY_PERIOD_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Experience (min years)">
                <select
                  name="experienceMinYears"
                  value={formik.values.experienceMinYears}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  {EXPERIENCE_YEAR_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Experience (max years)">
                <select
                  name="experienceMaxYears"
                  value={formik.values.experienceMaxYears}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                >
                  {EXPERIENCE_YEAR_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Number of positions">
                <input
                  type="number"
                  name="numberOfPositions"
                  value={formik.values.numberOfPositions}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  min="1"
                  max="999"
                  className={inputClass}
                />
              </Field>

              <Field
                label="Application URL"
                error={formik.touched.url ? formik.errors.url : undefined}
              >
                <input
                  name="url"
                  value={formik.values.url}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="https://…"
                  className={inputClass}
                />
              </Field>

              <Field label="Application deadline">
                <input
                  type="datetime-local"
                  name="deadline"
                  value={formik.values.deadline}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  className={inputClass}
                />
              </Field>

              <Field full label="Description">
                <RichTextEditor
                  value={formik.values.description}
                  onChange={(v) => formik.setFieldValue('description', v)}
                  placeholder="Role overview, responsibilities, requirements..."
                  disabled={!canEdit}
                />
              </Field>
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-surface-variant px-6 py-5 sm:flex-row sm:items-center sm:justify-between">
              <Link
                to="/dashboard/admin/jobs"
                className="inline-flex items-center justify-center rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
              >
                Cancel
              </Link>
              <button
                type="button"
                disabled={submitting || !canEdit}
                onClick={() => formik.submitForm()}
                className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
              >
                {submitting ? (
                  <>
                    <span
                      aria-hidden="true"
                      className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/40 border-t-on-accent"
                    />
                    Saving…
                  </>
                ) : (
                  'Save changes'
                )}
              </button>
            </div>
          </div>

          {/* Publish / unpublish controls */}
          {loadedJob && (
            <div className="mt-4 flex flex-wrap gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-4 shadow-sm">
              <span className="font-label-sm text-label-sm font-medium text-on-surface-variant">Status:</span>
              <span className="rounded-full bg-amber-100 px-3 py-1 font-label-sm text-amber-900">
                {loadedJob.status === 'published' ? 'Published' : loadedJob.status === 'pendingApproval' ? 'Pending review' : loadedJob.status}
              </span>
              <div className="ml-auto flex gap-2">
                {loadedJob.status === 'published' ? (
                  <button
                    type="button"
                    disabled={adminActionLoading}
                    onClick={handleUnpublish}
                    className="inline-flex items-center gap-2 rounded-lg border border-error/30 bg-error-container px-5 py-2.5 font-label-md text-label-md font-medium text-on-error transition-colors hover:bg-error-container/30 disabled:pointer-events-none disabled:opacity-50"
                  >
                    {adminActionLoading ? (
                      <>
                        <span
                          aria-hidden="true"
                          className="h-4 w-4 animate-spin rounded-full border-2 border-on-error/40 border-t-on-error"
                        />
                        Unpublishing…
                      </>
                    ) : (
                      <>
                        <span className="material-symbols-outlined text-lg">unpublished</span>
                        Unpublish
                      </>
                    )}
                  </button>
                ) : (
                  <button
                    type="button"
                    disabled={adminActionLoading}
                    onClick={handleApprove}
                    className="inline-flex items-center gap-2 rounded-lg bg-success px-5 py-2.5 font-label-md text-label-md font-medium text-on-success transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50"
                  >
                    {adminActionLoading ? (
                      <>
                        <span
                          aria-hidden="true"
                          className="h-4 w-4 animate-spin rounded-full border-2 border-on-success/40 border-t-on-success"
                        />
                        Publishing…
                      </>
                    ) : (
                      <>
                        <span className="material-symbols-outlined text-lg">publish</span>
                        Publish now
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          )}
        </form>
      )}
    </DashboardShell>
  )
}
