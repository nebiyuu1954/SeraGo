import { useCallback, useEffect, useRef, useState } from 'react'
import { useFormik } from 'formik'
import { array, object, string } from 'yup'
import {
  fetchProfile,
  getApiErrorMessage,
  getStoredAuthTokens,
  parseResume,
  patchSettingsCategory,
  updateProfile,
} from '../../../api'
import { useRequireRole, useSettingsQuery, useUnsavedChanges } from '../../../hooks'
import type { RequiredRole } from '../../../hooks'
import type {
  ParsedResumeProfile,
  ProfileResponse,
  UpdateProfileRequest,
} from '../../../types'
import { useLocation, useNavigate } from 'react-router-dom'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import UnsavedChangesDialog from '../../../components/ui/UnsavedChangesDialog.tsx'
import RichTextEditor from '../../../components/ui/RichTextEditor.tsx'
import PasswordSetupCard from '../../../components/dashboard/PasswordSetupCard.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
import ResumeReviewModal from './ResumeReviewModal.tsx'
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

const EDUCATION_LEVELS = ['HighSchool', 'Certificate', 'Bachelors', 'Masters', 'PhD']
const EDUCATION_LEVEL_LABELS: Record<string, string> = {
  HighSchool: 'High School',
  Certificate: 'Certificate / Online Course',
  Bachelors: "Bachelor's Degree",
  Masters: "Master's Degree",
  PhD: 'PhD / Doctorate',
}

export interface WorkExperienceEntry {
  company: string
  title: string
  startDate: string
  endDate: string
  description: string
}

export interface EducationEntry {
  level: string
  institution: string
  degree: string
  gpa: string
  startYear: string
  endYear: string
}

/** Default privacy: everything visible. */
const DEFAULT_VISIBILITY: Record<string, boolean> = {
  phone: true,
  dateOfBirth: true,
  address: true,
  education: true,
  linkedin: true,
  github: true,
  portfolio: true,
  skills: true,
  experience: true,
  resume: true,
  avatar: true,
  middleName: true,
  city: true,
  currentIndustry: true,
  currentProfession: true,
  preferredLocations: true,
  about: true,
  desiredRoles: true,
  workMode: true,
  availability: true,
}

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

export const ETHIOPIAN_CITIES = [
  'Addis Ababa',
  'Adama',
  'Arba Minch',
  'Asosa',
  'Bahir Dar',
  'Bishoftu',
  'Debre Birhan',
  'Dessie',
  'Dilla',
  'Dire Dawa',
  'Gambela',
  'Gondar',
  'Harar',
  'Hawassa',
  'Hosanna',
  'Jigjiga',
  'Jimma',
  'Mekelle',
  'Nekemte',
  'Semera',
  'Shashamane',
  'Wolaita Sodo',
  'Other',
]

const FIELD_LABELS: Record<string, string> = {
  firstName: 'First name',
  middleName: 'Middle name',
  lastName: 'Last name',
  avatarUrl: 'Profile photo',
  city: 'City',
  about: 'About',
  experienceLevel: 'Experience level',
  yearsOfExperience: 'Years of experience',
  desiredRoles: 'Desired roles',
  skills: 'Skills',
  desiredJobTypes: 'Desired job types',
  workMode: 'Work mode',
  availability: 'Availability',
  resumeUrl: 'Resume',
  linkedInUrl: 'LinkedIn',
  githubUrl: 'GitHub',
  portfolioUrl: 'Portfolio',
  phoneNumber: 'Phone number',
  address: 'Address',
  workExperience: 'Work experience',
  educationLevel: 'Education level',
  educationHistory: 'Education history',
  currentIndustry: 'Current industry',
  currentProfession: 'Current profession',
  preferredLocations: 'Preferred locations',
  profileVisibility: 'Profile visibility',
  skillVisibility: 'Skill visibility',
  // Work experience sub-fields
  company: 'Company',
  title: 'Job title',
  startDate: 'Start date',
  endDate: 'End date',
  description: 'Description',
  // Education sub-fields
  level: 'Level',
  institution: 'Institution',
  degree: 'Degree',
  gpa: 'GPA',
  startYear: 'Start year',
  endYear: 'End year',
  'education-cross-field': 'Education entry',
}

const MISSING_LABELS: Record<string, string> = {
  about: 'About',
  experienceLevel: 'Experience level',
  yearsOfExperience: 'Years of experience',
  desiredRoles: 'Desired roles',
  skills: 'Skills',
  desiredJobTypes: 'Desired job types',
  workMode: 'Work mode',
}

const getInputClass = (hasError?: boolean, extraClasses?: string) =>
  cn(
    'w-full rounded-lg bg-surface-container-lowest px-3.5 py-2.5 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:outline-none focus:ring-1',
    hasError
      ? 'border border-error ring-1 ring-error focus:border-error focus:ring-error'
      : 'border border-outline-variant focus:border-primary focus:ring-primary',
    extraClasses,
  )

const inputClass = getInputClass(false)

const optionalUrl = string().test(
  'absolute-url',
  'Enter a valid URL (e.g. https://example.com).',
  (value) => !value || /^https?:\/\/[^\s]+$/i.test(value),
)

const workExperienceSchema = object().test(
  'has-data',
  'Work experience entry is incomplete — all fields are required.',
  (entry) => {
    if (!entry) return true
    const e = entry as unknown as WorkExperienceEntry
    const hasAny = e.company || e.title || e.startDate || e.endDate || e.description
    if (!hasAny) return true // empty row — skip validation
    return (
      string().required('Company name is required.').max(255).isValidSync(e.company) &&
      string().required('Job title is required.').max(200).isValidSync(e.title) &&
      string().required('Start date is required.').isValidSync(e.startDate) &&
      string().required('End date is required.').isValidSync(e.endDate) &&
      string().required('Description is required.').isValidSync(e.description)
    )
  },
).shape({
  company: string().max(255),
  title: string().max(200),
  startDate: string(),
  endDate: string(),
  description: string().max(10000, 'Description must be 10000 characters or less.'),
})

const educationEntrySchema = object().test(
  'has-data',
  'Education entry is incomplete — all fields except GPA are required.',
  (entry) => {
    if (!entry) return true
    const e = entry as unknown as EducationEntry
    const hasAny = e.level || e.institution || e.degree || e.gpa || e.startYear || e.endYear
    if (!hasAny) return true // empty row — skip validation
    const validYear = string().required('Start year is required.').test(
      'valid-year', '',
      (v) => /^\d{4}$/.test(v) && Number(v) >= 1970 && Number(v) <= 2099,
    )
    return (
      string().required('Education level is required.').isValidSync(e.level) &&
      string().required('Institution name is required.').max(255).isValidSync(e.institution) &&
      string().required('Degree is required.').max(255).isValidSync(e.degree) &&
      validYear.isValidSync(e.startYear) &&
      string().required('End year is required.').test(
        'valid-year', '',
        (v) => /^\d{4}$/.test(v) && Number(v) >= 1970 && Number(v) <= 2099,
      ).isValidSync(e.endYear)
    )
  },
).shape({
  level: string(),
  institution: string().max(255),
  degree: string().max(255),
  gpa: string().max(20),
  startYear: string(),
  endYear: string(),
})

const commonSchema = object({
  firstName: string().required('First name is required.').max(120),
  middleName: string().max(120),
  lastName: string().required('Last name is required.').max(120),
  avatarUrl: optionalUrl,
  city: string().required('City is required.').max(120),
})

const talentSchema = commonSchema
  .concat(
    object({
      phoneNumber: string().required('Phone number is required.').max(32),
      about: string().max(10000),
      experienceLevel: string().required('Experience level is required.'),
      yearsOfExperience: string()
        .required('Years of experience is required.')
        .test(
          'non-negative-int',
          (value) => !value || /^\d+$/.test(value),
        ),
      workMode: string(),
      availability: string(),
      currentIndustry: string().required('Current industry is required.'),
      currentProfession: string().required('Current profession is required.'),
      skills: array()
        .of(string())
        .required()
        .min(1, 'Add at least one skill.'),
      desiredRoles: array().of(string()),
      desiredJobTypes: array().of(string()),
      resumeUrl: string().max(512),
      linkedInUrl: optionalUrl,
      githubUrl: optionalUrl,
      portfolioUrl: optionalUrl,
      workExperience: array().of(workExperienceSchema),
      educationLevel: string().required('Highest education level is required.'),
      educationHistory: array().of(educationEntrySchema),
    }),
  )
  .test('education-cross-field', 'Education requirements not met.', function (values: any) {
    if (!values) return true
    const { educationLevel, educationHistory } = values
    if (!educationLevel) return true // educationLevel required rule handles this
    if (!educationHistory || educationHistory.length === 0) {
      if (educationLevel === 'HighSchool') return true
      return (
        this.createError({
          message:
            educationLevel === 'Masters'
              ? 'Add at least one Masters and one Bachelors education entry.'
              : 'Add at least one Bachelors education entry.',
          path: 'educationHistory',
        })
      )
    }
    const levels = educationHistory
      .filter((e: any) => e && typeof e === 'object')
      .map((e: any) => e.level)
      .filter(Boolean)
    if (educationLevel === 'Masters') {
      if (!levels.includes('Masters') || !levels.includes('Bachelors')) {
        return this.createError({
          message: 'Add at least one Masters and one Bachelors education entry.',
          path: 'educationHistory',
        })
      }
    } else if (educationLevel === 'Bachelors') {
      if (!levels.includes('Bachelors')) {
        return this.createError({
          message: 'Add at least one Bachelors education entry.',
          path: 'educationHistory',
        })
      }
    }
    return true
  })

interface ProfileFormValues {
  // Common
  firstName: string
  middleName: string
  lastName: string
  avatarUrl: string
  city: string
  // Talent
  about: string
  experienceLevel: string
  yearsOfExperience: string
  desiredRoles: string[]
  skills: string[]
  desiredJobTypes: string[]
  workMode: string
  availability: string
  resumeUrl: string
  linkedInUrl: string
  githubUrl: string
  portfolioUrl: string
  // Identity / personal
  phoneNumber: string
  address: string
  // Work experience
  workExperience: WorkExperienceEntry[]
  // Education
  educationLevel: string
  educationHistory: EducationEntry[]
  // Professional context
  currentIndustry: string
  currentProfession: string
  preferredLocations: string[]
  // Privacy
  profileVisibility: Record<string, boolean>
  skillVisibility: Record<string, boolean>
}

const initialValues: ProfileFormValues = {
  firstName: '',
  middleName: '',
  lastName: '',
  avatarUrl: '',
  city: 'Addis Ababa',
  about: '',
  experienceLevel: '',
  yearsOfExperience: '',
  desiredRoles: [],
  skills: [],
  desiredJobTypes: [],
  workMode: 'Hybrid',
  availability: '',
  resumeUrl: '',
  linkedInUrl: '',
  githubUrl: '',
  portfolioUrl: '',
  phoneNumber: '',
  address: '',
  workExperience: [],
  educationLevel: '',
  educationHistory: [],
  currentIndustry: '',
  currentProfession: '',
  preferredLocations: [],
  profileVisibility: { ...DEFAULT_VISIBILITY },
  skillVisibility: {},
}

/* ------------------------------------------------------------ Field bits */

function Field({
  label,
  full,
  hint,
  error,
  visibilityKey,
  visibility,
  onToggleVisibility,
  children,
}: {
  label: string
  full?: boolean
  hint?: string
  error?: string
  visibilityKey?: string
  visibility?: Record<string, boolean>
  onToggleVisibility?: (key: string) => void
  children: React.ReactNode
}) {
  const isVisible = visibilityKey && visibility ? (visibility[visibilityKey] ?? true) : true
  return (
    <div className={cn(full && 'sm:col-span-2')}>
      <div className="flex items-center justify-between gap-2">
        <label className="font-label-sm text-label-sm font-medium text-on-surface">
          {label}
        </label>
        {visibilityKey && visibility && onToggleVisibility && (
          <button
            type="button"
            role="switch"
            aria-checked={isVisible}
            aria-label={`Share ${label} when applying`}
            onClick={() => onToggleVisibility(visibilityKey)}
            className={cn(
              'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors',
              isVisible ? 'bg-primary' : 'bg-surface-variant',
            )}
          >
            <span
              className={cn(
                'inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
                isVisible ? 'translate-x-4' : 'translate-x-0.5',
              )}
            />
          </button>
        )}
      </div>
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
  hasError,
}: {
  value: string[]
  onChange: (next: string[]) => void
  placeholder?: string
  hasError?: boolean
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
    <div
      className={cn(
        'flex flex-wrap items-center gap-1.5 rounded-lg bg-surface-container-lowest px-2 py-1.5 transition-colors',
        hasError
          ? 'border border-error ring-1 ring-error'
          : 'border border-outline-variant focus-within:border-primary focus-within:ring-1 focus-within:ring-primary',
      )}
    >
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

function SkillAddButton({ onAdd }: { onAdd: (skill: string) => void }) {
  const [open, setOpen] = useState(false)
  const [value, setValue] = useState('')

  const submit = () => {
    const trimmed = value.trim()
    if (trimmed) onAdd(trimmed)
    setValue('')
    setOpen(false)
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex items-center justify-center gap-1 rounded-lg border border-dashed border-outline-variant bg-surface-container-lowest px-2.5 py-2 font-label-sm text-label-sm text-on-surface-variant transition-colors hover:border-primary hover:text-primary"
      >
        <span className="material-symbols-outlined text-[14px]">add</span>
        Add
      </button>
    )
  }

  return (
    <div className="flex items-center gap-1 rounded-lg border border-primary bg-surface-container-lowest px-2 py-1">
      <input
        autoFocus
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') { e.preventDefault(); submit() }
          if (e.key === 'Escape') { setOpen(false); setValue('') }
        }}
        onBlur={submit}
        placeholder="Skill name"
        className="min-w-0 flex-1 bg-transparent px-1 py-0.5 font-label-sm text-label-sm text-on-surface placeholder:text-on-surface-variant/60 focus:outline-none"
      />
    </div>
  )
}

/* -------------------------------------------------------------- The form */

const COPY: Record<RequiredRole, { blurb: string }> = {
  Talent: {
    blurb:
      'Your professional details — used to match you with the right roles.',
  },
  Admin: {
    blurb: 'Your account details. Admins have no role-specific profile section.',
  },
}

export default function ProfileForm({ role }: { role: RequiredRole }) {
  const auth = useRequireRole(role)
  const navigate = useNavigate()
  const location = useLocation()
  const { settings } = useSettingsQuery()
  const currentSectorIds = settings.forYou?.sectorIds ?? []
  const autoParsedHandledRef = useRef(false)
  const fromOnboardingRef = useRef(false)
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [avatarUploading, setAvatarUploading] = useState(false)
  const [photoModalOpen, setPhotoModalOpen] = useState(false)
  const [resumeUploading, setResumeUploading] = useState(false)
  const [resumeParsing, setResumeParsing] = useState(false)
  const [resumeNotice, setResumeNotice] = useState<{
    kind: 'filled' | 'empty'
    message: string
  } | null>(null)
  const [reviewProfileData, setReviewProfileData] = useState<{ profile: ParsedResumeProfile; fieldsFound: number } | null>(null)
  const { showToast } = useToast()
  const [validationModalOpen, setValidationModalOpen] = useState(false)
  const [validationErrors, setValidationErrors] = useState<{ field: string; message: string }[]>([])
  const avatarInputRef = useRef<HTMLInputElement>(null)
  const resumeInputRef = useRef<HTMLInputElement>(null)

  const isTalent = role === 'Talent'

  type ProfileTab = 'all' | 'personal' | 'professional' | 'skills' | 'experience' | 'education' | 'links'
  const [activeTab, setActiveTab] = useState<ProfileTab>(() => {
    if (typeof window !== 'undefined') {
      const hash = window.location.hash.toLowerCase()
      if (hash.includes('personal')) return 'personal'
      if (hash.includes('professional')) return 'professional'
      if (hash.includes('skill')) return 'skills'
      if (hash.includes('experience')) return 'experience'
      if (hash.includes('education')) return 'education'
      if (hash.includes('link') || hash.includes('resume')) return 'links'
    }
    return 'all'
  })

  const validationSchema = role === 'Talent' ? talentSchema : commonSchema

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
        // Parse work experience JSON
        let parsedWorkExperience: WorkExperienceEntry[] = []
        try {
          if (p.talent?.workExperience && p.talent.workExperience !== '[]') {
            parsedWorkExperience = JSON.parse(p.talent.workExperience)
          }
        } catch { /* keep empty */ }

        // Parse education history JSON
        let parsedEducation: EducationEntry[] = []
        try {
          if (p.talent?.educationHistory && p.talent.educationHistory !== '[]') {
            parsedEducation = JSON.parse(p.talent.educationHistory)
          }
        } catch { /* keep empty */ }

        // Parse visibility JSON
        let parsedVisibility = { ...DEFAULT_VISIBILITY }
        try {
          if (p.talent?.profileVisibility && p.talent.profileVisibility !== '{}') {
            parsedVisibility = { ...DEFAULT_VISIBILITY, ...JSON.parse(p.talent.profileVisibility) }
          }
        } catch { /* keep defaults */ }

        // Parse skill visibility JSON
        let parsedSkillVisibility: Record<string, boolean> = {}
        try {
          if (p.talent?.skillVisibility && p.talent.skillVisibility !== '{}') {
            parsedSkillVisibility = JSON.parse(p.talent.skillVisibility)
          }
        } catch { /* keep empty — all skills visible by default */ }

        // Parse preferred locations JSON
        let parsedLocations: string[] = []
        try {
          if (p.talent?.preferredLocations && p.talent.preferredLocations !== '[]') {
            parsedLocations = JSON.parse(p.talent.preferredLocations)
          }
        } catch { /* keep empty */ }

        formik.resetForm({
          values: {
            firstName: p.firstName,
            middleName: p.middleName ?? '',
            lastName: p.lastName,
            avatarUrl: p.avatarUrl,
            city: p.city || 'Addis Ababa',
            about: p.talent?.about ?? '',
            experienceLevel: p.talent?.experienceLevel ?? '',
            yearsOfExperience:
              p.talent?.yearsOfExperience == null
                ? ''
                : String(p.talent.yearsOfExperience),
            desiredRoles: p.talent?.desiredRoles ?? [],
            skills: p.talent?.skills ?? [],
            desiredJobTypes: p.talent?.desiredJobTypes ?? [],
            workMode: p.talent?.workMode || 'Hybrid',
            availability: p.talent?.availability ?? '',
            resumeUrl: p.talent?.resumeUrl ?? '',
            linkedInUrl: p.talent?.linkedInUrl ?? '',
            githubUrl: p.talent?.githubUrl ?? '',
            portfolioUrl: p.talent?.portfolioUrl ?? '',
            phoneNumber: p.talent?.phoneNumber ?? '',
            address: p.talent?.address ?? '',
            workExperience: parsedWorkExperience,
            educationLevel: p.talent?.educationLevel ?? '',
            educationHistory: parsedEducation,
            currentIndustry: p.talent?.currentIndustry ?? '',
            currentProfession: p.talent?.currentProfession ?? '',
            preferredLocations: parsedLocations,
            profileVisibility: parsedVisibility,
            skillVisibility: parsedSkillVisibility,
          },
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.status, role])

  // When coming from the onboarding modal with an uploaded & parsed CV:
  useEffect(() => {
    if (loading) return
    const state = location.state as {
      fromOnboarding?: boolean
      autoParsedResume?: {
        resumeUrl: string
        parsedProfile: ParsedResumeProfile
        fieldsFound: number
      }
    } | null

    if (state?.autoParsedResume && !autoParsedHandledRef.current) {
      autoParsedHandledRef.current = true
      if (state.fromOnboarding) {
        fromOnboardingRef.current = true
      }
      const { resumeUrl, parsedProfile, fieldsFound } = state.autoParsedResume
      formik.setFieldValue('resumeUrl', resumeUrl)
      setReviewProfileData({ profile: parsedProfile, fieldsFound })
      // Clear router location state so refreshing does not re-open modal
      navigate(location.pathname, { replace: true, state: {} })
    }
  }, [loading, location.state, formik, navigate, location.pathname])

  const handleSave = async (values: ProfileFormValues) => {
    // Guard: never save while profile data is still loading — this prevents
    // wiping the database with empty Formik initial values on page load / hot-reload.
    if (loading) return
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setSubmitError('You are not signed in.')
      return
    }
    formik.setSubmitting(true)
    setSubmitError(null)

    const payload: UpdateProfileRequest = {
      firstName: values.firstName.trim(),
      middleName: values.middleName.trim(),
      lastName: values.lastName.trim(),
      avatarUrl: values.avatarUrl.trim(),
      city: values.city.trim(),
    }
    if (isTalent) {
      payload.talent = {
        about: values.about.trim(),
        experienceLevel: values.experienceLevel || null,
        yearsOfExperience:
          values.yearsOfExperience === ''
            ? null
            : Number(values.yearsOfExperience),
        desiredRoles: values.desiredRoles,
        skills: values.skills,
        desiredJobTypes: values.desiredJobTypes,
        workMode: values.workMode || null,
        availability: values.availability || null,
        resumeUrl: values.resumeUrl.trim(),
        linkedInUrl: values.linkedInUrl.trim(),
        githubUrl: values.githubUrl.trim(),
        portfolioUrl: values.portfolioUrl.trim(),
        phoneNumber: values.phoneNumber.trim() || '',
        address: values.address.trim() || '',
        workExperience: JSON.stringify(values.workExperience),
        educationLevel: values.educationLevel || '',
        educationHistory: JSON.stringify(values.educationHistory),
        currentIndustry: values.currentIndustry.trim() || '',
        currentProfession: values.currentProfession.trim() || '',
        preferredLocations: JSON.stringify(values.preferredLocations),
        profileVisibility: JSON.stringify(values.profileVisibility),
        skillVisibility: JSON.stringify(values.skillVisibility),
      }
    }

    try {
      const updated = await updateProfile(tokens.accessToken, payload)
      setProfile(updated)
      showToast('Profile saved.')
      // Reset formik's dirty tracking so the Unsaved Changes guard
      // doesn't fire after a successful save.
      formik.resetForm({ values: formik.values })
    } catch (err) {
      setSubmitError(getApiErrorMessage(err))
    } finally {
      formik.setSubmitting(false)
    }
  }

  const saveFromDialog = useCallback(() => handleSave(formik.values), [formik.values])

  /** Flatten Formik errors into a flat list of { field, message } for the modal. */
  const flattenErrors = (
    errs: Record<string, any>,
    prefix = '',
  ): { field: string; message: string }[] => {
    const result: { field: string; message: string }[] = []
    for (const [key, val] of Object.entries(errs)) {
      const label = prefix ? `${prefix} > ${FIELD_LABELS[key] ?? key}` : (FIELD_LABELS[key] ?? key)
      if (typeof val === 'string') {
        result.push({ field: label, message: val })
      } else if (Array.isArray(val)) {
        val.forEach((item: any, idx: number) => {
          if (item && typeof item === 'object') {
            result.push(
              ...flattenErrors(item, `${label} #${idx + 1}`),
            )
          } else if (typeof item === 'string') {
            result.push({ field: `${label} #${idx + 1}`, message: item })
          }
        })
      } else if (val && typeof val === 'object') {
        result.push(...flattenErrors(val, label))
      }
    }
    return result
  }

  /** Custom save handler that shows validation errors instead of silently blocking. */
  const handleSaveClick = async () => {
    // Guard: never save while profile data is still loading
    if (loading) return
    const errors = await formik.validateForm()
    if (Object.keys(errors).length > 0) {
      // Mark all fields touched so inline error messages appear
      const allTouched: Record<string, boolean> = {}
      for (const key of Object.keys(formik.values)) {
        allTouched[key] = true
      }
      formik.setTouched(allTouched as any)
      // Flatten errors into a user-friendly list and open the modal
      const flat = flattenErrors(errors as Record<string, any>)
      setValidationErrors(flat)
      setValidationModalOpen(true)
      return
    }
    setSubmitError(null)
    handleSave(formik.values)
  }

  const {
    dialogOpen,
    saving: dialogSaving,
    handleSave: confirmSave,
    handleDiscard,
    handleCancel,
  } = useUnsavedChanges({
    // Only block navigation when data has loaded AND form is dirty.
    // While loading, Formik has empty initial values — saving those
    // would wipe the user's real profile data from the database.
    isDirty: !loading && formik.dirty,
    onSave: saveFromDialog,
  })

  const toggleJobType = (value: string) => {
    const next = formik.values.desiredJobTypes.includes(value)
      ? formik.values.desiredJobTypes.filter((t) => t !== value)
      : [...formik.values.desiredJobTypes, value]
    formik.setFieldValue('desiredJobTypes', next)
  }

  const toggleVisibility = (key: string) => {
    formik.setFieldValue(
      'profileVisibility',
      { ...formik.values.profileVisibility, [key]: !formik.values.profileVisibility[key] },
    )
  }

  const toggleSkillVisibility = (skill: string) => {
    formik.setFieldValue(
      'skillVisibility',
      { ...formik.values.skillVisibility, [skill]: !(formik.values.skillVisibility[skill] ?? true) },
    )
  }

  const handleAvatarUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    // Validate
    if (!file.type.startsWith('image/')) {
      setSubmitError('Please upload an image file')
      return
    }
    if (file.size > 5 * 1024 * 1024) {
      setSubmitError('File size must be less than 5MB')
      return
    }
    setAvatarUploading(true)
    setSubmitError(null)
    try {
      const { uploadFile } = await import('../../../api/fileUpload.ts')
      const url = await uploadFile(file, 'avatar')
      formik.setFieldValue('avatarUrl', url)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setAvatarUploading(false)
      if (avatarInputRef.current) avatarInputRef.current.value = ''
    }
  }

  /**
   * Drops parsed resume fields into the form without saving anything. Only the
   * fields that were actually found are touched — everything else keeps
   * whatever the user already had. Also persists any chosen For You feed sectors.
   */
  const applyParsedResume = async (
    parsed: ParsedResumeProfile,
    selectedSectorIds?: string[],
  ) => {
    if (parsed.about) formik.setFieldValue('about', parsed.about)
    if (parsed.skills.length > 0) formik.setFieldValue('skills', parsed.skills)
    if (parsed.experience.length > 0) {
      formik.setFieldValue('workExperience', parsed.experience)
    }
    if (parsed.education.length > 0) {
      formik.setFieldValue(
        'educationHistory',
        parsed.education.map((e) => ({ ...e, gpa: e.gpa || '' })),
      )
      // The form tracks the highest level in its own field, separate from each
      // entry's `level`.
      if (!formik.values.educationLevel) {
        formik.setFieldValue('educationLevel', parsed.education[0].level)
      }
    }
    if (parsed.experienceLevel) {
      formik.setFieldValue('experienceLevel', parsed.experienceLevel)
    }
    if (parsed.yearsOfExperience !== null) {
      formik.setFieldValue('yearsOfExperience', String(parsed.yearsOfExperience))
    }
    if (parsed.currentIndustry) {
      formik.setFieldValue('currentIndustry', parsed.currentIndustry)
    }
    // `headline` has no field of its own in this form and carries the same kind
    // of value as `currentProfession`, so it is only used as a fallback.
    const profession = parsed.currentProfession ?? parsed.headline
    if (profession) formik.setFieldValue('currentProfession', profession)

    // Persist For You feed sectors if provided
    if (selectedSectorIds && selectedSectorIds.length > 0) {
      const tokens = getStoredAuthTokens()
      if (tokens) {
        try {
          await patchSettingsCategory(
            tokens.accessToken,
            'forYou',
            JSON.stringify({
              sectorIds: selectedSectorIds,
              onboardingCompleted: true,
            }),
          )
        } catch (err) {
          console.error('Failed to update For You feed sectors:', err)
        }
      }
    }
  }

  /**
   * Sends a stored resume key to the API for extraction and pre-fills the form
   * with whatever comes back. Called after an upload, and on demand from the
   * resume section.
   *
   * Every failure mode — AI service unreachable, scanned/image-only PDF, no
   * recognisable sections — ends in the same calm message rather than an error
   * state: the resume is optional, the profile form is not.
   */
  const parseUploadedResume = async (resumeKey: string) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setResumeParsing(true)
    setResumeNotice(null)
    try {
      const result = await parseResume(tokens.accessToken, resumeKey)
      const parsed = result.profile
      if (result.success && parsed && result.fieldsFound > 0) {
        setReviewProfileData({ profile: parsed, fieldsFound: result.fieldsFound })
      } else {
        setResumeNotice({
          kind: 'empty',
          message: "We couldn't read your resume. Please fill your profile manually.",
        })
      }
    } catch {
      setResumeNotice({
        kind: 'empty',
        message: "We couldn't read your resume. Please fill your profile manually.",
      })
    } finally {
      setResumeParsing(false)
    }
  }

  const handleResumeUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    if (file.type !== 'application/pdf') {
      setSubmitError('Please upload a PDF file')
      return
    }
    if (file.size > 10 * 1024 * 1024) {
      setSubmitError('File size must be less than 10MB')
      return
    }
    setResumeUploading(true)
    setResumeNotice(null)
    setSubmitError(null)
    let uploadedKey = ''
    try {
      const { uploadFile } = await import('../../../api/fileUpload.ts')
      uploadedKey = await uploadFile(file, 'resume')
      formik.setFieldValue('resumeUrl', uploadedKey)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Upload failed')
      return
    } finally {
      setResumeUploading(false)
      if (resumeInputRef.current) resumeInputRef.current.value = ''
    }

    // The upload has already succeeded. Parsing is a convenience layered on top
    // of it and must never turn a successful upload into an error.
    await parseUploadedResume(uploadedKey)
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

  /* ——— Talent: count fields filled per section for accordion badges ——— */
  const personalFilled = [
    formik.values.firstName,
    formik.values.lastName,
    formik.values.city,
    formik.values.phoneNumber,
    formik.values.avatarUrl,
    formik.values.middleName,
    formik.values.address,
  ].filter(Boolean).length

  const professionalFilled = [
    formik.values.about,
    formik.values.experienceLevel,
    formik.values.yearsOfExperience,
    formik.values.workMode,
    formik.values.availability,
    formik.values.currentIndustry,
    formik.values.currentProfession,
  ].filter(Boolean).length

  const skillsFilled = [
    formik.values.skills.length > 0 ? 'skills' : '',
    formik.values.desiredRoles.length > 0 ? 'roles' : '',
    formik.values.desiredJobTypes.length > 0 ? 'jobTypes' : '',
  ].filter(Boolean).length

  const educationFilled = [
    formik.values.educationLevel,
    formik.values.educationHistory.length > 0 ? 'history' : '',
  ].filter(Boolean).length

  const linksFilled = [
    formik.values.resumeUrl,
    formik.values.linkedInUrl,
    formik.values.githubUrl,
    formik.values.portfolioUrl,
  ].filter(Boolean).length

  const fullName = [formik.values.firstName, formik.values.middleName, formik.values.lastName]
    .filter(Boolean)
    .join(' ')

  /** Extract a human-readable filename from the resume URL/key.
   *  Keys look like: resumes/{userId}/{timestamp}_{originalName}.pdf
   *  We strip the timestamp prefix to show the original name. */
  const resumeDisplayName = (() => {
    const raw = formik.values.resumeUrl
    if (!raw) return ''
    const segments = raw.split('/')
    const last = segments[segments.length - 1] || raw
    // Strip the timestamp prefix (e.g. 20260825_193848_)
    const cleaned = last.replace(/^\d{8}_\d{6}_/, '').replace(/\.pdf$/i, '')
    return cleaned || last
  })()

  const hasCv = Boolean(formik.values.resumeUrl?.trim())

  const avatarInitials = fullName.split(' ').map((w) => w[0]).filter(Boolean).slice(0, 2).join('').toUpperCase()

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

  if (loading) {
    return (
      <DashboardShell role={role} authUser={auth.user}>
        <div className="flex min-h-[30svh] items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      </DashboardShell>
    )
  }

  return (
    <DashboardShell role={role} authUser={auth.user}>
      <div className="w-full pb-16">
        {/* Header Block */}
        <div className="flex flex-col gap-4 pb-6 border-b border-surface-variant">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h1 className="font-headline-lg text-headline-lg font-bold text-on-surface tracking-tight">
                {isTalent ? 'Talent Profile' : 'Profile'}
              </h1>
              <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                {COPY[role].blurb}
              </p>
            </div>
          </div>

          {/* Horizontal Pill Tab Bar */}
          <nav aria-label="Profile sections" className="flex items-center gap-2 overflow-x-auto pt-2 pb-1 no-scrollbar">
            <button
              type="button"
              onClick={() => setActiveTab('all')}
              className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                activeTab === 'all'
                  ? 'bg-primary text-on-primary shadow-sm font-semibold'
                  : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
              }`}
            >
              All Sections
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('personal')}
              className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                activeTab === 'personal'
                  ? 'bg-primary text-on-primary shadow-sm font-semibold'
                  : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
              }`}
            >
              Personal Info
            </button>

            {isTalent && (
              <>
                <button
                  type="button"
                  onClick={() => setActiveTab('professional')}
                  className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                    activeTab === 'professional'
                      ? 'bg-primary text-on-primary shadow-sm font-semibold'
                      : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
                  }`}
                >
                  Professional Profile
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('skills')}
                  className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                    activeTab === 'skills'
                      ? 'bg-primary text-on-primary shadow-sm font-semibold'
                      : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
                  }`}
                >
                  Skills &amp; Roles
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('experience')}
                  className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                    activeTab === 'experience'
                      ? 'bg-primary text-on-primary shadow-sm font-semibold'
                      : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
                  }`}
                >
                  Work Experience
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('education')}
                  className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                    activeTab === 'education'
                      ? 'bg-primary text-on-primary shadow-sm font-semibold'
                      : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
                  }`}
                >
                  Education
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('links')}
                  className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                    activeTab === 'links'
                      ? 'bg-primary text-on-primary shadow-sm font-semibold'
                      : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
                  }`}
                >
                  Links &amp; Resume
                </button>
              </>
            )}
          </nav>
        </div>

        {/* Completion & CV Intake Banner */}
        {completion && (isTalent || !completion.isComplete) && (
          <div className="mt-6 rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-sm overflow-hidden">
            <div
              className={cn(
                'grid grid-cols-1',
                isTalent && 'md:grid-cols-2 divide-y md:divide-y-0 md:divide-x divide-surface-variant',
              )}
            >
              {/* Left half: Profile Strength */}
              <div className="p-5 sm:p-6 flex flex-col justify-between gap-4">
                <div>
                  <div className="flex items-center justify-between gap-3">
                    <span className="flex items-center gap-2 font-label-md text-label-md font-semibold text-on-surface">
                      <span className="material-symbols-outlined text-primary text-lg">
                        trending_up
                      </span>
                      Profile Strength: {completion.percentComplete}%
                    </span>
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {completion.isComplete
                        ? 'Profile complete'
                        : `${completion.missingFields.length} recommended ${completion.missingFields.length === 1 ? 'field' : 'fields'} left`}
                    </span>
                  </div>

                  {/* Progress bar */}
                  <div className="mt-3 h-2 overflow-hidden rounded-full bg-surface-container-low">
                    <div
                      className={cn(
                        'h-full rounded-full transition-all duration-500',
                        completion.percentComplete === 100 ? 'bg-emerald-500' : 'bg-primary',
                      )}
                      style={{ width: `${completion.percentComplete}%` }}
                    />
                  </div>
                </div>

                {completion.missingFields.length > 0 ? (
                  <div className="flex flex-wrap gap-1.5 items-center">
                    <span className="font-label-xs text-label-xs text-on-surface-variant/70 mr-1">To complete:</span>
                    {completion.missingFields.map((field) => (
                      <span
                        key={field}
                        className="rounded-full bg-surface-container-low border border-surface-variant px-2.5 py-0.5 font-label-xs text-label-xs text-on-surface-variant"
                      >
                        {MISSING_LABELS[field] ?? field}
                      </span>
                    ))}
                  </div>
                ) : (
                  <div className="flex items-center gap-1.5 text-emerald-600 dark:text-emerald-400 font-label-xs text-label-xs">
                    <span className="material-symbols-outlined text-[16px]">check_circle</span>
                    <span>All recommended fields completed</span>
                  </div>
                )}
              </div>

              {/* Right half: CV status & intake (Talent only) */}
              {isTalent && (
                <div className="p-5 sm:p-6 flex flex-col justify-between gap-4 bg-surface-container-low/20">
                  <div className="flex items-center gap-4 sm:gap-5">
                    {/* Large 80px Hero Tile with 48px cloud_upload Icon */}
                    <div
                      onClick={() => resumeInputRef.current?.click()}
                      className={cn(
                        'w-20 h-20 rounded-2xl flex items-center justify-center shrink-0 cursor-pointer transition-all duration-200 hover:scale-[1.03] active:scale-[0.98]',
                        hasCv
                          ? 'bg-primary/10 text-primary border-2 border-primary/20 hover:border-primary/40 hover:bg-primary/15'
                          : 'bg-primary text-on-primary shadow-md hover:shadow-lg hover:opacity-95',
                      )}
                      title={hasCv ? 'Click to update CV' : 'Click to upload CV'}
                    >
                      <span className="material-symbols-outlined text-[48px] select-none">
                        cloud_upload
                      </span>
                    </div>

                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-label-lg text-label-lg font-semibold text-on-surface truncate">
                          {hasCv ? 'Update CV' : 'Upload CV'}
                        </h3>
                        {hasCv && (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300 border border-emerald-200/50 dark:border-emerald-800/40 shrink-0">
                            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
                            Uploaded
                          </span>
                        )}
                      </div>
                      <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant leading-snug">
                        {hasCv
                          ? 'Keep your profile skills and AI job matching up to date.'
                          : 'Auto-fill your profile and unlock instant AI job match scores.'}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center justify-between gap-3 pt-1">
                    <span className="font-label-xs text-label-xs text-on-surface-variant/70">
                      PDF format • max 10MB
                    </span>
                    <button
                      type="button"
                      onClick={() => resumeInputRef.current?.click()}
                      disabled={resumeUploading || resumeParsing}
                      className={cn(
                        'inline-flex items-center gap-2 rounded-xl px-4 py-2 font-label-md text-label-md transition-all shadow-sm shrink-0',
                        hasCv
                          ? 'border border-outline-variant bg-surface-container-lowest text-on-surface hover:border-primary hover:text-primary hover:bg-surface-container-low font-medium'
                          : 'bg-primary text-on-primary hover:opacity-90 font-semibold',
                      )}
                    >
                      {resumeUploading || resumeParsing ? (
                        <>
                          <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                          <span>{resumeParsing ? 'Analyzing CV…' : 'Uploading…'}</span>
                        </>
                      ) : (
                        <>
                          <span className="material-symbols-outlined text-[20px]">
                            cloud_upload
                          </span>
                          <span>{hasCv ? 'Update CV' : 'Upload CV'}</span>
                        </>
                      )}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Resume parse notice */}
        {isTalent && resumeNotice && (
          <div
            role="status"
            className={cn(
              'mt-6 flex items-start gap-2.5 rounded-2xl border px-5 py-4 font-label-md text-label-md shadow-sm',
              resumeNotice.kind === 'filled'
                ? 'border-success/30 bg-surface-container-lowest text-on-surface'
                : 'border-amber-200 bg-amber-50 text-amber-900',
            )}
          >
            <span className="material-symbols-outlined mt-0.5 text-lg text-success">
              {resumeNotice.kind === 'filled' ? 'check_circle' : 'info'}
            </span>
            <span className="min-w-0 flex-1">{resumeNotice.message}</span>
            <button
              type="button"
              onClick={() => setResumeNotice(null)}
              aria-label="Dismiss"
              className="shrink-0 rounded-lg p-0.5 text-on-surface-variant transition-colors hover:bg-surface-container-low"
            >
              <span className="material-symbols-outlined text-[18px]">close</span>
            </button>
          </div>
        )}

        {submitError && (
          <div
            role="alert"
            className="mt-6 flex items-start gap-2.5 rounded-2xl border border-error/30 bg-error-container/40 p-4 font-label-md text-label-md text-on-error-container"
          >
            <span className="material-symbols-outlined mt-0.5 text-lg text-error">
              error
            </span>
            <span>{submitError}</span>
          </div>
        )}

        {loadError && (
          <div
            role="alert"
            className="mt-6 rounded-2xl border border-error/30 bg-error-container/40 p-4 font-label-md text-label-md text-on-error-container"
          >
            {loadError}
          </div>
        )}

        <form
          onSubmit={formik.handleSubmit}
          noValidate
          className="w-full mt-6"
        >
          {/* Hidden file input for resume parsing/uploading */}
          <input
            ref={resumeInputRef}
            type="file"
            accept="application/pdf"
            onChange={handleResumeUpload}
            className="hidden"
          />

          {/* Main Profile Canvas - Unified divided container filling full width */}
          <div className="w-full rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-sm divide-y divide-surface-variant overflow-hidden">
            {/* ── Section 1: Personal information ── */}
            {(activeTab === 'all' || activeTab === 'personal') && (
              <div id="personal" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Personal Information
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    Your identity, profile photo, and location details shown across the platform.
                  </p>
                  <div className="mt-4 flex items-center gap-2">
                    <span className={`inline-block w-2 h-2 rounded-full ${personalFilled === 7 ? 'bg-emerald-500' : 'bg-primary'}`} />
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {personalFilled} of 7 fields completed
                    </span>
                  </div>
                </div>

                <div className="lg:col-span-8 flex flex-col gap-6">
                  {/* Photo Row */}
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl bg-surface-container-low border border-surface-variant/60">
                    <div className="flex items-center gap-4">
                      {formik.values.avatarUrl ? (
                        <img
                          className="w-16 h-16 rounded-full object-cover border border-surface-variant shadow-sm"
                          src={formik.values.avatarUrl}
                          alt={fullName}
                        />
                      ) : (
                        <span className="flex w-16 h-16 items-center justify-center rounded-full bg-primary-container/60 font-headline-md text-headline-md font-semibold text-primary shadow-sm">
                          {avatarInitials || 'U'}
                        </span>
                      )}
                      <div>
                        <p className="font-label-md text-label-md font-semibold text-on-surface">Profile Photo</p>
                        <p className="font-label-sm text-label-sm text-on-surface-variant/70">JPEG, PNG, WebP or GIF · Max 5MB</p>
                      </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                      <input
                        ref={avatarInputRef}
                        type="file"
                        accept="image/jpeg,image/png,image/webp,image/gif"
                        onChange={handleAvatarUpload}
                        className="hidden"
                      />
                      <button
                        type="button"
                        onClick={() => avatarInputRef.current?.click()}
                        disabled={avatarUploading}
                        className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-1.5 font-label-sm text-label-sm text-on-surface hover:bg-surface-container transition-colors disabled:opacity-50"
                      >
                        {avatarUploading ? (
                          <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                        ) : (
                          <span className="material-symbols-outlined text-base">upload</span>
                        )}
                        <span>{formik.values.avatarUrl ? 'Change' : 'Upload'}</span>
                      </button>
                      {formik.values.avatarUrl && (
                        <>
                          <button
                            type="button"
                            onClick={() => setPhotoModalOpen(true)}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-1.5 font-label-sm text-label-sm text-on-surface hover:bg-surface-container transition-colors"
                          >
                            <span className="material-symbols-outlined text-base">visibility</span>
                            <span>View</span>
                          </button>
                          <button
                            type="button"
                            onClick={() => formik.setFieldValue('avatarUrl', '')}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-error/30 bg-error/10 px-3.5 py-1.5 font-label-sm text-label-sm text-error hover:bg-error hover:text-white transition-colors"
                          >
                            <span className="material-symbols-outlined text-base">delete</span>
                            <span>Remove</span>
                          </button>
                        </>
                      )}
                      <button
                        type="button"
                        role="switch"
                        aria-checked={formik.values.profileVisibility.avatar ?? true}
                        aria-label="Toggle profile picture visibility"
                        onClick={() => toggleVisibility('avatar')}
                        className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-label-sm text-label-sm text-on-surface hover:bg-surface-container transition-colors"
                      >
                        <span
                          className={cn(
                            'relative inline-flex h-4 w-7 shrink-0 cursor-pointer rounded-full transition-colors',
                            (formik.values.profileVisibility.avatar ?? true) ? 'bg-primary' : 'bg-surface-variant',
                          )}
                        >
                          <span
                            className={cn(
                              'inline-block h-3 w-3 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
                              (formik.values.profileVisibility.avatar ?? true) ? 'translate-x-3.5' : 'translate-x-0.5',
                            )}
                          />
                        </span>
                        <span>{(formik.values.profileVisibility.avatar ?? true) ? 'Visible' : 'Hidden'}</span>
                      </button>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <Field
                      label="First name *"
                      error={formik.touched.firstName ? formik.errors.firstName : undefined}
                    >
                      <input
                        name="firstName"
                        value={formik.values.firstName}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="Jane"
                        className={getInputClass(Boolean(formik.touched.firstName && formik.errors.firstName))}
                      />
                    </Field>

                    <Field
                      label="Middle name"
                      error={formik.touched.middleName ? formik.errors.middleName : undefined}
                    >
                      <input
                        name="middleName"
                        value={formik.values.middleName}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="e.g. Kebede"
                        className={getInputClass(Boolean(formik.touched.middleName && formik.errors.middleName))}
                      />
                    </Field>

                    <Field
                      label="Last name *"
                      error={formik.touched.lastName ? formik.errors.lastName : undefined}
                    >
                      <input
                        name="lastName"
                        value={formik.values.lastName}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="Doe"
                        className={getInputClass(Boolean(formik.touched.lastName && formik.errors.lastName))}
                      />
                    </Field>

                    <Field
                      label="Phone number *"
                      error={formik.touched.phoneNumber ? formik.errors.phoneNumber : undefined}
                      visibilityKey="phone"
                      visibility={formik.values.profileVisibility}
                      onToggleVisibility={toggleVisibility}
                    >
                      <input
                        name="phoneNumber"
                        value={formik.values.phoneNumber}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="+251 91 123 4567"
                        className={getInputClass(Boolean(formik.touched.phoneNumber && formik.errors.phoneNumber))}
                      />
                    </Field>

                    <Field
                      label="City *"
                      error={formik.touched.city ? formik.errors.city : undefined}
                    >
                      <select
                        name="city"
                        value={formik.values.city}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        className={getInputClass(Boolean(formik.touched.city && formik.errors.city))}
                      >
                        <option value="">Select a city</option>
                        {(formik.values.city && !ETHIOPIAN_CITIES.includes(formik.values.city)
                          ? [formik.values.city, ...ETHIOPIAN_CITIES]
                          : ETHIOPIAN_CITIES
                        ).map((city) => (
                          <option key={city} value={city}>
                            {city}
                          </option>
                        ))}
                      </select>
                    </Field>

                    <Field
                      label="Street address"
                      visibilityKey="address"
                      visibility={formik.values.profileVisibility}
                      onToggleVisibility={toggleVisibility}
                    >
                      <input
                        name="address"
                        value={formik.values.address}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="Bole Road, Addis Ababa"
                        className={inputClass}
                      />
                    </Field>
                  </div>
                </div>
              </div>
            )}

            {/* ── Section 2: Professional profile ── */}
            {isTalent && (activeTab === 'all' || activeTab === 'professional') && (
              <div id="professional" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Professional Profile
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    What you do and how you work — your career identity and overview.
                  </p>
                  <div className="mt-4 flex items-center gap-2">
                    <span className={`inline-block w-2 h-2 rounded-full ${professionalFilled === 6 ? 'bg-emerald-500' : 'bg-primary'}`} />
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {professionalFilled} of 6 fields completed
                    </span>
                  </div>
                </div>

                <div className="lg:col-span-8 flex flex-col gap-6">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <Field
                      label="Years of experience *"
                      error={formik.touched.yearsOfExperience ? formik.errors.yearsOfExperience : undefined}
                    >
                      <input
                        name="yearsOfExperience"
                        type="number"
                        min={0}
                        value={formik.values.yearsOfExperience}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="5"
                        className={getInputClass(Boolean(formik.touched.yearsOfExperience && formik.errors.yearsOfExperience))}
                      />
                    </Field>

                    <Field
                      label="Experience level *"
                      error={formik.touched.experienceLevel ? formik.errors.experienceLevel : undefined}
                    >
                      <select
                        name="experienceLevel"
                        value={formik.values.experienceLevel}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        className={getInputClass(Boolean(formik.touched.experienceLevel && formik.errors.experienceLevel))}
                      >
                        <option value="">Not specified</option>
                        {EXPERIENCE_LEVELS.map((level) => (
                          <option key={level} value={level}>{level}</option>
                        ))}
                      </select>
                    </Field>

                    <Field label="Work mode">
                      <select
                        name="workMode"
                        value={formik.values.workMode}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        className={getInputClass(Boolean(formik.touched.workMode && formik.errors.workMode))}
                      >
                        <option value="">Not specified</option>
                        {WORK_MODES.map((mode) => (
                          <option key={mode} value={mode}>{mode}</option>
                        ))}
                      </select>
                    </Field>

                    <Field label="Availability">
                      <select
                        name="availability"
                        value={formik.values.availability}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        className={getInputClass(Boolean(formik.touched.availability && formik.errors.availability))}
                      >
                        <option value="">Not specified</option>
                        {AVAILABILITIES.map((availability) => (
                          <option key={availability} value={availability}>{availability}</option>
                        ))}
                      </select>
                    </Field>

                    <Field
                      label="Current industry *"
                      error={formik.touched.currentIndustry ? formik.errors.currentIndustry : undefined}
                      visibilityKey="currentIndustry"
                      visibility={formik.values.profileVisibility}
                      onToggleVisibility={toggleVisibility}
                    >
                      <input
                        name="currentIndustry"
                        value={formik.values.currentIndustry}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="e.g. Technology, Finance"
                        className={getInputClass(Boolean(formik.touched.currentIndustry && formik.errors.currentIndustry))}
                      />
                    </Field>

                    <Field
                      label="Current profession *"
                      error={formik.touched.currentProfession ? formik.errors.currentProfession : undefined}
                      visibilityKey="currentProfession"
                      visibility={formik.values.profileVisibility}
                      onToggleVisibility={toggleVisibility}
                    >
                      <input
                        name="currentProfession"
                        value={formik.values.currentProfession}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="e.g. Software Engineer"
                        className={getInputClass(Boolean(formik.touched.currentProfession && formik.errors.currentProfession))}
                      />
                    </Field>
                  </div>

                  <Field full label="About" hint="A short professional bio employers will see.">
                    <RichTextEditor
                      value={formik.values.about}
                      onChange={(v) => formik.setFieldValue('about', v)}
                      placeholder="Tell employers about yourself, your background, and your aspirations…"
                    />
                  </Field>
                </div>
              </div>
            )}

            {/* ── Section 3: Skills & roles ── */}
            {isTalent && (activeTab === 'all' || activeTab === 'skills') && (
              <div id="skills" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Skills &amp; Roles
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    What you can do and what you're looking for in your next role.
                  </p>
                  <div className="mt-4 flex items-center gap-2">
                    <span className={`inline-block w-2 h-2 rounded-full ${skillsFilled > 0 ? 'bg-emerald-500' : 'bg-surface-variant'}`} />
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {skillsFilled} of 3 sections completed ({formik.values.skills.length} {formik.values.skills.length === 1 ? 'skill' : 'skills'})
                    </span>
                  </div>
                </div>

                <div className="lg:col-span-8 flex flex-col gap-6">
                  <div>
                    <div className="flex items-center justify-between gap-2 mb-2">
                      <label className="font-label-sm text-label-sm font-medium text-on-surface">
                        Skills *
                      </label>
                    </div>
                    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
                      {formik.values.skills.map((skill) => {
                        const isVisible = formik.values.skillVisibility[skill] ?? true
                        return (
                          <div
                            key={skill}
                            className={cn(
                              'flex items-center justify-between gap-1 rounded-lg border px-2.5 py-2 font-label-sm text-label-sm transition-all',
                              isVisible
                                ? 'border-primary-container/30 bg-primary-container/10 text-primary'
                                : 'border-surface-variant bg-surface-container-low text-on-surface-variant/50 line-through',
                            )}
                          >
                            <span className="truncate">{skill}</span>
                            <div className="flex items-center gap-0.5 shrink-0">
                              <button
                                type="button"
                                onClick={() => toggleSkillVisibility(skill)}
                                className="p-0.5 rounded transition-colors hover:bg-primary-container/20"
                                aria-label={isVisible ? `Hide ${skill}` : `Show ${skill}`}
                              >
                                <span className={cn(
                                  'material-symbols-outlined text-[14px]',
                                  isVisible ? 'text-primary' : 'text-on-surface-variant/40',
                                )}>
                                  {isVisible ? 'visibility' : 'visibility_off'}
                                </span>
                              </button>
                              <button
                                type="button"
                                onClick={() => {
                                  const next = formik.values.skills.filter((s) => s !== skill)
                                  formik.setFieldValue('skills', next)
                                }}
                                className="p-0.5 rounded transition-colors hover:bg-error-container/40"
                                aria-label={`Remove ${skill}`}
                              >
                                <span className="material-symbols-outlined text-[14px] text-on-surface-variant/50 hover:text-error">
                                  close
                                </span>
                              </button>
                            </div>
                          </div>
                        )
                      })}
                      <SkillAddButton onAdd={(skill) => {
                        if (!skill.trim()) return
                        if (!formik.values.skills.some((s) => s.toLowerCase() === skill.trim().toLowerCase())) {
                          formik.setFieldValue('skills', [...formik.values.skills, skill.trim()])
                        }
                      }} />
                    </div>
                  </div>

                  <Field label="Desired roles" hint="Press Enter or comma after each role.">
                    <TagInput
                      value={formik.values.desiredRoles}
                      onChange={(next) => formik.setFieldValue('desiredRoles', next)}
                      placeholder="e.g. Product Designer, Frontend Engineer"
                    />
                  </Field>

                  <Field full label="Desired job types">
                    <div className="mt-1.5 flex flex-wrap gap-2">
                      {JOB_TYPE_CHOICES.map((choice) => {
                        const checked = formik.values.desiredJobTypes.includes(choice.value)
                        return (
                          <button
                            key={choice.value}
                            type="button"
                            aria-pressed={checked}
                            onClick={() => toggleJobType(choice.value)}
                            className={cn(
                              'rounded-full px-4 py-2 font-label-md text-label-md font-medium transition-all',
                              checked
                                ? 'bg-primary text-on-primary shadow-sm'
                                : 'border border-outline-variant/60 bg-surface-container-lowest text-on-surface-variant hover:bg-surface-container-low hover:text-on-surface',
                            )}
                          >
                            {choice.label}
                          </button>
                        )
                      })}
                    </div>
                  </Field>
                </div>
              </div>
            )}

            {/* ── Section 4: Work experience ── */}
            {isTalent && (activeTab === 'all' || activeTab === 'experience') && (
              <div id="experience" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Work Experience
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    Your employment history and past roles — shown when you apply.
                  </p>
                  <div className="mt-4 flex items-center gap-2">
                    <span className={`inline-block w-2 h-2 rounded-full ${formik.values.workExperience.length > 0 ? 'bg-emerald-500' : 'bg-surface-variant'}`} />
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {formik.values.workExperience.length} {formik.values.workExperience.length === 1 ? 'position' : 'positions'} listed
                    </span>
                  </div>
                </div>

                <div className="lg:col-span-8 flex flex-col gap-6">
                  <div className="space-y-4">
                    {formik.values.workExperience.map((entry, idx) => (
                      <div
                        key={idx}
                        className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm"
                      >
                        <div className="flex items-center justify-between pb-3 border-b border-surface-variant/60 mb-3">
                          <span className="font-label-md text-label-md font-semibold text-on-surface">
                            {entry.title || entry.company || `Experience #${idx + 1}`}
                          </span>
                          <button
                            type="button"
                            onClick={() => {
                              const next = [...formik.values.workExperience]
                              next.splice(idx, 1)
                              formik.setFieldValue('workExperience', next)
                            }}
                            className="inline-flex items-center gap-1 text-on-surface-variant transition-colors hover:text-error text-label-sm font-medium"
                          >
                            <span className="material-symbols-outlined text-base">delete</span>
                            <span>Remove</span>
                          </button>
                        </div>

                        <div className="grid gap-4 sm:grid-cols-2">
                          <div>
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">Company name *</label>
                            <input
                              value={entry.company}
                              onChange={(e) => {
                                const next = [...formik.values.workExperience]
                                next[idx] = { ...next[idx], company: e.target.value }
                                formik.setFieldValue('workExperience', next)
                              }}
                              placeholder="e.g. Google"
                              className={getInputClass(Boolean(formik.touched.workExperience?.[idx]?.company && (formik.errors.workExperience as any)?.[idx]?.company))}
                            />
                            {formik.touched.workExperience?.[idx]?.company &&
                              (formik.errors.workExperience as any)?.[idx]?.company && (
                                <p className="mt-1 font-label-sm text-label-sm text-error">
                                  {(formik.errors.workExperience as any)[idx].company}
                                </p>
                              )}
                          </div>
                          <div>
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">Job title *</label>
                            <input
                              value={entry.title}
                              onChange={(e) => {
                                const next = [...formik.values.workExperience]
                                next[idx] = { ...next[idx], title: e.target.value }
                                formik.setFieldValue('workExperience', next)
                              }}
                              placeholder="e.g. Software Engineer"
                              className={getInputClass(Boolean(formik.touched.workExperience?.[idx]?.title && (formik.errors.workExperience as any)?.[idx]?.title))}
                            />
                            {formik.touched.workExperience?.[idx]?.title &&
                              (formik.errors.workExperience as any)?.[idx]?.title && (
                                <p className="mt-1 font-label-sm text-label-sm text-error">
                                  {(formik.errors.workExperience as any)[idx].title}
                                </p>
                              )}
                          </div>
                          <div>
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">Start date *</label>
                            <input
                              type="date"
                              value={entry.startDate}
                              onChange={(e) => {
                                const next = [...formik.values.workExperience]
                                next[idx] = { ...next[idx], startDate: e.target.value }
                                formik.setFieldValue('workExperience', next)
                              }}
                              className={getInputClass(Boolean(formik.touched.workExperience?.[idx]?.startDate && (formik.errors.workExperience as any)?.[idx]?.startDate))}
                            />
                            {formik.touched.workExperience?.[idx]?.startDate &&
                              (formik.errors.workExperience as any)?.[idx]?.startDate && (
                                <p className="mt-1 font-label-sm text-label-sm text-error">
                                  {(formik.errors.workExperience as any)[idx].startDate}
                                </p>
                              )}
                          </div>
                          <div>
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">End date *</label>
                            <input
                              type="date"
                              value={entry.endDate}
                              onChange={(e) => {
                                const next = [...formik.values.workExperience]
                                next[idx] = { ...next[idx], endDate: e.target.value }
                                formik.setFieldValue('workExperience', next)
                              }}
                              className={getInputClass(Boolean(formik.touched.workExperience?.[idx]?.endDate && (formik.errors.workExperience as any)?.[idx]?.endDate))}
                            />
                          </div>
                          <div className="sm:col-span-2">
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">Description *</label>
                            <RichTextEditor
                              value={entry.description}
                              onChange={(v) => {
                                const next = [...formik.values.workExperience]
                                next[idx] = { ...next[idx], description: v }
                                formik.setFieldValue('workExperience', next)
                              }}
                              placeholder="Describe your role, key achievements, and responsibilities…"
                            />
                            {formik.touched.workExperience?.[idx]?.description &&
                              (formik.errors.workExperience as any)?.[idx]?.description && (
                                <p className="mt-1 font-label-sm text-label-sm text-error">
                                  {(formik.errors.workExperience as any)[idx].description}
                                </p>
                              )}
                          </div>
                        </div>
                      </div>
                    ))}

                    <button
                      type="button"
                      onClick={() => {
                        const next = [...formik.values.workExperience, {
                          company: '', title: '', startDate: '', endDate: '', description: '',
                        }]
                        formik.setFieldValue('workExperience', next)
                      }}
                      className="w-full flex items-center justify-center gap-2 rounded-xl border border-dashed border-outline-variant bg-surface-container-low/40 p-4 font-label-md text-label-md text-primary font-medium transition-colors hover:bg-primary-container/20"
                    >
                      <span className="material-symbols-outlined text-lg">add</span>
                      <span>Add work experience</span>
                    </button>
                  </div>
                </div>
              </div>
            )}

            {/* ── Section 5: Education ── */}
            {isTalent && (activeTab === 'all' || activeTab === 'education') && (
              <div id="education" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Education
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    Your educational background — shown when you apply.
                  </p>
                  <div className="mt-4 flex items-center gap-2">
                    <span className={`inline-block w-2 h-2 rounded-full ${educationFilled === 2 ? 'bg-emerald-500' : 'bg-primary'}`} />
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {educationFilled} of 2 education sections filled
                    </span>
                  </div>
                </div>

                <div className="lg:col-span-8 flex flex-col gap-6">
                  <Field
                    label="Highest education level *"
                    error={formik.touched.educationLevel ? formik.errors.educationLevel : undefined}
                    visibilityKey="education"
                    visibility={formik.values.profileVisibility}
                    onToggleVisibility={toggleVisibility}
                  >
                    <select
                      name="educationLevel"
                      value={formik.values.educationLevel}
                      onChange={formik.handleChange}
                      onBlur={formik.handleBlur}
                      className={getInputClass(Boolean(formik.touched.educationLevel && formik.errors.educationLevel))}
                    >
                      <option value="">Not specified</option>
                      {EDUCATION_LEVELS.map((level) => (
                        <option key={level} value={level}>{EDUCATION_LEVEL_LABELS[level]}</option>
                      ))}
                    </select>
                  </Field>

                  <Field
                    full
                    label="Education history"
                    error={(formik.errors as any)['education-cross-field']}
                  >
                    <div className="space-y-4">
                      {formik.values.educationHistory.map((entry, idx) => (
                        <div
                          key={idx}
                          className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm"
                        >
                          <div className="flex items-center justify-between pb-3 border-b border-surface-variant/60 mb-3">
                            <span className="font-label-md text-label-md font-semibold text-on-surface">
                              {(EDUCATION_LEVEL_LABELS[entry.level] ?? entry.level) || `Degree #${idx + 1}`}
                            </span>
                            <button
                              type="button"
                              onClick={() => {
                                const next = [...formik.values.educationHistory]
                                next.splice(idx, 1)
                                formik.setFieldValue('educationHistory', next)
                              }}
                              className="inline-flex items-center gap-1 text-on-surface-variant transition-colors hover:text-error text-label-sm font-medium"
                            >
                              <span className="material-symbols-outlined text-base">delete</span>
                              <span>Remove</span>
                            </button>
                          </div>

                          <div className="grid gap-4 sm:grid-cols-2">
                            <div>
                              <label className="font-label-sm text-label-sm font-medium text-on-surface">Education level *</label>
                              <select
                                value={entry.level}
                                onChange={(e) => {
                                  const next = [...formik.values.educationHistory]
                                  next[idx] = { ...next[idx], level: e.target.value }
                                  formik.setFieldValue('educationHistory', next)
                                }}
                                className={getInputClass(Boolean(formik.touched.educationHistory?.[idx]?.level && (formik.errors.educationHistory as any)?.[idx]?.level))}
                              >
                                <option value="">Select level</option>
                                {EDUCATION_LEVELS.map((level) => (
                                  <option key={level} value={level}>{EDUCATION_LEVEL_LABELS[level]}</option>
                                ))}
                              </select>
                              {formik.touched.educationHistory?.[idx]?.level &&
                                (formik.errors.educationHistory as any)?.[idx]?.level && (
                                  <p className="mt-1 font-label-sm text-label-sm text-error">
                                    {(formik.errors.educationHistory as any)[idx].level}
                                  </p>
                                )}
                            </div>
                            <div>
                              <label className="font-label-sm text-label-sm font-medium text-on-surface">Institution *</label>
                              <input
                                value={entry.institution}
                                onChange={(e) => {
                                  const next = [...formik.values.educationHistory]
                                  next[idx] = { ...next[idx], institution: e.target.value }
                                  formik.setFieldValue('educationHistory', next)
                                }}
                                placeholder="e.g. Addis Ababa University"
                                className={getInputClass(Boolean(formik.touched.educationHistory?.[idx]?.institution && (formik.errors.educationHistory as any)?.[idx]?.institution))}
                              />
                              {formik.touched.educationHistory?.[idx]?.institution &&
                                (formik.errors.educationHistory as any)?.[idx]?.institution && (
                                  <p className="mt-1 font-label-sm text-label-sm text-error">
                                    {(formik.errors.educationHistory as any)[idx].institution}
                                  </p>
                                )}
                            </div>
                            <div>
                              <label className="font-label-sm text-label-sm font-medium text-on-surface">Degree *</label>
                              <input
                                value={entry.degree}
                                onChange={(e) => {
                                  const next = [...formik.values.educationHistory]
                                  next[idx] = { ...next[idx], degree: e.target.value }
                                  formik.setFieldValue('educationHistory', next)
                                }}
                                placeholder="e.g. BSc Computer Science"
                                className={getInputClass(Boolean(formik.touched.educationHistory?.[idx]?.degree && (formik.errors.educationHistory as any)?.[idx]?.degree))}
                              />
                              {formik.touched.educationHistory?.[idx]?.degree &&
                                (formik.errors.educationHistory as any)?.[idx]?.degree && (
                                  <p className="mt-1 font-label-sm text-label-sm text-error">
                                    {(formik.errors.educationHistory as any)[idx].degree}
                                  </p>
                                )}
                            </div>
                            <div>
                              <label className="font-label-sm text-label-sm font-medium text-on-surface">
                                GPA <span className="font-normal text-on-surface-variant text-[11px]">(Optional)</span>
                              </label>
                              <input
                                value={entry.gpa}
                                onChange={(e) => {
                                  const next = [...formik.values.educationHistory]
                                  next[idx] = { ...next[idx], gpa: e.target.value }
                                  formik.setFieldValue('educationHistory', next)
                                }}
                                placeholder="e.g. 3.8/4.0"
                                className={inputClass}
                              />
                            </div>
                            <div>
                              <label className="font-label-sm text-label-sm font-medium text-on-surface">Start year *</label>
                              <input
                                type="number"
                                value={entry.startYear}
                                onChange={(e) => {
                                  const next = [...formik.values.educationHistory]
                                  next[idx] = { ...next[idx], startYear: e.target.value }
                                  formik.setFieldValue('educationHistory', next)
                                }}
                                min="1970"
                                max="2099"
                                className={getInputClass(Boolean(formik.touched.educationHistory?.[idx]?.startYear && (formik.errors.educationHistory as any)?.[idx]?.startYear))}
                              />
                              {formik.touched.educationHistory?.[idx]?.startYear &&
                                (formik.errors.educationHistory as any)?.[idx]?.startYear && (
                                  <p className="mt-1 font-label-sm text-label-sm text-error">
                                    {(formik.errors.educationHistory as any)[idx].startYear}
                                  </p>
                                )}
                            </div>
                            <div>
                              <label className="font-label-sm text-label-sm font-medium text-on-surface">End year *</label>
                              <input
                                type="number"
                                value={entry.endYear}
                                onChange={(e) => {
                                  const next = [...formik.values.educationHistory]
                                  next[idx] = { ...next[idx], endYear: e.target.value }
                                  formik.setFieldValue('educationHistory', next)
                                }}
                                min="1970"
                                max="2099"
                                placeholder="e.g. 2024"
                                className={getInputClass(Boolean(formik.touched.educationHistory?.[idx]?.endYear && (formik.errors.educationHistory as any)?.[idx]?.endYear))}
                              />
                              {formik.touched.educationHistory?.[idx]?.endYear &&
                                (formik.errors.educationHistory as any)?.[idx]?.endYear && (
                                  <p className="mt-1 font-label-sm text-label-sm text-error">
                                    {(formik.errors.educationHistory as any)[idx].endYear}
                                  </p>
                                )}
                            </div>
                          </div>
                        </div>
                      ))}

                      <button
                        type="button"
                        onClick={() => {
                          const next = [...formik.values.educationHistory, {
                            level: '', institution: '', degree: '', gpa: '', startYear: '', endYear: '',
                          }]
                          formik.setFieldValue('educationHistory', next)
                        }}
                        className="w-full flex items-center justify-center gap-2 rounded-xl border border-dashed border-outline-variant bg-surface-container-low/40 p-4 font-label-md text-label-md text-primary font-medium transition-colors hover:bg-primary-container/20"
                      >
                        <span className="material-symbols-outlined text-lg">add</span>
                        <span>
                          {formik.values.educationLevel === 'PhD'
                            ? 'Add PhD degree'
                            : formik.values.educationLevel === 'Masters'
                              ? "Add Master's degree"
                              : formik.values.educationLevel === 'Bachelors'
                                ? "Add Bachelor's degree"
                                : 'Add degree'}
                        </span>
                      </button>
                    </div>
                  </Field>
                </div>
              </div>
            )}

            {/* ── Section 6: Links & resume ── */}
            {isTalent && (activeTab === 'all' || activeTab === 'links') && (
              <div id="links" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Links &amp; Resume
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    Where employers can find your portfolio and review your formal resume.
                  </p>
                  <div className="mt-4 flex items-center gap-2">
                    <span className={`inline-block w-2 h-2 rounded-full ${linksFilled > 0 ? 'bg-emerald-500' : 'bg-surface-variant'}`} />
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      {linksFilled} of 4 items added {formik.values.resumeUrl ? '· Resume uploaded' : ''}
                    </span>
                  </div>
                </div>

                <div className="lg:col-span-8 flex flex-col gap-6">
                  <div>
                    <label className="font-label-sm text-label-sm font-semibold text-on-surface mb-2 block">
                      Resume (PDF)
                    </label>
                    {formik.values.resumeUrl ? (
                      <div className="flex flex-col sm:flex-row sm:items-center gap-4 rounded-xl border border-surface-variant bg-surface-container-low p-4">
                        <div className="flex items-center gap-3 min-w-0 flex-1">
                          <span className="material-symbols-outlined text-[28px] text-primary shrink-0">description</span>
                          <div className="min-w-0">
                            <p className="font-label-md text-label-md font-semibold text-on-surface truncate" title={formik.values.resumeUrl}>{resumeDisplayName}</p>
                            <p className="font-label-sm text-label-sm text-on-surface-variant">PDF document ready</p>
                          </div>
                        </div>
                        <div className="flex flex-wrap items-center gap-2 shrink-0">
                          <button
                            type="button"
                            onClick={() => resumeInputRef.current?.click()}
                            disabled={resumeUploading}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-1.5 font-label-sm text-label-sm text-on-surface transition-colors hover:bg-surface-container-low disabled:opacity-50"
                          >
                            {resumeUploading ? (
                              <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                            ) : (
                              <span className="material-symbols-outlined text-base">upload</span>
                            )}
                            Change
                          </button>
                          <button
                            type="button"
                            onClick={() => formik.setFieldValue('resumeUrl', '')}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-error/30 bg-error/10 px-3.5 py-1.5 font-label-sm text-label-sm text-error transition-colors hover:bg-error hover:text-white"
                          >
                            <span className="material-symbols-outlined text-base">delete</span>
                            Remove
                          </button>
                          <button
                            type="button"
                            onClick={async () => {
                              try {
                                const { getPresignedDownloadUrl } = await import('../../../api/fileUpload.ts')
                                const url = await getPresignedDownloadUrl(formik.values.resumeUrl)
                                window.open(url, '_blank')
                              } catch (err) {
                                setSubmitError(err instanceof Error ? err.message : 'Failed to open resume')
                              }
                            }}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-1.5 font-label-sm text-label-sm text-primary transition-colors hover:bg-primary-container/10"
                          >
                            <span className="material-symbols-outlined text-base">open_in_new</span>
                            View
                          </button>
                          <button
                            type="button"
                            onClick={() => parseUploadedResume(formik.values.resumeUrl)}
                            disabled={resumeParsing}
                            className="inline-flex items-center gap-1.5 rounded-lg bg-primary px-3.5 py-1.5 font-label-sm text-label-sm text-on-primary transition-colors hover:opacity-90 disabled:opacity-50 shadow-sm"
                          >
                            {resumeParsing ? (
                              <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-on-primary/30 border-t-on-primary" />
                            ) : (
                              <span className="material-symbols-outlined text-base">auto_awesome</span>
                            )}
                            {resumeParsing ? 'Reading…' : 'Fill from resume'}
                          </button>
                        </div>
                      </div>
                    ) : (
                      <button
                        type="button"
                        onClick={() => resumeInputRef.current?.click()}
                        disabled={resumeUploading}
                        className="inline-flex items-center gap-2 rounded-xl border-2 border-dashed border-outline-variant bg-surface-container-lowest p-6 font-label-md text-label-md text-on-surface transition-colors hover:border-primary hover:bg-primary-container/5 disabled:opacity-50 w-full justify-center"
                      >
                        {resumeUploading ? (
                          <span className="h-4 w-4 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                        ) : (
                          <span className="material-symbols-outlined text-[20px]">upload</span>
                        )}
                        <span>Upload resume (PDF)</span>
                      </button>
                    )}
                    <p className="mt-1.5 font-label-sm text-label-sm text-on-surface-variant/60">
                      PDF only · Max 10MB
                    </p>
                  </div>

                  <div className="grid grid-cols-1 gap-4">
                    <Field label="LinkedIn" visibilityKey="linkedin" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                      <input
                        name="linkedInUrl"
                        value={formik.values.linkedInUrl}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="https://linkedin.com/in/…"
                        className={inputClass}
                      />
                    </Field>
                    <Field label="GitHub" visibilityKey="github" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                      <input
                        name="githubUrl"
                        value={formik.values.githubUrl}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="https://github.com/…"
                        className={inputClass}
                      />
                    </Field>
                    <Field label="Portfolio" visibilityKey="portfolio" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                      <input
                        name="portfolioUrl"
                        value={formik.values.portfolioUrl}
                        onChange={formik.handleChange}
                        onBlur={formik.handleBlur}
                        placeholder="https://…"
                        className={inputClass}
                      />
                    </Field>
                  </div>
                </div>
              </div>
            )}

            {/* ── Section 7: Admin Profile ── */}
            {role === 'Admin' && (
              <div id="admin-profile" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
                <div className="lg:col-span-4 flex flex-col justify-start">
                  <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    Admin Information
                  </h2>
                  <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
                    Account details and administrative access on SeraGo.
                  </p>
                </div>
                <div className="lg:col-span-8 flex flex-col gap-4">
                  <p className="font-body-md text-body-md text-on-surface-variant">
                    Admins do not have candidate-specific sections. All your system credentials and activity can be reviewed in Security &amp; Settings.
                  </p>
                </div>
              </div>
            )}
          </div>
        </form>

        {/* Floating Bottom Save Dock */}
        {formik.dirty && (
          <aside className="sticky bottom-6 z-30 mt-12 w-full rounded-2xl bg-surface-container-lowest/95 backdrop-blur-md shadow-xl border border-surface-variant p-4 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="relative flex h-3 w-3">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-accent opacity-75" />
                <span className="relative inline-flex rounded-full h-3 w-3 bg-accent" />
              </span>
              <div className="flex items-center gap-2">
                <span className="font-label-md text-label-md font-medium text-on-surface">Unsaved changes:</span>
                <span className="font-label-sm text-label-sm text-on-surface-variant">Profile has been modified</span>
              </div>
              {submitError && (
                <span className="ml-2 font-label-sm text-label-sm text-error">({submitError})</span>
              )}
            </div>

            <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
              <button
                type="button"
                onClick={() => formik.resetForm()}
                className="w-full sm:w-auto px-5 py-2.5 rounded-lg bg-surface-container-lowest text-on-surface font-label-md text-label-md shadow-sm hover:bg-surface-container-low transition-colors border border-outline-variant"
              >
                Discard
              </button>
              <button
                type="button"
                onClick={handleSaveClick}
                disabled={loading || formik.isSubmitting}
                className="w-full sm:w-auto px-7 py-2.5 rounded-lg bg-accent text-on-accent font-label-md text-label-md font-medium shadow-md hover:opacity-90 active:scale-[0.98] transition-all flex items-center justify-center gap-2 disabled:pointer-events-none disabled:opacity-60"
              >
                {formik.isSubmitting ? (
                  <>
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent" />
                    <span>Saving…</span>
                  </>
                ) : (
                  <>
                    <span>Save profile</span>
                    <span className="material-symbols-outlined text-base">check</span>
                  </>
                )}
              </button>
            </div>
          </aside>
        )}
      </div>

      <PasswordSetupCard />

      <UnsavedChangesDialog
        open={dialogOpen}
        saving={dialogSaving}
        onSave={confirmSave}
        onDiscard={handleDiscard}
        onCancel={handleCancel}
      />

      {/* Validation errors modal */}
      {validationModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div
            className="absolute inset-0 bg-inverse-surface/40 backdrop-blur-sm"
            onClick={() => setValidationModalOpen(false)}
          />
          <div className="relative z-10 w-full max-w-lg max-h-[80vh] flex flex-col rounded-xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl">
            <div className="flex items-center gap-2 mb-1">
              <span className="material-symbols-outlined text-error text-lg">error</span>
              <h2 className="font-headline-md text-headline-md text-on-surface">
                Required fields are missing
              </h2>
            </div>
            <p className="font-body-md text-body-sm text-on-surface-variant mb-4">
              The following required fields haven't been filled in yet. Please fill them out to complete your profile:
            </p>
            <div className="flex-1 overflow-y-auto space-y-2 pr-1">
              {validationErrors.map((err, i) => (
                <div
                  key={i}
                  className="flex items-start gap-2 rounded-lg border border-error/20 bg-error-container/20 px-3 py-2.5"
                >
                  <span className="material-symbols-outlined text-error text-sm mt-0.5">priority_high</span>
                  <div className="min-w-0">
                    <span className="font-label-md text-label-md font-semibold text-on-surface">{err.field}</span>
                    <p className="font-body-sm text-body-sm text-on-surface-variant">{err.message}</p>
                  </div>
                </div>
              ))}
            </div>
            <div className="flex justify-end gap-3 mt-4 pt-3 border-t border-surface-variant">
              <button
                type="button"
                onClick={() => setValidationModalOpen(false)}
                className="px-4 py-2 rounded-lg font-label-md text-label-md font-medium text-on-accent bg-accent hover:opacity-90 transition-opacity"
              >
                Understood
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Photo preview modal */}
      {photoModalOpen && formik.values.avatarUrl && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
          onClick={() => setPhotoModalOpen(false)}
        >
          <div
            className="relative max-h-[80vh] max-w-lg rounded-xl bg-surface-container-lowest shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            <button
              type="button"
              onClick={() => setPhotoModalOpen(false)}
              className="absolute right-3 top-3 z-10 flex h-8 w-8 items-center justify-center rounded-full bg-surface-container-low/80 text-on-surface transition-colors hover:bg-surface-container"
            >
              <span className="material-symbols-outlined text-[20px]">close</span>
            </button>
            <img
              src={formik.values.avatarUrl}
              alt={fullName}
              className="w-full rounded-xl object-contain"
            />
          </div>
        </div>
      )}

      <ResumeReviewModal
        open={!!reviewProfileData}
        profile={reviewProfileData?.profile ?? null}
        currentSectorIds={currentSectorIds}
        onApply={async (selectedSectorIds) => {
          if (!reviewProfileData) return
          await applyParsedResume(reviewProfileData.profile, selectedSectorIds)
          const sectorCount = selectedSectorIds.length
          const feedMsg = sectorCount > 0
            ? ` and set ${sectorCount} sector${sectorCount === 1 ? '' : 's'} for your For You feed`
            : ''
          setResumeNotice({
            kind: 'filled',
            message: `We read your resume and pre-filled ${reviewProfileData.fieldsFound} section${reviewProfileData.fieldsFound === 1 ? '' : 's'
              }${feedMsg}. Nothing is saved yet — review the fields below, then press Save profile.`,
          })
          showToast(
            `Pre-filled profile${feedMsg}. Remember to review and save!`,
            'success',
          )
          setReviewProfileData(null)
        }}
        onCancel={() => {
          const cameFromOnboarding = fromOnboardingRef.current
          fromOnboardingRef.current = false
          setReviewProfileData(null)
          formik.resetForm()
          if (isTalent && cameFromOnboarding) {
            navigate('/dashboard/talent/settings', {
              state: { openSectorModal: true },
            })
          } else {
            setResumeNotice({
              kind: 'empty',
              message: "Resume parsing cancelled. You can fill your profile manually.",
            })
          }
        }}
      />
    </DashboardShell>
  )
}
