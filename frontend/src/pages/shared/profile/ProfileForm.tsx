import { useCallback, useEffect, useRef, useState } from 'react'
import { useFormik } from 'formik'
import { array, object, string } from 'yup'
import {
  fetchProfile,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
  updateProfile,
} from '../../../api'
import { useRequireRole, useUnsavedChanges } from '../../../hooks'
import type { RequiredRole } from '../../../hooks'
import type { ProfileResponse, UpdateProfileRequest } from '../../../types'
import { useNavigate } from 'react-router-dom'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import UnsavedChangesDialog from '../../../components/ui/UnsavedChangesDialog.tsx'
import AccordionSection from '../../../components/ui/AccordionSection.tsx'
import RichTextEditor from '../../../components/ui/RichTextEditor.tsx'
import PasswordSetupCard from '../../../components/dashboard/PasswordSetupCard.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'
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

const EDUCATION_LEVELS = ['HighSchool', 'Bachelors', 'Masters', 'PhD']
const EDUCATION_LEVEL_LABELS: Record<string, string> = {
  HighSchool: 'High School',
  Bachelors: "Bachelor's Degree",
  Masters: "Master's Degree",
  PhD: 'PhD / Doctorate',
}

const COMPANY_TYPES = [
  { value: 'Public', label: 'Public' },
  { value: 'Private', label: 'Private' },
  { value: 'NonProfit', label: 'Non-profit / NGO' },
  { value: 'Government', label: 'Government' },
  { value: 'Startup', label: 'Startup' },
  { value: 'SoleProprietorship', label: 'Sole Proprietorship' },
  { value: 'Partnership', label: 'Partnership' },
]

/** Default privacy for company profile: everything visible. */
const DEFAULT_COMPANY_VISIBILITY: Record<string, boolean> = {
  avatar: true,
  companyName: true,
  industry: true,
  companySize: true,
  websiteUrl: true,
  about: true,
  foundedYear: true,
  headquarters: true,
  phoneNumber: true,
  email: true,
  companyType: true,
  linkedInUrl: true,
  twitterUrl: true,
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
  country: true,
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

const FIELD_LABELS: Record<string, string> = {
  firstName: 'First name',
  middleName: 'Middle name',
  lastName: 'Last name',
  avatarUrl: 'Profile photo',
  city: 'City',
  country: 'Country',
  about: 'About',
  experienceLevel: 'Experience level',
  yearsOfExperience: 'Years of experience',
  desiredRoles: 'Desired roles',
  skills: 'Skills',
  desiredJobTypes: 'Desired job types',
  workMode: 'Work mode',
  availability: 'Availability',
  preferredSectorIds: 'Preferred sectors',
  resumeUrl: 'Resume',
  linkedInUrl: 'LinkedIn',
  githubUrl: 'GitHub',
  portfolioUrl: 'Portfolio',
  phoneNumber: 'Phone number',
  dateOfBirth: 'Date of birth',
  address: 'Address',
  workExperience: 'Work experience',
  educationLevel: 'Education level',
  educationHistory: 'Education history',
  currentIndustry: 'Current industry',
  currentProfession: 'Current profession',
  preferredLocations: 'Preferred locations',
  profileVisibility: 'Profile visibility',
  skillVisibility: 'Skill visibility',
  companyName: 'Company name',
  industry: 'Industry',
  companySize: 'Company size',
  websiteUrl: 'Website',
  foundedYear: 'Founded year',
  headquarters: 'Headquarters',
  companyPhoneNumber: 'Phone number',
  companyEmail: 'Email',
  companyType: 'Company type',
  companyLinkedInUrl: 'LinkedIn',
  companyTwitterUrl: 'Twitter',
  companyVisibility: 'Company visibility',
  isCompanyPrivate: 'Company privacy',
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
}

const MISSING_LABELS: Record<string, string> = {
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
  foundedYear: 'Founded year',
  headquarters: 'Headquarters',
  phoneNumber: 'Phone number',
  email: 'Email',
  companyType: 'Company type',
}

const inputClass =
  'w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary'

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
  country: string().required('Country is required.').max(120),
})

const talentSchema = commonSchema
  .concat(
    object({
      phoneNumber: string().required('Phone number is required.').max(32),
      dateOfBirth: string().required('Date of birth is required.'),
      about: string().max(10000),
      experienceLevel: string().required('Experience level is required.'),
      yearsOfExperience: string()
        .required('Years of experience is required.')
        .test(
          'non-negative-int',
          'Years of experience must be 0 or greater.',
          (value) => !value || /^\d+$/.test(value),
        ),
      workMode: string().required('Work mode is required.'),
      availability: string().required('Availability is required.'),
      currentIndustry: string().required('Current industry is required.'),
      currentProfession: string().required('Current profession is required.'),
      skills: array()
        .of(string())
        .required()
        .min(1, 'Add at least one skill.'),
      desiredRoles: array().of(string()),
      desiredJobTypes: array().of(string()),
      preferredSectorIds: array()
        .of(string())
        .required()
        .min(1, 'Select at least one preferred sector.'),
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

const recruiterSchema = commonSchema.concat(
  object({
    companyName: string().required('Company name is required.').max(255),
    industry: string().max(120),
    companySize: string().max(64),
    websiteUrl: optionalUrl,
    about: string().max(10000),
    foundedYear: string().test(
      'valid-year',
      'Enter a valid year (e.g. 2015).',
      (value) => !value || (/^\d{4}$/.test(value) && Number(value) >= 1800 && Number(value) <= 2100),
    ),
    headquarters: string().max(255),
    companyPhoneNumber: string().max(32),
    companyEmail: string().email('Enter a valid email address.'),
    companyType: string(),
    companyLinkedInUrl: optionalUrl,
    companyTwitterUrl: optionalUrl,
  }),
)

interface ProfileFormValues {
  // Common
  firstName: string
  middleName: string
  lastName: string
  avatarUrl: string
  city: string
  country: string
  // Talent
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
  // Identity / personal
  phoneNumber: string
  dateOfBirth: string
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
  // Recruiter
  companyName: string
  industry: string
  companySize: string
  websiteUrl: string
  foundedYear: string
  headquarters: string
  companyPhoneNumber: string
  companyEmail: string
  companyType: string
  companyLinkedInUrl: string
  companyTwitterUrl: string
  // Recruiter privacy
  companyVisibility: Record<string, boolean>
  isCompanyPrivate: boolean
}

const initialValues: ProfileFormValues = {
  firstName: '',
  middleName: '',
  lastName: '',
  avatarUrl: '',
  city: '',
  country: '',
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
  phoneNumber: '',
  dateOfBirth: '',
  address: '',
  workExperience: [],
  educationLevel: '',
  educationHistory: [],
  currentIndustry: '',
  currentProfession: '',
  preferredLocations: [],
  profileVisibility: { ...DEFAULT_VISIBILITY },
  skillVisibility: {},
  companyName: '',
  industry: '',
  companySize: '',
  websiteUrl: '',
  foundedYear: '',
  headquarters: '',
  companyPhoneNumber: '',
  companyEmail: '',
  companyType: '',
  companyLinkedInUrl: '',
  companyTwitterUrl: '',
  companyVisibility: { ...DEFAULT_COMPANY_VISIBILITY },
  isCompanyPrivate: false,
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
    <div className={cn(full && 'md:col-span-2')}>
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
  Recruiter: {
    blurb:
      'Your personal details and company information — shown on your job posts.',
  },
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
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [sectors, setSectors] = useState<{ id: string; name: string }[]>([])
  const [avatarUploading, setAvatarUploading] = useState(false)
  const [photoModalOpen, setPhotoModalOpen] = useState(false)
  const [resumeUploading, setResumeUploading] = useState(false)
  const { showToast } = useToast()
  const [validationModalOpen, setValidationModalOpen] = useState(false)
  const [validationErrors, setValidationErrors] = useState<{ field: string; message: string }[]>([])
  const avatarInputRef = useRef<HTMLInputElement>(null)
  const resumeInputRef = useRef<HTMLInputElement>(null)

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

        // Parse company visibility JSON
        let parsedCompanyVisibility = { ...DEFAULT_COMPANY_VISIBILITY }
        try {
          if (p.recruiter?.companyVisibility && p.recruiter.companyVisibility !== '{}') {
            parsedCompanyVisibility = { ...DEFAULT_COMPANY_VISIBILITY, ...JSON.parse(p.recruiter.companyVisibility) }
          }
        } catch { /* keep defaults */ }

        formik.resetForm({
          values: {
            firstName: p.firstName,
            middleName: p.middleName ?? '',
            lastName: p.lastName,
            avatarUrl: p.avatarUrl,
            city: p.city,
            country: p.country,
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
            phoneNumber: p.talent?.phoneNumber ?? '',
            dateOfBirth: p.talent?.dateOfBirth ?? '',
            address: p.talent?.address ?? '',
            workExperience: parsedWorkExperience,
            educationLevel: p.talent?.educationLevel ?? '',
            educationHistory: parsedEducation,
            currentIndustry: p.talent?.currentIndustry ?? '',
            currentProfession: p.talent?.currentProfession ?? '',
            preferredLocations: parsedLocations,
            profileVisibility: parsedVisibility,
            skillVisibility: parsedSkillVisibility,
            companyName: p.recruiter?.companyName ?? '',
            industry: p.recruiter?.industry ?? '',
            companySize: p.recruiter?.companySize ?? '',
            websiteUrl: p.recruiter?.websiteUrl ?? '',
            foundedYear: p.recruiter?.foundedYear != null ? String(p.recruiter.foundedYear) : '',
            headquarters: p.recruiter?.headquarters ?? '',
            companyPhoneNumber: p.recruiter?.phoneNumber ?? '',
            companyEmail: p.recruiter?.email ?? '',
            companyType: p.recruiter?.companyType ?? '',
            companyLinkedInUrl: p.recruiter?.linkedInUrl ?? '',
            companyTwitterUrl: p.recruiter?.twitterUrl ?? '',
            companyVisibility: parsedCompanyVisibility,
            isCompanyPrivate: p.recruiter?.isCompanyPrivate ?? false,
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
      country: values.country.trim(),
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
        preferredSectorIds: values.preferredSectorIds,
        workMode: values.workMode || null,
        availability: values.availability || null,
        resumeUrl: values.resumeUrl.trim(),
        linkedInUrl: values.linkedInUrl.trim(),
        githubUrl: values.githubUrl.trim(),
        portfolioUrl: values.portfolioUrl.trim(),
        phoneNumber: values.phoneNumber.trim() || '',
        dateOfBirth: values.dateOfBirth || '',
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
    } else if (isRecruiter) {
      payload.recruiter = {
        companyName: values.companyName.trim(),
        industry: values.industry.trim(),
        companySize: values.companySize.trim(),
        websiteUrl: values.websiteUrl.trim(),
        about: values.about.trim(),
        foundedYear: values.foundedYear === '' ? null : Number(values.foundedYear),
        headquarters: values.headquarters.trim() || '',
        phoneNumber: values.companyPhoneNumber.trim() || '',
        email: values.companyEmail.trim() || '',
        companyType: values.companyType || null,
        linkedInUrl: values.companyLinkedInUrl.trim() || '',
        twitterUrl: values.companyTwitterUrl.trim() || '',
        companyVisibility: JSON.stringify(values.companyVisibility),
        isCompanyPrivate: values.isCompanyPrivate,
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

  const toggleSector = (id: string) => {
    const next = formik.values.preferredSectorIds.includes(id)
      ? formik.values.preferredSectorIds.filter((s) => s !== id)
      : [...formik.values.preferredSectorIds, id]
    formik.setFieldValue('preferredSectorIds', next)
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

  const toggleCompanyVisibility = (key: string) => {
    formik.setFieldValue(
      'companyVisibility',
      { ...formik.values.companyVisibility, [key]: !formik.values.companyVisibility[key] },
    )
  }

  const toggleCompanyPrivate = () => {
    formik.setFieldValue('isCompanyPrivate', !formik.values.isCompanyPrivate)
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
    setSubmitError(null)
    try {
      const { uploadFile } = await import('../../../api/fileUpload.ts')
      const url = await uploadFile(file, 'resume')
      formik.setFieldValue('resumeUrl', url)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setResumeUploading(false)
      if (resumeInputRef.current) resumeInputRef.current.value = ''
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

  const completion = profile?.completion

  /* ——— Talent: count fields filled per section for accordion badges ——— */
  const personalFilled = [
    formik.values.firstName,
    formik.values.lastName,
    formik.values.city,
    formik.values.country,
    formik.values.phoneNumber,
    formik.values.dateOfBirth,
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
    formik.values.preferredSectorIds.length > 0 ? 'sectors' : '',
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

  /* ——— Recruiter: count fields filled per section for accordion badges ——— */
  const recruiterPersonalFilled = [
    formik.values.firstName,
    formik.values.lastName,
    formik.values.middleName,
    formik.values.city,
    formik.values.country,
    formik.values.avatarUrl,
  ].filter(Boolean).length

  const recruiterCompanyFilled = [
    formik.values.companyName,
    formik.values.industry,
    formik.values.companySize,
    formik.values.websiteUrl,
    formik.values.about,
    formik.values.foundedYear,
    formik.values.headquarters,
    formik.values.companyPhoneNumber,
    formik.values.companyEmail,
    formik.values.companyType,
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

  const avatarInitials = fullName.split(' ').map((w) => w[0]).filter(Boolean).slice(0, 2).join('').toUpperCase()

  return (
    <DashboardShell role={role} authUser={auth.user}>
      {isTalent ? (
        <>
          {/* Profile preview header card */}
          <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-6 relative overflow-hidden">
            <div className="flex items-center gap-6 z-10">
              {formik.values.avatarUrl ? (
                <img
                  className="w-20 h-20 rounded-full object-cover border border-surface-variant"
                  src={formik.values.avatarUrl}
                  alt={fullName}
                />
              ) : (
                <span className="flex w-20 h-20 items-center justify-center rounded-full bg-primary-container/60 font-headline-lg text-headline-lg font-semibold text-primary">
                  {avatarInitials}
                </span>
              )}
              <div>
                <h1 className="font-headline-lg text-headline-lg text-primary mb-1">
                  {fullName || 'Your name'}
                </h1>
              </div>
            </div>
            <div className="flex flex-col items-start gap-2 z-10">
              <div className="flex items-center gap-2">
                <input
                  ref={avatarInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp,image/gif"
                  onChange={handleAvatarUpload}
                  className="hidden"
                />
                <button
                  type="button"
                  role="switch"
                  aria-checked={formik.values.profileVisibility.avatar ?? true}
                  aria-label="Toggle profile picture visibility"
                  onClick={() => toggleVisibility('avatar')}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                >
                  <span
                    className={cn(
                      'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors',
                      (formik.values.profileVisibility.avatar ?? true) ? 'bg-primary' : 'bg-surface-variant',
                    )}
                  >
                    <span
                      className={cn(
                        'inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
                        (formik.values.profileVisibility.avatar ?? true) ? 'translate-x-4' : 'translate-x-0.5',
                      )}
                    />
                  </span>
                  {formik.values.profileVisibility.avatar ?? true ? 'Hide' : 'Show'}
                </button>
                <button
                  type="button"
                  onClick={() => avatarInputRef.current?.click()}
                  disabled={avatarUploading}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low disabled:opacity-50"
                >
                  {avatarUploading ? (
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                  ) : (
                    <span className="material-symbols-outlined text-[18px]">upload</span>
                  )}
                  {formik.values.avatarUrl ? 'Change photo' : 'Upload photo'}
                </button>
                {formik.values.avatarUrl && (
                  <>
                    <button
                      type="button"
                      onClick={() => setPhotoModalOpen(true)}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                    >
                      <span className="material-symbols-outlined text-[18px]">visibility</span>
                      View photo
                    </button>
                    <button
                      type="button"
                      onClick={() => formik.setFieldValue('avatarUrl', '')}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-error-container hover:text-error"
                    >
                      <span className="material-symbols-outlined text-[18px]">person_remove</span>
                      Remove
                    </button>
                  </>
                )}
              </div>
              <p className="font-label-sm text-label-sm text-on-surface-variant/60 text-center w-full">
                JPEG, PNG, WebP, or GIF · Max 5MB
              </p>
            </div>
            <div className="absolute top-0 right-0 w-64 h-full bg-gradient-to-l from-surface-container-low to-transparent opacity-50 z-0 pointer-events-none" />
          </section>
        </>
      ) : role === 'Admin' ? (
        <>
          <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
            Profile
          </h1>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            {COPY[role].blurb}
          </p>
        </>
      ) : (
        /* ── Recruiter: profile preview header card (mirrors the talent one) ── */
        <>
          <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-6 relative overflow-hidden">
            <div className="flex items-center gap-6 z-10">
              {formik.values.avatarUrl ? (
                <img
                  className="w-20 h-20 rounded-full object-cover border border-surface-variant"
                  src={formik.values.avatarUrl}
                  alt={fullName}
                />
              ) : (
                <span className="flex w-20 h-20 items-center justify-center rounded-full bg-primary-container/60 font-headline-lg text-headline-lg font-semibold text-primary">
                  {avatarInitials || '?'}
                </span>
              )}
              <div>
                <h1 className="font-headline-lg text-headline-lg text-primary mb-1">
                  {fullName || 'Your name'}
                </h1>
                {formik.values.companyName && (
                  <p className="font-body-lg text-body-lg text-on-surface-variant mb-2">
                    Hiring for {formik.values.companyName}
                  </p>
                )}
              </div>
            </div>
            <div className="flex flex-col items-start gap-2 z-10">
              <div className="flex items-center gap-2">
                <input
                  ref={avatarInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp,image/gif"
                  onChange={handleAvatarUpload}
                  className="hidden"
                />
                <button
                  type="button"
                  role="switch"
                  aria-checked={formik.values.profileVisibility.avatar ?? true}
                  aria-label="Toggle profile picture visibility"
                  onClick={() => toggleVisibility('avatar')}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                >
                  <span
                    className={cn(
                      'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors',
                      (formik.values.profileVisibility.avatar ?? true) ? 'bg-primary' : 'bg-surface-variant',
                    )}
                  >
                    <span
                      className={cn(
                        'inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
                        (formik.values.profileVisibility.avatar ?? true) ? 'translate-x-4' : 'translate-x-0.5',
                      )}
                    />
                  </span>
                  {formik.values.profileVisibility.avatar ?? true ? 'Hide' : 'Show'}
                </button>
                <button
                  type="button"
                  onClick={() => avatarInputRef.current?.click()}
                  disabled={avatarUploading}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low disabled:opacity-50"
                >
                  {avatarUploading ? (
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                  ) : (
                    <span className="material-symbols-outlined text-[18px]">upload</span>
                  )}
                  {formik.values.avatarUrl ? 'Change photo' : 'Upload photo'}
                </button>
                {formik.values.avatarUrl && (
                  <>
                    <button
                      type="button"
                      onClick={() => setPhotoModalOpen(true)}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                    >
                      <span className="material-symbols-outlined text-[18px]">visibility</span>
                      View photo
                    </button>
                    <button
                      type="button"
                      onClick={() => formik.setFieldValue('avatarUrl', '')}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-error-container hover:text-error"
                    >
                      <span className="material-symbols-outlined text-[18px]">person_remove</span>
                      Remove
                    </button>
                  </>
                )}
              </div>
              <p className="font-label-sm text-label-sm text-on-surface-variant/60 text-center w-full">
                JPEG, PNG, WebP, or GIF · Max 5MB
              </p>
            </div>
            <div className="absolute top-0 right-0 w-64 h-full bg-gradient-to-l from-surface-container-low to-transparent opacity-50 z-0 pointer-events-none" />
          </section>
        </>
      )}

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
          className="mt-8 flex flex-col gap-4"
        >
          {/* ═══════════════════════════ TALENT SECTIONS ═══════════════════════════ */}
          {isTalent && (
            <>
              {/* ── Section 1: Personal information ── */}
              <AccordionSection
                title="Personal information"
                description="Your name and location — shown across the platform."
                icon="person"
                defaultOpen
                completion={{ filled: personalFilled, total: 9 }}
              >
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
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Middle name *"
                  error={formik.touched.middleName ? formik.errors.middleName : undefined}
                >
                  <input
                    name="middleName"
                    value={formik.values.middleName}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="e.g. Kebede"
                    className={inputClass}
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
                    className={inputClass}
                  />
                </Field>
                <Field label="Phone number *" visibilityKey="phone" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                  <input
                    name="phoneNumber"
                    value={formik.values.phoneNumber}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="+251 91 123 4567"
                    className={inputClass}
                  />
                </Field>
                <Field label="Date of birth *" visibilityKey="dateOfBirth" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                  <input
                    type="date"
                    name="dateOfBirth"
                    value={formik.values.dateOfBirth}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    className={inputClass}
                  />
                </Field>
                <Field label="City *">
                  <input
                    name="city"
                    value={formik.values.city}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="Addis Ababa"
                    className={inputClass}
                  />
                </Field>
                <Field label="Country *">
                  <input
                    name="country"
                    value={formik.values.country}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="Ethiopia"
                    className={inputClass}
                  />
                </Field>
                <Field label="Street address" visibilityKey="address" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                  <input
                    name="address"
                    value={formik.values.address}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="Bole Road, Addis Ababa"
                    className={inputClass}
                  />
                </Field>
              </AccordionSection>

              {/* ── Section 2: Professional profile ── */}
              <AccordionSection
                title="Professional profile"
                description="What you do and how you work — your career identity."
                icon="work"
                completion={{ filled: professionalFilled, total: 7 }}
              >
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
                    className={inputClass}
                  />
                </Field>
                <Field label="Experience level *">
                  <select
                    name="experienceLevel"
                    value={formik.values.experienceLevel}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    className={inputClass}
                  >
                    <option value="">Not specified</option>
                    {EXPERIENCE_LEVELS.map((level) => (
                      <option key={level} value={level}>{level}</option>
                    ))}
                  </select>
                </Field>
                <Field label="Work mode *">
                  <select
                    name="workMode"
                    value={formik.values.workMode}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    className={inputClass}
                  >
                    <option value="">Not specified</option>
                    {WORK_MODES.map((mode) => (
                      <option key={mode} value={mode}>{mode}</option>
                    ))}
                  </select>
                </Field>
                <Field label="Availability *">
                  <select
                    name="availability"
                    value={formik.values.availability}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    className={inputClass}
                  >
                    <option value="">Not specified</option>
                    {AVAILABILITIES.map((availability) => (
                      <option key={availability} value={availability}>{availability}</option>
                    ))}
                  </select>
                </Field>
                <Field label="Current industry *" visibilityKey="currentIndustry" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                  <input
                    name="currentIndustry"
                    value={formik.values.currentIndustry}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="e.g. Technology, Finance"
                    className={inputClass}
                  />
                </Field>
                <Field label="Current profession *" visibilityKey="currentProfession" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                  <input
                    name="currentProfession"
                    value={formik.values.currentProfession}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="e.g. Software Engineer"
                    className={inputClass}
                  />
                </Field>
                <Field full label="About" hint="A short bio recruiters will see.">
                  <RichTextEditor
                    value={formik.values.about}
                    onChange={(v) => formik.setFieldValue('about', v)}
                    placeholder="Tell recruiters about yourself…"
                  />
                </Field>
              </AccordionSection>

              {/* ── Section 3: Skills & roles ── */}
              <AccordionSection
                title="Skills & roles"
                description="What you can do and what you're looking for."
                icon="psychology"
                completion={{ filled: skillsFilled, total: 4 }}
              >
                <div className="md:col-span-2">
                  <div className="flex items-center justify-between gap-2 mb-2">
                    <label className="font-label-sm text-label-sm font-medium text-on-surface">
                      Skills *
                    </label>
                  </div>
                  <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2">
                    {formik.values.skills.map((skill) => {
                      const isVisible = formik.values.skillVisibility[skill] ?? true
                      return (
                        <div
                          key={skill}
                          className={cn(
                            'flex items-center justify-between gap-1 rounded-lg border px-2.5 py-2 font-label-sm text-label-sm',
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
                <Field label="Desired roles" hint="Press Enter after each role.">
                  <TagInput
                    value={formik.values.desiredRoles}
                    onChange={(next) => formik.setFieldValue('desiredRoles', next)}
                    placeholder="e.g. Product Designer"
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
                <Field full label="Preferred sectors *">
                  {sectors.length === 0 ? (
                    <p className="font-label-sm text-label-sm text-on-surface-variant">
                      No sectors available yet.
                    </p>
                  ) : (
                    <div className="mt-1.5 flex flex-wrap gap-2">
                      {sectors.map((sector) => {
                        const checked = formik.values.preferredSectorIds.includes(sector.id)
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
              </AccordionSection>

              {/* ── Section 4: Work experience ── */}
              <AccordionSection
                title="Work experience"
                description="Your work history — shown to recruiters when you apply."
                icon="work_history"
                completion={{ filled: formik.values.workExperience.length > 0 ? 1 : 0, total: 1 }}
              >
                <div className="md:col-span-2">
                  <div className="space-y-4">
                    {formik.values.workExperience.map((entry, idx) => (
                      <div
                        key={idx}
                        className="rounded-lg border border-surface-variant p-4"
                      >
                        <div className="flex items-center justify-between">
                          <span className="font-label-sm text-label-sm font-medium text-on-surface">
                            {entry.title || entry.company || `Experience ${idx + 1}`}
                          </span>
                          <button
                            type="button"
                            onClick={() => {
                              const next = [...formik.values.workExperience]
                              next.splice(idx, 1)
                              formik.setFieldValue('workExperience', next)
                            }}
                            className="text-on-surface-variant transition-colors hover:text-error"
                          >
                            <span className="material-symbols-outlined text-lg">delete</span>
                          </button>
                        </div>
                        <div className="mt-3 grid gap-3 sm:grid-cols-2">
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
                              className={inputClass}
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
                              className={inputClass}
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
                              className={inputClass}
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
                              className={inputClass}
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
                              placeholder="Describe your role and responsibilities"
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
                      className="flex items-center gap-1.5 rounded-lg border border-dashed border-outline-variant px-4 py-2.5 font-label-md text-label-md text-primary transition-colors hover:bg-primary-container/20"
                    >
                      <span className="material-symbols-outlined text-lg">add</span>
                      Add experience
                    </button>
                  </div>
                </div>
              </AccordionSection>

              {/* ── Section 5: Education ── */}
              <AccordionSection
                title="Education"
                description="Your educational background — shown to recruiters when you apply."
                icon="school"
                completion={{ filled: educationFilled, total: 2 }}
              >
                <Field label="Highest education level *" visibilityKey="education" visibility={formik.values.profileVisibility} onToggleVisibility={toggleVisibility}>
                  <select
                    name="educationLevel"
                    value={formik.values.educationLevel}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    className={inputClass}
                  >
                    <option value="">Not specified</option>
                    {EDUCATION_LEVELS.map((level) => (
                      <option key={level} value={level}>{EDUCATION_LEVEL_LABELS[level]}</option>
                    ))}
                  </select>
                </Field>
                <Field full label="Education history">
                  <div className="space-y-4">
                    {formik.values.educationHistory.map((entry, idx) => (
                      <div
                        key={idx}
                        className="rounded-lg border border-surface-variant p-4"
                      >
                        <div className="flex items-center justify-between">
                          <span className="font-label-sm text-label-sm font-medium text-on-surface">
                            {(EDUCATION_LEVEL_LABELS[entry.level] ?? entry.level) || `Degree ${idx + 1}`}
                          </span>
                          <button
                            type="button"
                            onClick={() => {
                              const next = [...formik.values.educationHistory]
                              next.splice(idx, 1)
                              formik.setFieldValue('educationHistory', next)
                            }}
                            className="text-on-surface-variant transition-colors hover:text-error"
                          >
                            <span className="material-symbols-outlined text-lg">delete</span>
                          </button>
                        </div>                        <div className="mt-3 grid gap-3 sm:grid-cols-2">
                          <div>
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">Education level *</label>
                            <select
                              value={entry.level}
                              onChange={(e) => {
                                const next = [...formik.values.educationHistory]
                                next[idx] = { ...next[idx], level: e.target.value }
                                formik.setFieldValue('educationHistory', next)
                              }}
                              className={inputClass}
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
                              className={inputClass}
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
                              className={inputClass}
                            />
                            {formik.touched.educationHistory?.[idx]?.degree &&
                              (formik.errors.educationHistory as any)?.[idx]?.degree && (
                                <p className="mt-1 font-label-sm text-label-sm text-error">
                                  {(formik.errors.educationHistory as any)[idx].degree}
                                </p>
                              )}
                          </div>
                          <div>
                            <label className="font-label-sm text-label-sm font-medium text-on-surface">GPA</label>
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
                              className={inputClass}
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
                              className={inputClass}
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
                      className="flex items-center gap-1.5 rounded-lg border border-dashed border-outline-variant px-4 py-2.5 font-label-md text-label-md text-primary transition-colors hover:bg-primary-container/20"
                    >
                      <span className="material-symbols-outlined text-lg">add</span>
                      Add degree
                    </button>
                  </div>
                </Field>
              </AccordionSection>

              {/* ── Section 6: Links & resume ── */}
              <AccordionSection
                title="Links & resume"
                description="Where recruiters can find you online."
                icon="link"
                completion={{ filled: linksFilled, total: 4 }}
              >
                <div className="md:col-span-2">
                  <label className="font-label-sm text-label-sm font-medium text-on-surface mb-2 block">
                    Resume (PDF)
                  </label>
                  <input
                    ref={resumeInputRef}
                    type="file"
                    accept="application/pdf"
                    onChange={handleResumeUpload}
                    className="hidden"
                  />
                  {formik.values.resumeUrl ? (
                    <div className="flex flex-col sm:flex-row sm:items-center gap-4 rounded-lg border border-surface-variant bg-surface-container-low p-4">
                      <div className="flex items-center gap-3 min-w-0 flex-1">
                        <span className="material-symbols-outlined text-[24px] text-primary shrink-0">description</span>
                        <div className="min-w-0">
                          <p className="font-label-md text-label-md text-on-surface truncate" title={formik.values.resumeUrl}>{resumeDisplayName}</p>
                          <p className="font-label-sm text-label-sm text-on-surface-variant">PDF uploaded</p>
                        </div>
                      </div>
                      <div className="flex flex-wrap items-center gap-2 shrink-0">
                        <button
                          type="button"
                          onClick={() => resumeInputRef.current?.click()}
                          disabled={resumeUploading}
                          className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low disabled:opacity-50"
                        >
                          {resumeUploading ? (
                            <span className="h-4 w-4 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                          ) : (
                            <span className="material-symbols-outlined text-[18px]">upload</span>
                          )}
                          Change resume
                        </button>
                        <button
                          type="button"
                          onClick={() => formik.setFieldValue('resumeUrl', '')}
                          className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-error-container hover:text-error"
                        >
                          <span className="material-symbols-outlined text-[18px]">delete</span>
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
                          className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-primary transition-colors hover:bg-primary-container/10"
                        >
                          <span className="material-symbols-outlined text-[18px]">open_in_new</span>
                          View resume
                        </button>
                      </div>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => resumeInputRef.current?.click()}
                      disabled={resumeUploading}
                      className="inline-flex items-center gap-2 rounded-lg border-2 border-dashed border-outline-variant bg-surface-container-lowest px-6 py-6 font-label-md text-label-md text-on-surface transition-colors hover:border-primary hover:bg-primary-container/5 disabled:opacity-50 w-full justify-center"
                    >
                      {resumeUploading ? (
                        <span className="h-4 w-4 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
                      ) : (
                        <span className="material-symbols-outlined text-[20px]">upload</span>
                      )}
                      Upload resume (PDF)
                    </button>
                  )}
                  <p className="mt-1.5 font-label-sm text-label-sm text-on-surface-variant/60">
                    PDF only · Max 10MB
                  </p>
                </div>
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
              </AccordionSection>
            </>
          )}

          {/* ═══════════════════════════ RECRUITER SECTIONS ═══════════════════════════ */}
          {isRecruiter && (
            <AccordionSection
              title="Personal information"
              description="Your name and location — shown across the platform."
              icon="person"
              defaultOpen
              completion={{ filled: recruiterPersonalFilled, total: 6 }}
            >
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
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Middle name *"
                  error={formik.touched.middleName ? formik.errors.middleName : undefined}
                >
                  <input
                    name="middleName"
                    value={formik.values.middleName}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="e.g. Kebede"
                    className={inputClass}
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
            </AccordionSection>
          )}

          {isRecruiter && (
            <AccordionSection
              title="Company information"
              description="Your company details — shown on every job post you publish."
              icon="apartment"
              completion={{ filled: recruiterCompanyFilled, total: 10 }}
            >
                {/* ── Master Private Toggle ── */}
                <div className="md:col-span-2">
                  <div className="flex items-center justify-between gap-4 rounded-lg border border-surface-variant bg-surface-container-low p-4">
                    <div className="flex items-center gap-3">
                      <span className="material-symbols-outlined text-xl text-primary">lock</span>
                      <div>
                        <p className="font-label-md text-label-md font-medium text-on-surface">
                          Make company private
                        </p>
                        <p className="font-label-sm text-label-sm text-on-surface-variant">
                          When enabled, talent will see "Confidential Recruiter" instead of your name, and "Confidential Company" instead of your company details.
                        </p>
                      </div>
                    </div>
                    <button
                      type="button"
                      role="switch"
                      aria-checked={formik.values.isCompanyPrivate}
                      aria-label="Toggle company privacy"
                      onClick={toggleCompanyPrivate}
                      className={cn(
                        'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full transition-colors',
                        formik.values.isCompanyPrivate ? 'bg-primary' : 'bg-surface-variant',
                      )}
                    >
                      <span
                        className={cn(
                          'inline-block h-5 w-5 transform rounded-full bg-white shadow-sm transition-transform mt-0.5',
                          formik.values.isCompanyPrivate ? 'translate-x-5' : 'translate-x-0.5',
                        )}
                      />
                    </button>
                  </div>
                </div>

                <Field
                  label="Company name *"
                  visibilityKey="companyName"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  error={formik.touched.companyName ? formik.errors.companyName : undefined}
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
                  label="Industry"
                  visibilityKey="industry"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                >
                  <input
                    name="industry"
                    value={formik.values.industry}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="Technology"
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Company size"
                  visibilityKey="companySize"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                >
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
                  visibilityKey="websiteUrl"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  error={formik.touched.websiteUrl ? formik.errors.websiteUrl : undefined}
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
                <Field
                  label="Company type"
                  visibilityKey="companyType"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                >
                  <select
                    name="companyType"
                    value={formik.values.companyType}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    className={inputClass}
                  >
                    <option value="">Not specified</option>
                    {COMPANY_TYPES.map((ct) => (
                      <option key={ct.value} value={ct.value}>{ct.label}</option>
                    ))}
                  </select>
                </Field>
                <Field
                  label="Founded year"
                  visibilityKey="foundedYear"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  error={formik.touched.foundedYear ? formik.errors.foundedYear : undefined}
                >
                  <input
                    name="foundedYear"
                    type="number"
                    min={1800}
                    max={2100}
                    value={formik.values.foundedYear}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="e.g. 2015"
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Headquarters"
                  visibilityKey="headquarters"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                >
                  <input
                    name="headquarters"
                    value={formik.values.headquarters}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="e.g. Addis Ababa, Ethiopia"
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Phone number"
                  visibilityKey="phoneNumber"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                >
                  <input
                    name="companyPhoneNumber"
                    value={formik.values.companyPhoneNumber}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="+251 91 123 4567"
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Email"
                  visibilityKey="email"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  error={formik.touched.companyEmail ? formik.errors.companyEmail : undefined}
                >
                  <input
                    name="companyEmail"
                    type="email"
                    value={formik.values.companyEmail}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="contact@company.com"
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="LinkedIn URL"
                  visibilityKey="linkedInUrl"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  error={formik.touched.companyLinkedInUrl ? formik.errors.companyLinkedInUrl : undefined}
                >
                  <input
                    name="companyLinkedInUrl"
                    value={formik.values.companyLinkedInUrl}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="https://linkedin.com/company/…"
                    className={inputClass}
                  />
                </Field>
                <Field
                  label="Twitter / X URL"
                  visibilityKey="twitterUrl"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  error={formik.touched.companyTwitterUrl ? formik.errors.companyTwitterUrl : undefined}
                >
                  <input
                    name="companyTwitterUrl"
                    value={formik.values.companyTwitterUrl}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                    placeholder="https://x.com/…"
                    className={inputClass}
                  />
                </Field>
                <Field
                  full
                  label="About the company"
                  visibilityKey="about"
                  visibility={formik.values.companyVisibility}
                  onToggleVisibility={toggleCompanyVisibility}
                  hint="Shown on your profile and every job post."
                >
                  <RichTextEditor
                    value={formik.values.about}
                    onChange={(v) => formik.setFieldValue('about', v)}
                    placeholder="What does your company do?"
                  />
                </Field>
            </AccordionSection>
          )}

          {/* ═══════════════════════════ ADMIN ═══════════════════════════ */}
          {role === 'Admin' && (
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              Admins don't have a role-specific profile section — everything
              above is what the platform stores about you.
            </p>
          )}

          {/* ═══════════════════════════ ACTION BUTTONS ═══════════════════════════ */}            <div className="flex items-center justify-end gap-3 pt-2">
            {(isTalent || isRecruiter) && (
              <button
                type="button"
                onClick={() =>
                  navigate(
                    isTalent
                      ? '/dashboard/talent/profile/preview'
                      : '/dashboard/recruiter/profile/preview',
                  )
                }
                className="inline-flex items-center justify-center gap-2 rounded-lg border border-outline-variant px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-low"
              >
                <span className="material-symbols-outlined text-[18px]">visibility</span>
                Preview profile
              </button>
            )}
            <button
              type="button"
              onClick={handleSaveClick}
              disabled={loading || formik.isSubmitting}
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
                Can't save — fix the errors below
              </h2>
            </div>
            <p className="font-body-md text-body-sm text-on-surface-variant mb-4">
              The following fields need attention before your profile can be saved.
            </p>
            <div className="flex-1 overflow-y-auto space-y-2 pr-1">
              {validationErrors.map((err, i) => (
                <div
                  key={i}
                  className="flex items-start gap-2 rounded-lg border border-error/20 bg-error-container/20 px-3 py-2.5"
                >
                  <span className="material-symbols-outlined text-error text-sm mt-0.5">cancel</span>
                  <div className="min-w-0">
                    <span className="font-label-md text-label-md font-medium text-on-surface">{err.field}</span>
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
                Got it
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
    </DashboardShell>
  )
}
