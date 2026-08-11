/** Standard envelope returned by the SeraGo API. */
export interface ApiResponse<T> {
  data: T
  message?: string
}

/**
 * Shape of an error payload returned by the SeraGo API.
 * Aufy returns RFC 7807 ProblemDetails (`title`/`detail`/`errors`);
 * plain `{ message }` payloads are tolerated for other endpoints.
 */
export interface ApiErrorPayload {
  type?: string
  title?: string
  status?: number
  detail?: string
  message?: string
  /** Field → list of validation messages (ASP.NET Identity / DataAnnotations). */
  errors?: Record<string, string[]>
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
