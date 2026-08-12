/** Status header on every API response. */
export type ApiResponseStatus = 'Success' | 'Failed'

/**
 * Standard envelope returned by the SeraGo API. The response-shaping
 * middleware wraps every endpoint: success = `{ responseStatus: "Success",
 * messageCode: null, message: null, data }`; failure = `{ responseStatus:
 * "Failed", messageCode, message, data: null }`.
 */
export interface ApiResponse<T> {
  responseStatus: ApiResponseStatus
  /** Stable machine-readable code, e.g. "NOT_FOUND" / "VALIDATION_ERROR". */
  messageCode: string | null
  /** Human-readable message (ProblemDetails title/detail/errors flattened). */
  message: string | null
  data: T | null
}

/** Shape of an error payload — the Failed envelope. */
export interface ApiErrorPayload {
  responseStatus: 'Failed'
  messageCode: string | null
  message: string | null
  data: null
}

/** Example health-check response. */
export interface HealthResponse {
  status: 'ok' | 'degraded'
  timestamp: string
}

/** Roles a user may self-select at registration (mirrors backend Roles.SelfService). */
export type SignUpRole = 'Talent' | 'Recruiter'

/** Body of POST /api/auth/signup (Aufy SignUpRequest + SeraGo profile fields). */
export interface SignUpRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  role: SignUpRole
}

/** Response of POST /api/auth/signup. */
export interface SignUpResponse {
  requiresEmailConfirmation: boolean
}

/**
 * Body of POST /api/auth/signup/external — role + password step for new Google
 * users. First/last name are optional: the backend pulls them from Google's
 * claims. The password is required so the account is created WITH one from
 * the start (email + password login works on any device).
 */
export interface SignUpExternalRequest {
  firstName?: string
  lastName?: string
  role: SignUpRole
  password: string
}

/** Body of POST /api/account/password/forgot. */
export interface ForgotPasswordRequest {
  email: string
}

/** Body of POST /api/account/password/reset. */
export interface ResetPasswordRequest {
  /** Base64-url-encoded token from the email link (?code=…). */
  code: string
  email: string
  password: string
}

/**
 * Response of POST /api/auth/token (Aufy AccessTokenResponse).
 *
 * NOTE: the released Aufy 1.0.0 package serializes this via a source-generated
 * JsonSerializerContext (not ASP.NET's camelCase web defaults), so the wire
 * format is PascalCase: { TokenType, AccessToken, ExpiresIn, RefreshToken }.
 *
 * `RefreshToken` is null for the external (Google) flows: those deliver the
 * refresh token as the httpOnly `Aufy.RefreshToken` cookie instead of in the
 * body (only POST /api/auth/token returns it in the body). `AccessToken` is
 * always present in the body for the flows this app uses (we never pass
 * `?useCookie=true`).
 */
export interface AuthTokenResponse {
  TokenType: string
  AccessToken: string
  ExpiresIn: number
  RefreshToken: string | null
}

/** Body of POST /api/auth/token. */
export interface SignInRequest {
  email: string
  password: string
}

/** Response of GET /api/auth/whoami. */
export interface WhoAmIResponse {
  username: string | null
  email: string | null
  roles: string[]
  /** False until the account email is confirmed — blocks dashboard access. */
  emailConfirmed: boolean
}

/** Response of GET /api/account/profile — mirrors ProfileEndpoints.ProfileResponse. */
export interface ProfileResponse {
  firstName: string
  lastName: string
  email: string
  role: string
  avatarUrl: string
  city: string
  country: string
  /** True when the account has a password (Google-only accounts don't). */
  hasPassword: boolean
  talent: TalentProfileResponse | null
  recruiter: RecruiterProfileResponse | null
  completion: ProfileCompletionResponse | null
}

/** Role-specific section of the profile — kept minimal until the profile UI lands. */
export interface TalentProfileResponse {
  headline: string
  about: string
  experienceLevel: string | null
  yearsOfExperience: number | null
  desiredRoles: string[]
  skills: string[]
  desiredJobTypes: string[]
  workMode: string | null
  availability: string | null
  resumeUrl: string
  linkedInUrl: string
  githubUrl: string
  portfolioUrl: string
}

export interface RecruiterProfileResponse {
  companyName: string
  companyLogoUrl: string
  industry: string
  companySize: string
  websiteUrl: string
  about: string
}

export interface ProfileCompletionResponse {
  isComplete: boolean
  percentComplete: number
  missingFields: string[]
}

/** Body of POST /api/account/password/set — first-time password for Google users. */
export interface SetPasswordRequest {
  password: string
}

/**
 * Body of PUT /api/account/profile — mirrors ProfileEndpoints.UpdateProfileRequest.
 *
 * Common fields (null = leave unchanged). The role-specific section matching
 * the caller's role is applied as a full replace: strings null → "", lists
 * null → [], enums null → unset. Send the whole form and the result matches.
 */
export interface UpdateProfileRequest {
  firstName?: string
  lastName?: string
  avatarUrl?: string
  city?: string
  country?: string
  talent?: TalentProfileUpdate
  recruiter?: RecruiterProfileUpdate
}

/** Talent section of PUT /api/account/profile. Enums are PascalCase names. */
export interface TalentProfileUpdate {
  headline?: string
  about?: string
  /** Entry | Junior | Mid | Senior | Lead — null/blank unsets. */
  experienceLevel?: string | null
  yearsOfExperience?: number | null
  desiredRoles?: string[]
  skills?: string[]
  /** PascalCase JobType names: FullTime | PartTime | Contract | ... */
  desiredJobTypes?: string[]
  /** Onsite | Remote | Hybrid — null/blank unsets. */
  workMode?: string | null
  /** Immediate | WithinTwoWeeks | WithinOneMonth | MoreThanOneMonth — null/blank unsets. */
  availability?: string | null
  resumeUrl?: string
  linkedInUrl?: string
  githubUrl?: string
  portfolioUrl?: string
}

/** Recruiter section of PUT /api/account/profile. CompanyName is required. */
export interface RecruiterProfileUpdate {
  companyName: string
  companyLogoUrl?: string
  industry?: string
  companySize?: string
  websiteUrl?: string
  about?: string
}

// ---------------------------------------------------------------- Jobs

/** Job lifecycle status — lowerCamel, matches the API's JobStatus. */
export type JobStatus = 'draft' | 'pendingApproval' | 'published' | 'rejected'

/** Job type — lowerCamel, matches the API's JobType. */
export type JobType =
  | 'fullTime'
  | 'partTime'
  | 'contract'
  | 'contractual'
  | 'remote'
  | 'internship'
  | 'freelance'
  | 'temporary'
  | 'other'

/** A single job posting — the data item of GET /api/jobs. */
export interface JobResponse {
  id: string
  title: string
  description: string
  company: string
  location: string
  jobType: JobType | string
  url: string
  salary: string
  /** UTC ISO-8601, e.g. "2026-09-15T14:00:00Z" — or null. */
  publishedAt: string | null
  deadline: string | null
  status: JobStatus
  isActive: boolean
  isOwner: boolean
  /** null when empty. */
  rejectionReason: string | null
  createdAt: string
  updatedAt: string
}

export interface PaginationResponse {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
}

/** The `data` payload of GET /api/jobs. */
export interface JobListData {
  items: JobResponse[]
  pagination: PaginationResponse
}

/** Query parameters of GET /api/jobs. */
export interface JobListParams {
  q?: string
  jobType?: string
  location?: string
  /** newest | oldest | title_asc | title_desc | deadline */
  sort?: string
  page?: number
  pageSize?: number
  /** true → the caller's own jobs (any status) — owner only. */
  mine?: boolean
  /** Status filter — admins, or combined with mine. */
  status?: string
  /** Include inactive jobs — admins only. */
  includeInactive?: boolean
}

/** Body of POST /api/jobs and PUT /api/jobs/{id}. */
export interface JobWriteRequest {
  title: string
  description?: string
  company?: string
  location?: string
  jobType?: string
  url?: string
  salary?: string
  /** UTC ISO-8601 datetime. */
  publishedAt?: string
  deadline?: string
  /** true → hidden draft; false → submit for review (admins publish directly). */
  saveAsDraft?: boolean
}
