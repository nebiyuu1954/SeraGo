import { useEffect, useState } from 'react'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
  fetchProfile,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
  updateProfile,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { RequiredRole } from '../../../hooks'
import type { ProfileResponse, UpdateProfileRequest } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import PasswordSetupCard from '../../../components/dashboard/PasswordSetupCard.tsx'
import { cn } from '../../../lib/cn.ts'

/* ------------------------------------------------------- Backend constants */

/** Enum values the API stores/returns as PascalCase (.ToString()). */
const EXPERIENCE_LEVELS = ['Entry', 'Junior', 'Mid', 'Senior', 'Lead']
const WORK_MODES = ['Onsite', 'Remote', 'Hybrid']
const AVAILABILITIES = [
  'Immediate',
  'WithinTwoWeeks',
  'WithinOneMonth',
  'MoreThanOneMonth',
]

const JOB_TYPE_CHOICES = [
  { value: 'FullTime', label: 'Full-time' },
  { value: 'PartTime', label: 'Part-time' },
  { value: 'Contract', label: 'Contract' },
  { value: 'Contractual', label: 'Contractual' },
  { value: 'Remote', label: 'Remote' },
  { value: 'Internship', label: 'Internship' },
  { value: 'Freelance', label: 'Freelance' },
  { value: 'Temporary', label: 'Temporary' },
  { value: 'Other', label: 'Other' },
]

const MISSING_LABELS: Record<string, string> = {
  headline: 'Headline',
  about: 'About',
  experienceLevel: 'Experience level',
  yearsOfExperience: 'Years of experience',
  desiredRoles: 'Desired roles',
  skills: 'Skills',
  desiredJobTypes: 'Desired job types',
  workMode: 'Work mode',
  availability: 'Availability',
  preferredSectors: 'Preferred sectors',
  companyName: 'Company name',
  industry: 'Industry',
  companySize: 'Company size',
  websiteUrl: 'Website',
}

const inputClass =
  'w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary'

const optionalUrl = string().test(
  'absolute-url',
  'Enter a valid URL (e.g. https://example.com).',
  (value) => !value || /^https?:\/\/[^\s]+$/i.test(value),
)

const commonSchema = object({
  firstName: string().required('First name is required.').max(120),
  lastName: string().required('Last name is required.').max(120),
  avatarUrl: optionalUrl,
  city: string().max(120),
  country: string().max(120),
})

const talentSchema = commonSchema.concat(
  object({
    headline: string().max(200),
    about: string().max(4000),
    yearsOfExperience: string().test(
      'non-negative-int',
      'Years of experience must be 0 or greater.',
      (value) => !value || /^\d+$/.test(value),
    ),
    resumeUrl: optionalUrl,
    linkedInUrl: optionalUrl,
    githubUrl: optionalUrl,
    portfolioUrl: optionalUrl,
  }),
)

const recruiterSchema = commonSchema.concat(
  object({
    companyName: string().required('Company name is required.').max(255),
    companyLogoUrl: optionalUrl,
    industry: string().max(120),
    companySize: string().max(64),
    websiteUrl: optionalUrl,
    about: string().max(4000),
  }),
)

interface ProfileFormValues {
  // Common
  firstName: string
  lastName: string
  avatarUrl: string
  city: string
  country: string
  // Talent
  headline: string
  about: string
  experienceLevel: string
  yearsOfExperience: string
  desiredRoles: string[]
  skills: string[]
  desiredJobTypes: string[]
  workMode: string
  availability: string
  preferredSectorIds: string[]
  resumeUrl: string
  linkedInUrl: string
  githubUrl: string
  portfolioUrl: string
  // Recruiter
  companyName: string
  companyLogoUrl: string
  industry: string
  companySize: string
  websiteUrl: string
}

const initialValues: ProfileFormValues = {
  firstName: '',
  lastName: '',
  avatarUrl: '',
  city: '',
  country: '',
  headline: '',
  about: '',
  experienceLevel: '',
  yearsOfExperience: '',
  desiredRoles: [],
  skills: [],
  desiredJobTypes: [],
  workMode: '',
  availability: '',
  preferredSectorIds: [],
  resumeUrl: '',
  linkedInUrl: '',
  githubUrl: '',
  portfolioUrl: '',
  companyName: '',
  companyLogoUrl: '',
  industry: '',
  companySize: '',
  websiteUrl: '',
}

/* ------------------------------------------------------------ Field bits */

function Field({
  label,
  full,
  hint,
  error,
  children,
}: {
  label: string
  full?: boolean
  hint?: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div className={cn(full && 'md:col-span-2')}>
      <label className="font-label-sm text-label-sm font-medium text-on-surface">
        {label}
      </label>
      <div className="mt-1.5">{children}</div>
      {hint && !error && (
        <p className="mt-1.5 font-label-sm text-label-sm text-on-surface-variant/70">
          {hint}
        </p>
      )}
      {error && (
        <p className="mt-1.5 font-label-sm text-label-sm text-error">{error}</p>
      )}
    </div>
  )
}

function TagInput({
  value,
  onChange,
  placeholder,
}: {
  value: string[]
  onChange: (next: string[]) => void
  placeholder?: string
}) {
  const [input, setInput] = useState('')

  const add = () => {
    const tag = input.trim()
    if (tag && !value.some((t) => t.toLowerCase() === tag.toLowerCase())) {
      onChange([...value, tag])
    }
    setInput('')
  }

  return (
    <div className="flex flex-wrap items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-2 py-1.5 transition-colors focus-within:border-primary focus-within:ring-1 focus-within:ring-primary">
      {value.map((tag) => (
        <span
          key={tag}
          className="inline-flex items-center gap-1 rounded-md bg-primary-container/60 px-2 py-1 font-label-sm text-label-sm font-medium text-primary"
        >
          {tag}
          <button
            type="button"
            aria-label={`Remove ${tag}`}
            onClick={() => onChange(value.filter((t) => t !== tag))}
            className="text-primary/70 transition-colors hover:text-primary"
          >
            <span className="material-symbols-outlined text-sm">close</span>
          </button>
        </span>
      ))}
      <input
        value={input}
        onChange={(e) => setInput(e.target.value)}
        onBlur={add}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ',') {
            e.preventDefault()
            add()
          } else if (
            e.key === 'Backspace' &&
            input === '' &&
            value.length > 0
          ) {
            onChange(value.slice(0, -1))
          }
        }}
        placeholder={value.length === 0 ? placeholder : ''}
        className="min-w-[8rem] flex-1 bg-transparent px-1 py-1 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:outline-none"
      />
    </div>
  )
}

function SectionCard({
  title,
  description,
  children,
}: {
  title: string
  description: string
  children: React.ReactNode
}) {
  return (
    <section className="rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
      <div className="border-b border-surface-variant px-6 py-4">
        <h2 className="font-label-md text-label-md font-semibold text-on-surface">
          {title}
        </h2>
        <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
          {description}
        </p>
      </div>
      <div className="grid gap-6 p-6 md:grid-cols-2">{children}</div>
    </section>
  )
}

/* -------------------------------------------------------------- The form */

const COPY: Record<RequiredRole, { blurb: string }> = {
  Recruiter: {
    blurb:
      'Your personal details and company information — shown on your job posts.',
  },
  Talent: {
    blurb:
      'Your professional details — used to match you with the right roles.',
  },
  Admin: {
    blurb:
      'Your account details. Admins have no role-specific profile section.',
  },
}

export default function ProfileForm({ role }: { role: RequiredRole }) {
  const auth = useRequireRole(role)
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [sectors, setSectors] = useState<{ id: string; name: string }[]>([])

  // Auto-dismiss the saved confirmation.
  useEffect(() => {
    if (!saved) return
    const t = setTimeout(() => setSaved(false), 3000)
    return () => clearTimeout(t)
  }, [saved])

  const isTalent = role === 'Talent'
  const isRecruiter = role === 'Recruiter'

  // The sector vocabulary for the Job preferences picker (talent only).
  useEffect(() => {
    if (auth.status !== 'authenticated' || !isTalent) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    fetchSectors(tokens.accessToken)
      .then((list) => setSectors(list))
      .catch(() => {
        /* Picker stays empty — the profile still saves; sectors re-fetch next visit. */
      })
  }, [auth.status, isTalent])

  const validationSchema =
    role === 'Talent'
      ? talentSchema
      : role === 'Recruiter'
        ? recruiterSchema
        : commonSchema

  const formik = useFormik<ProfileFormValues>({
    initialValues,
    validationSchema,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: (values) => handleSave(values),
  })

  // Load the current profile and prefill the form.
  useEffect(() => {
    if (auth.status !== 'authenticated') return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    fetchProfile(tokens.accessToken)
      .then((p) => {
        if (cancelled) return
        setProfile(p)
        formik.setValues({
          firstName: p.firstName,
          lastName: p.lastName,
          avatarUrl: p.avatarUrl,
          city: p.city,
          country: p.country,
          headline: p.talent?.headline ?? '',
          about: isTalent
            ? (p.talent?.about ?? '')
            : (p.recruiter?.about ?? ''),
          experienceLevel: p.talent?.experienceLevel ?? '',
          yearsOfExperience:
            p.talent?.yearsOfExperience == null
              ? ''
              : String(p.talent.yearsOfExperience),
          desiredRoles: p.talent?.desiredRoles ?? [],
          skills: p.talent?.skills ?? [],
          desiredJobTypes: p.talent?.desiredJobTypes ?? [],
          preferredSectorIds: p.talent?.preferredSectorIds ?? [],
          workMode: p.talent?.workMode ?? '',
          availability: p.talent?.availability ?? '',
          resumeUrl: p.talent?.resumeUrl ?? '',
          linkedInUrl: p.talent?.linkedInUrl ?? '',
          githubUrl: p.talent?.githubUrl ?? '',
          portfolioUrl: p.talent?.portfolioUrl ?? '',
          companyName: p.recruiter?.companyName ?? '',
          companyLogoUrl: p.recruiter?.companyLogoUrl ?? '',
          industry: p.recruiter?.industry ?? '',
          companySize: p.recruiter?.companySize ?? '',
          websiteUrl: p.recruiter?.websiteUrl ?? '',
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
    // formik.setValues is stable enough here; the load only needs auth/role.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.status, role])

  const handleSave = async (values: ProfileFormValues) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setSubmitError('You are not signed in.')
      return
    }
    setSubmitError(null)
    setSaved(false)

    const payload: UpdateProfileRequest = {
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim(),
      avatarUrl: values.avatarUrl.trim(),
      city: values.city.trim(),
      country: values.country.trim(),
    }
    if (isTalent) {
      payload.talent = {
        headline: values.headline.trim(),
        about: values.about.trim(),
        experienceLevel: values.experienceLevel || null,
        yearsOfExperience:
          values.yearsOfExperience === ''
            ? null
            : Number(values.yearsOfExperience),
        desiredRoles: values.desiredRoles,
        skills: values.skills,
        desiredJobTypes: values.desiredJobTypes,
        preferredSectorIds: values.preferredSectorIds,
        workMode: values.workMode || null,
        availability: values.availability || null,
        resumeUrl: values.resumeUrl.trim(),
        linkedInUrl: values.linkedInUrl.trim(),
        githubUrl: values.githubUrl.trim(),
        portfolioUrl: values.portfolioUrl.trim(),
      }
    } else if (isRecruiter) {
      payload.recruiter = {
        companyName: values.companyName.trim(),
        companyLogoUrl: values.companyLogoUrl.trim(),
        industry: values.industry.trim(),
        companySize: values.companySize.trim(),
        websiteUrl: values.websiteUrl.trim(),
        about: values.about.trim(),
      }
    }

    try {
      const updated = await updateProfile(tokens.accessToken, payload)
      setProfile(updated)
      setSaved(true)
    } catch (err) {
      setSubmitError(getApiErrorMessage(err))
    }
  }

  const toggleJobType = (value: string) => {
    const next = formik.values.desiredJobTypes.includes(value)
      ? formik.values.desiredJobTypes.filter((t) => t !== value)
      : [...formik.values.desiredJobTypes, value]
    formik.setFieldValue('desiredJobTypes', next)
  }

  const toggleSector = (id: string) => {
    const next = formik.values.preferredSectorIds.includes(id)
      ? formik.values.preferredSectorIds.filter((s) => s !== id)
      : [...formik.values.preferredSectorIds, id]
    formik.setFieldValue('preferredSectorIds', next)
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

  const completion = profile?.completion

  return (
    <DashboardShell role={role} authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
        Profile
      </h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        {COPY[role].blurb}
      </p>

      {/* Completion banner */}
      {completion &&
        (completion.isComplete ? (
          <div className="mt-6 flex items-center gap-2.5 rounded-xl border border-success/30 bg-surface-container-lowest px-4 py-3 font-label-md text-label-md text-success">
            <span className="material-symbols-outlined text-lg">
              check_circle
            </span>
            Profile complete — you're all set.
          </div>
        ) : (
          <div className="mt-6 rounded-xl border border-amber-200 bg-amber-50 p-4">
            <div className="flex items-center justify-between gap-3">
              <span className="flex items-center gap-2 font-label-md text-label-md font-medium text-amber-900">
                <span className="material-symbols-outlined text-lg">
                  pending_actions
                </span>
                Your profile is {completion.percentComplete}% complete
              </span>
              <span className="font-label-sm text-label-sm text-amber-800">
                {completion.missingFields.length} field
                {completion.missingFields.length === 1 ? '' : 's'} to go
              </span>
            </div>
            <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-amber-200/70">
              <div
                className="h-full rounded-full bg-amber-500 transition-all"
                style={{ width: `${completion.percentComplete}%` }}
              />
            </div>
            {completion.missingFields.length > 0 && (
              <div className="mt-3 flex flex-wrap gap-1.5">
                {completion.missingFields.map((field) => (
                  <span
                    key={field}
                    className="rounded-full bg-amber-100 px-2.5 py-0.5 font-label-sm text-label-sm text-amber-800"
                  >
                    {MISSING_LABELS[field] ?? field}
                  </span>
                ))}
              </div>
            )}
          </div>
        ))}

      {/* Saved toast */}
      {saved && (
        <div
          role="status"
          className="mt-6 flex items-center gap-2.5 rounded-xl border border-success/30 bg-surface-container-lowest px-4 py-3 font-label-md text-label-md text-success"
        >
          <span className="material-symbols-outlined text-lg">
            check_circle
          </span>
          Profile saved.
        </div>
      )}

      {submitError && (
        <div
          role="alert"
          className="mt-6 flex items-start gap-2.5 rounded-xl border border-error/30 bg-error-container px-4 py-3.5 font-label-md text-label-md text-on-error-container"
        >
          <span className="material-symbols-outlined mt-0.5 text-lg">
            error
          </span>
          <span>{submitError}</span>
        </div>
      )}

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
        <form
          onSubmit={formik.handleSubmit}
          noValidate
          className="mt-8 space-y-6"
        >
          <SectionCard
            title="Personal information"
            description="Your name and location — shown across the platform."
          >
            <Field
              label="First name *"
              error={
                formik.touched.firstName ? formik.errors.firstName : undefined
              }
            >
              <input
                name="firstName"
                value={formik.values.firstName}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                placeholder="Jane"
                className={inputClass}
              />
            </Field>
            <Field
              label="Last name *"
              error={
                formik.touched.lastName ? formik.errors.lastName : undefined
              }
            >
              <input
                name="lastName"
                value={formik.values.lastName}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                placeholder="Recruiter"
                className={inputClass}
              />
            </Field>
            <Field
              label="Avatar URL"
              hint="Optional — a link to your profile picture."
              error={
                formik.touched.avatarUrl ? formik.errors.avatarUrl : undefined
              }
            >
              <input
                name="avatarUrl"
                value={formik.values.avatarUrl}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                placeholder="https://…"
                className={inputClass}
              />
            </Field>
            <Field label="City">
              <input
                name="city"
                value={formik.values.city}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                placeholder="Addis Ababa"
                className={inputClass}
              />
            </Field>
            <Field label="Country">
              <input
                name="country"
                value={formik.values.country}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                placeholder="Ethiopia"
                className={inputClass}
              />
            </Field>
          </SectionCard>

          {isRecruiter && (
            <SectionCard
              title="Company information"
              description="Your company details — shown on every job post you publish."
            >
              <Field
                label="Company name *"
                error={
                  formik.touched.companyName
                    ? formik.errors.companyName
                    : undefined
                }
              >
                <input
                  name="companyName"
                  value={formik.values.companyName}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="SeraGo HR"
                  className={inputClass}
                />
              </Field>
              <Field
                label="Company logo URL"
                error={
                  formik.touched.companyLogoUrl
                    ? formik.errors.companyLogoUrl
                    : undefined
                }
              >
                <input
                  name="companyLogoUrl"
                  value={formik.values.companyLogoUrl}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="https://…"
                  className={inputClass}
                />
              </Field>
              <Field label="Industry">
                <input
                  name="industry"
                  value={formik.values.industry}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="Technology"
                  className={inputClass}
                />
              </Field>
              <Field label="Company size">
                <input
                  name="companySize"
                  value={formik.values.companySize}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="11–50 employees"
                  className={inputClass}
                />
              </Field>
              <Field
                label="Website"
                error={
                  formik.touched.websiteUrl
                    ? formik.errors.websiteUrl
                    : undefined
                }
              >
                <input
                  name="websiteUrl"
                  value={formik.values.websiteUrl}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="https://company.com"
                  className={inputClass}
                />
              </Field>
              <Field full label="About the company">
                <textarea
                  name="about"
                  rows={4}
                  value={formik.values.about}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="What does your company do?"
                  className={cn(inputClass, 'resize-y')}
                />
              </Field>
            </SectionCard>
          )}

          {isTalent && (
            <SectionCard
              title="Job preferences"
              description="Pick the sectors you want in your feed — only matching jobs will be shown to you."
            >
              <Field full label="Preferred sectors">
                {sectors.length === 0 ? (
                  <p className="font-label-sm text-label-sm text-on-surface-variant">
                    No sectors available yet.
                  </p>
                ) : (
                  <div className="mt-1.5 flex flex-wrap gap-2">
                    {sectors.map((sector) => {
                      const checked = formik.values.preferredSectorIds.includes(
                        sector.id,
                      )
                      return (
                        <button
                          key={sector.id}
                          type="button"
                          aria-pressed={checked}
                          onClick={() => toggleSector(sector.id)}
                          className={cn(
                            'rounded-full px-3.5 py-1.5 font-label-sm text-label-sm font-medium transition-colors',
                            checked
                              ? 'bg-primary text-on-primary'
                              : 'border border-outline-variant bg-surface-container-lowest text-on-surface-variant hover:bg-surface-container-low',
                          )}
                        >
                          {sector.name}
                        </button>
                      )
                    })}
                  </div>
                )}
              </Field>
            </SectionCard>
          )}

          {isTalent && (
            <SectionCard
              title="Professional details"
              description="What you're looking for — used to match you with roles."
            >
              <Field
                label="Headline"
                error={
                  formik.touched.headline ? formik.errors.headline : undefined
                }
              >
                <input
                  name="headline"
                  value={formik.values.headline}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="e.g. Senior Flutter Developer"
                  className={inputClass}
                />
              </Field>
              <Field
                label="Years of experience"
                error={
                  formik.touched.yearsOfExperience
                    ? formik.errors.yearsOfExperience
                    : undefined
                }
              >
                <input
                  name="yearsOfExperience"
                  type="number"
                  min={0}
                  value={formik.values.yearsOfExperience}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="5"
                  className={inputClass}
                />
              </Field>
              <Field label="Experience level">
                <select
                  name="experienceLevel"
                  value={formik.values.experienceLevel}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  className={inputClass}
                >
                  <option value="">Not specified</option>
                  {EXPERIENCE_LEVELS.map((level) => (
                    <option key={level} value={level}>
                      {level}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Work mode">
                <select
                  name="workMode"
                  value={formik.values.workMode}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  className={inputClass}
                >
                  <option value="">Not specified</option>
                  {WORK_MODES.map((mode) => (
                    <option key={mode} value={mode}>
                      {mode}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Availability">
                <select
                  name="availability"
                  value={formik.values.availability}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  className={inputClass}
                >
                  <option value="">Not specified</option>
                  {AVAILABILITIES.map((availability) => (
                    <option key={availability} value={availability}>
                      {availability}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Desired roles" hint="Press Enter after each role.">
                <TagInput
                  value={formik.values.desiredRoles}
                  onChange={(next) =>
                    formik.setFieldValue('desiredRoles', next)
                  }
                  placeholder="e.g. Product Designer"
                />
              </Field>
              <Field label="Skills" hint="Press Enter after each skill.">
                <TagInput
                  value={formik.values.skills}
                  onChange={(next) => formik.setFieldValue('skills', next)}
                  placeholder="e.g. React, Figma"
                />
              </Field>
              <Field full label="Desired job types">
                <div className="mt-1.5 flex flex-wrap gap-2">
                  {JOB_TYPE_CHOICES.map((choice) => {
                    const checked = formik.values.desiredJobTypes.includes(
                      choice.value,
                    )
                    return (
                      <button
                        key={choice.value}
                        type="button"
                        aria-pressed={checked}
                        onClick={() => toggleJobType(choice.value)}
                        className={cn(
                          'rounded-full px-3.5 py-1.5 font-label-sm text-label-sm font-medium transition-colors',
                          checked
                            ? 'bg-primary text-on-primary'
                            : 'border border-outline-variant bg-surface-container-lowest text-on-surface-variant hover:bg-surface-container-low',
                        )}
                      >
                        {choice.label}
                      </button>
                    )
                  })}
                </div>
              </Field>
              <Field label="About" hint="A short bio recruiters will see.">
                <textarea
                  name="about"
                  rows={4}
                  value={formik.values.about}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="Tell recruiters about yourself…"
                  className={cn(inputClass, 'resize-y')}
                />
              </Field>
              <Field label="Resume URL">
                <input
                  name="resumeUrl"
                  value={formik.values.resumeUrl}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="https://…"
                  className={inputClass}
                />
              </Field>
              <Field label="LinkedIn">
                <input
                  name="linkedInUrl"
                  value={formik.values.linkedInUrl}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="https://linkedin.com/in/…"
                  className={inputClass}
                />
              </Field>
              <Field label="GitHub">
                <input
                  name="githubUrl"
                  value={formik.values.githubUrl}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="https://github.com/…"
                  className={inputClass}
                />
              </Field>
              <Field label="Portfolio">
                <input
                  name="portfolioUrl"
                  value={formik.values.portfolioUrl}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  placeholder="https://…"
                  className={inputClass}
                />
              </Field>
            </SectionCard>
          )}

          {role === 'Admin' && (
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              Admins don't have a role-specific profile section — everything
              above is what the platform stores about you.
            </p>
          )}

          <div className="flex items-center justify-end gap-3">
            {saved && (
              <span className="font-label-md text-label-md text-success">
                Saved
              </span>
            )}
            <button
              type="submit"
              disabled={formik.isSubmitting}
              className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent shadow-sm transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
            >
              {formik.isSubmitting ? (
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
        </form>
      )}

      <PasswordSetupCard />
    </DashboardShell>
  )
}
