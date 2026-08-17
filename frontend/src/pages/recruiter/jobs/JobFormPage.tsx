import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
  createJob,
  fetchJob,
  getApiErrorMessage,
  getStoredAuthTokens,
  submitJob,
  updateJob,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type {
  JobResponse,
  JobType,
  JobWriteRequest,
  WorkMode,
} from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import {
  FORM_JOB_TYPE_OPTIONS,
  FORM_WORK_ARRANGEMENT_OPTIONS,
  JOB_TYPE_LABELS,
  WORK_ARRANGEMENT_LABELS,
} from '../../../components/dashboard/jobOptions.ts'
import { cn } from '../../../lib/cn.ts'

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
  deadline: '',
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
 * Create / edit job form. Two submit modes — "Save draft" keeps the job
 * hidden (resumable later); "Submit for review" sends it to the admin queue
 * (create) or saves then submits (edit). Editing a rejected job resets it to
 * draft, exactly like the API.
 */
export default function JobFormPage() {
  const { jobId } = useParams<{ jobId: string }>()
  const isEdit = Boolean(jobId)
  const auth = useRequireRole('Recruiter')
  const navigate = useNavigate()

  const submitMode = useRef<'draft' | 'submit'>('draft')

  const [loadedJob, setLoadedJob] = useState<JobResponse | null>(null)
  const [loading, setLoading] = useState(isEdit)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [rejection, setRejection] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [canEdit, setCanEdit] = useState(true)

  const formik = useFormik({
    initialValues,
    validationSchema,
    enableReinitialize: true,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: (values) => handleSubmit(values, submitMode.current),
  })

  // Load the job when editing (prefill + status banners).
  useEffect(() => {
    if (!isEdit || auth.status !== 'authenticated') return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    fetchJob(jobId as string, tokens.accessToken)
      .then((job) => {
        if (cancelled) return
        setLoadedJob(job)
        if (job.status === 'rejected') {
          setRejection(job.rejectionReason ?? 'No reason was provided.')
        }
        if (job.status === 'published' && !job.isOwner) {
          setCanEdit(false)
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
          deadline: job.deadline ? toLocalDateTimeInput(job.deadline) : '',
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
  }, [isEdit, jobId, auth.status])

  const handleSubmit = async (
    values: typeof initialValues,
    mode: 'draft' | 'submit',
  ) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setSubmitError('You are not signed in.')
      return
    }
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
      deadline: values.deadline
        ? new Date(values.deadline).toISOString()
        : undefined,
      saveAsDraft: true,
    }

    try {
      if (isEdit) {
        await updateJob(jobId as string, payload, tokens.accessToken)
        if (mode === 'submit') {
          await submitJob(jobId as string, tokens.accessToken)
        }
        navigate('/dashboard/recruiter', {
          state: {
            toast:
              mode === 'submit'
                ? 'Job submitted for review.'
                : 'Changes saved.',
          },
        })
      } else {
        const job = await createJob(
          { ...payload, saveAsDraft: mode === 'draft' },
          tokens.accessToken,
        )
        navigate('/dashboard/recruiter', {
          state: {
            toast:
              mode === 'draft'
                ? 'Draft saved — you can finish it later.'
                : job.status === 'published'
                  ? 'Job published.'
                  : 'Job submitted for review.',
          },
        })
      }
    } catch (err) {
      setSubmitError(getApiErrorMessage(err))
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

  const isPending = isEdit && loadedJob?.status === 'pendingApproval'
  // Create → the primary CTA submits for review. Edit → save & submit, unless
  // the job is already pending (then saving is all there is to do).
  const primaryMode: 'draft' | 'submit' = isEdit
    ? isPending
      ? 'draft'
      : 'submit'
    : 'submit'
  const primaryLabel = isEdit
    ? isPending
      ? 'Save changes'
      : 'Save & submit for review'
    : 'Submit for review'
  const secondaryLabel = 'Save as draft'
  const submitting = formik.isSubmitting

  return (
    <DashboardShell role="Recruiter" authUser={auth.user}>
      <Link
        to="/dashboard/recruiter"
        className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-lg">arrow_back</span>
        Back to jobs
      </Link>

      <h1 className="mt-4 font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
        {isEdit ? 'Edit job' : 'Create job'}
      </h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        {isEdit
          ? 'Update the details below. Changes stay hidden until you submit for review.'
          : 'Post a job opening — save it as a draft and finish later, or submit it for review now.'}
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
                  <p className="mt-0.5 text-amber-800">
                    Fix the issues below and submit again — saving will reset it
                    to a draft.
                  </p>
                </div>
              </div>
            )}

            {!canEdit && (
              <div className="mx-6 mt-6 flex items-start gap-3 rounded-xl border border-error/30 bg-error-container px-4 py-3.5 font-label-md text-label-md text-on-error-container">
                <span className="material-symbols-outlined mt-0.5 text-lg">
                  lock
                </span>
                <span>
                  This job is published and can't be edited. Contact an admin to
                  make changes.
                </span>
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

              <Field label="Salary">
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
                <textarea
                  name="description"
                  rows={5}
                  value={formik.values.description}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  disabled={!canEdit}
                  placeholder="Role overview, responsibilities, requirements..."
                  className={cn(inputClass, 'resize-y')}
                />
              </Field>
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-surface-variant px-6 py-5 sm:flex-row sm:items-center sm:justify-between">
              <Link
                to="/dashboard/recruiter"
                className="inline-flex items-center justify-center rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
              >
                Cancel
              </Link>
              <div className="flex flex-col-reverse gap-3 sm:flex-row">
                {!(isEdit && isPending) && (
                  <button
                    type="button"
                    disabled={submitting || !canEdit}
                    onClick={() => {
                      submitMode.current = 'draft'
                      formik.submitForm()
                    }}
                    className="rounded-lg border border-outline-variant bg-surface-container-lowest px-6 py-3 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-60"
                  >
                    {secondaryLabel}
                  </button>
                )}
                <button
                  type="button"
                  disabled={submitting || !canEdit}
                  onClick={() => {
                    submitMode.current = primaryMode
                    formik.submitForm()
                  }}
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
                    primaryLabel
                  )}
                </button>
              </div>
            </div>
          </div>
        </form>
      )}
    </DashboardShell>
  )
}
