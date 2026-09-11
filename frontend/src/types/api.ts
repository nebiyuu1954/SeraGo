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
  /** When false, admin API is disabled — sidebar hides admin items, admin routes show disabled screen. */
  adminApiEnabled: boolean
}

/** Response of GET /api/account/profile — mirrors ProfileEndpoints.ProfileResponse. */
export interface ProfileResponse {
  firstName: string
  middleName: string
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
  // Identity / personal
  middleName: string
  phoneNumber: string | null
  dateOfBirth: string | null
  address: string
  // Work experience
  workExperience: string
  // Education
  educationLevel: string
  educationHistory: string
  // Professional context
  currentIndustry: string
  currentProfession: string
  preferredLocations: string
  // Privacy
  profileVisibility: string
  skillVisibility: string
}

export interface RecruiterProfileResponse {
  companyName: string
  industry: string
  companySize: string
  websiteUrl: string
  about: string
  // New attributes
  foundedYear: number | null
  headquarters: string
  phoneNumber: string
  email: string
  companyType: string | null  // PascalCase enum: Public | Private | NonProfit | …
  linkedInUrl: string
  twitterUrl: string
  // Privacy
  companyVisibility: string  // JSON object
  isCompanyPrivate: boolean
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
  middleName?: string
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
  // Identity / personal
  phoneNumber?: string
  dateOfBirth?: string
  address?: string
  // Work experience
  workExperience?: string
  // Education
  educationLevel?: string
  educationHistory?: string
  // Professional context
  currentIndustry?: string
  currentProfession?: string
  preferredLocations?: string
  // Privacy
  profileVisibility?: string
  skillVisibility?: string
}

/** Recruiter section of PUT /api/account/profile. CompanyName is required. */
export interface RecruiterProfileUpdate {
  companyName: string
  industry?: string
  companySize?: string
  websiteUrl?: string
  about?: string
  // New attributes
  foundedYear?: number | null
  headquarters?: string
  phoneNumber?: string
  email?: string
  companyType?: string | null  // PascalCase enum name
  linkedInUrl?: string
  twitterUrl?: string
  // Privacy
  companyVisibility?: string  // JSON object
  isCompanyPrivate?: boolean
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
  | 'internship'
  | 'freelance'
  | 'temporary'
  | 'other'

/** Work arrangement (where the work happens) — mirror of the API's WorkMode. */
export type WorkMode = 'onsite' | 'remote' | 'hybrid'

/** A single job posting — the data item of GET /api/jobs. */
export interface JobResponse {
  id: string
  title: string
  description: string
  company: string
  location: string
  jobType: JobType | string
  /** lowerCamel enum name — "onsite" | "remote" | "hybrid" (only Afriwork carries it). */
  workMode: string
  /** Number of times this job has been viewed. */
  viewCount: number
  url: string
  salary: string
  salaryMin: number | null
  salaryMax: number | null
  salaryCurrency: string | null
  salaryPeriod: string | null
  experienceMinYears: number | null
  experienceMaxYears: number | null
  numberOfPositions: number
  /** UTC ISO-8601, e.g. "2026-09-15T14:00:00Z" — or null. */
  publishedAt: string | null
  /** UTC ISO-8601 — when the source last refreshed (reposted) the listing, or null. */
  refreshedAt: string | null
  deadline: string | null
  status: JobStatus
  isActive: boolean
  isOwner: boolean
  /** null when empty. */
  rejectionReason: string | null
  /** Source website display name, e.g. "EthioJobs" — null for SeraGo-posted jobs. */
  sourceName: string | null
  /** Original listing URL on the source website. */
  sourceUrl: string | null
  /** The source's own id for this listing (dedup key with sourceName). */
  externalId: string | null
  /** Company logo URL from the source — fall back to initials when null. */
  companyLogoUrl: string | null
  /** Canonical sector id — null for uncategorized jobs. */
  sectorId: string | null
  /** Canonical sector display name, e.g. "Technology & IT". */
  sectorName: string | null
  /** Normalized experience level, e.g. "Junior" or "3+ years". */
  experienceLevel: string | null
  /** Skill tags as a JSON array string, e.g. '["React","Node.js"]' — or null. */
  skills: string | null
  /** Source-specific rendering fields — populated from per-site scraper models. */
  // Afriwork
  sourceSectors: string | null
  compensationAmountCents: number | null
  compensationType: string | null
  compensationCurrency: string | null
  entityType: string | null
  // EthioJobs
  sourceCategories: string | null
  // Shared (EthioJobs + HaHu)
  applicationMethod: string | null
  applicationEmail: string | null
  applicationUrl: string | null
  // HaHuJobs
  upstreamSource: string | null
  areaName: string | null
  subSectorName: string | null
  numberOfApplicants: number | null
  // GeezJobs
  employmentText: string | null
  jobTime: string | null
  siteJobType: string | null
  experienceText: string | null
  maxExperienceYears: number | null
  postedText: string | null
  deadlineText: string | null
  // ReporterJobs
  jobTypeText: string | null
  createdAt: string
  updatedAt: string
  /** AI match score 0-100 for this talent's "For You" feed — null when not computed. */
  matchScore: number | null
  /** Profile keywords/skills that matched the job posting. */
  matchedSkills: string[] | null
  /** Job requirements the talent's profile doesn't cover. */
  missingSkills: string[] | null
}

/** A canonical job sector — the vocabulary of GET /api/sectors. */
export interface SectorResponse {
  id: string
  name: string
  slug: string
  isActive: boolean
}

/** Admin view of a sector — includes aliases and how many jobs reference it. */
export interface SectorDetailResponse extends SectorResponse {
  jobCount: number
  aliases: { id: string; alias: string }[]
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

/** The `data` payload of POST /api/jobs/for-you/match ("Run AI matching"). */
export interface ForYouMatchResult {
  /** Jobs whose score was freshly computed this run. */
  scored: number
  /** Jobs whose stored score was already current and simply reused. */
  cached?: number
  total: number
  message?: string | null
}

/** The `data` payload of POST /api/applications/job/{jobId}/match ("Run AI matching"). */
export interface ApplicationsMatchResult {
  /** Applications freshly scored this run. */
  scored: number
  /** Applications whose stored score was already current and reused. */
  cached?: number
  /** Applications that couldn't be scored (e.g. no shared profile data). */
  failed?: number
  total: number
  message?: string | null
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
  /** true → the caller's preferred sectors only (personalized feed). */
  forMe?: boolean
  /** Canonical sector id filter. */
  sectorId?: string
  /** true → jobs without a sector (admin review) — admins only. */
  uncategorized?: boolean
  /** Admin-only: filter by poster user id (for user detail page). */
  postedBy?: string
  /** Comma-separated source display names, e.g. "Afriwork,EthioJobs". */
  source?: string
  /** Exact experience-level match, e.g. "Junior" / "Senior". */
  experienceLevel?: string
  /** Work arrangement: "onsite" | "remote" | "hybrid". */
  workMode?: string
  /** Days: only jobs published within the last N days. */
  postedWithin?: number
  /** Days: only jobs whose deadline falls within the next N days. */
  closingWithin?: number
  /** Minimum salary (inclusive) for range-based filtering. */
  salaryMin?: number
  /** Maximum salary (inclusive) for range-based filtering. */
  salaryMax?: number
}

/** Body of POST /api/jobs and PUT /api/jobs/{id}. */
export interface JobWriteRequest {
  title: string
  description?: string
  company?: string
  location?: string
  jobType?: string
  /** Work arrangement: "onsite" | "remote" | "hybrid" (blank = onsite). */
  workMode?: string
  url?: string
  salary?: string
  salaryMin?: number
  salaryMax?: number
  salaryCurrency?: string
  salaryPeriod?: string
  experienceMinYears?: number
  experienceMaxYears?: number
  numberOfPositions?: number
  /** UTC ISO-8601 datetime. */
  publishedAt?: string
  deadline?: string
  /** true → hidden draft; false → submit for review (admins publish directly). */
  saveAsDraft?: boolean
  /** Canonical sector id (admin/import use; recruiters usually omit). */
  sectorId?: string
}

/** Body of PATCH /api/jobs/{id}/sector — admin (re)assigns a job's sector. */
export interface SetJobSectorRequest {
  /** null clears the job's sector (back to the review queue). */
  sectorId: string | null
}

/** A saved job card in GET /api/saved-jobs. */
export interface SavedJobItem {
  savedJobId: string
  /** The referenced job — null once the job was deleted by the lifecycle cleanup. */
  jobId: string | null
  title: string
  company: string
  sourceName: string | null
  sourceUrl: string | null
  companyLogoUrl: string | null
  location: string
  salary: string
  /** UTC ISO-8601 — or null. */
  deadline: string | null
  /** open | deadlinePassed | removed. */
  status: 'open' | 'deadlinePassed' | 'removed'
  /** UTC ISO-8601 — when the card leaves the list (deadline + 7 days); the countdown target. */
  removedAt: string | null
  savedAt: string
}

/** The `data` payload of GET /api/saved-jobs. */
export interface SavedJobListData {
  items: SavedJobItem[]
  /** All saved rows — including snapshots of jobs already deleted (the lifetime stat). */
  totalSaved: number
  /** How many of the listed cards are deadlinePassed or removed (drives the banner). */
  affectedCount: number
}

/** Result of POST /api/admin/sync/scraped-jobs. */
export interface SyncScrapedJobsResult {
  inserted: number
  updated: number
  unchanged: number
  uncategorized: number
  /** Jobs hidden because their listing vanished from the source. */
  deactivated: number
  unknownSectors: string[]
}

/** One ranked entry of the top-sectors stat. */
export interface SectorCountStat {
  name: string
  count: number
}

/** One ranked entry of the top-websites stat. */
export interface WebsiteStat {
  slug: string
  name: string
  itemsFound: number
  itemsInserted: number
  runCount: number
  apiHits: number
}

// ---------------------------------------------------------------- Applications

/** A job application in GET /api/applications. */
export interface ApplicationResponse {
  id: string
  jobId: string
  jobTitle: string
  jobCompany: string
  jobLocation: string | null
  jobSourceName: string | null
  userId: string
  applicantName: string
  applicantEmail: string
  applicantHeadline: string | null
  applicantAvatarUrl: string | null
  coverLetter: string | null
  resumeUrl: string | null
  /** lowerCamel: pending | reviewed | accepted | rejected */
  status: ApplicationStatus
  appliedAt: string
  statusUpdatedAt: string | null
  /** True when the talent shared their full profile; false for resume-only applications. */
  profileShared: boolean
  /** JSON snapshot of the talent's visible profile data at apply time. */
  profileSnapshot: string | null
  /** Salary string from the job posting, e.g. "10,000 - 15,000 ETB/month". */
  jobSalary: string | null
  /** Deadline ISO-8601 datetime of the job posting, or null. */
  jobDeadline: string | null
  /** Company logo image URL, or null. */
  companyLogoUrl: string | null
  /** AI match score 0-100 — null when the AI service hasn't scored it yet. */
  matchScore: number | null
  /** Profile keywords/skills that matched the job posting. */
  matchedSkills: string[] | null
  /** Job requirements the talent's profile doesn't cover. */
  missingSkills: string[] | null
}

/** Application status — lowerCamel enum name. */
export type ApplicationStatus = 'pending' | 'reviewed' | 'interview' | 'hired' | 'rejected' | 'shortlisted'

/** The `data` payload of GET /api/applications. */
export interface ApplicationListData {
  items: ApplicationResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
}

/** Body of POST /api/applications. */
export interface ApplyRequest {
  jobId: string
  coverLetter?: string
  resumeUrl?: string
  shareProfile?: boolean
}

/** Body of PATCH /api/applications/{id}/status. */
export interface UpdateApplicationStatusRequest {
  status: ApplicationStatus
  recruiterNotes?: string
}

/** Per-job stats for the recruiter applications dashboard. */
export interface RecruiterJobStats {
  jobId: string
  jobTitle: string
  jobCompany: string
  jobLocation: string | null
  jobType: string
  viewCount: number
  pendingCount: number
  reviewedCount: number
  interviewCount: number
  hiredCount: number
  rejectedCount: number
  totalApplications: number
}

/** Result of GET /api/admin/stats/top — top sectors + websites for a period. */
export interface StatsTopResponse {
  period: 'day' | 'week' | 'month' | 'year'
  start: string
  end: string
  topSectors: SectorCountStat[]
  topWebsites: WebsiteStat[]
}

/** The `data` payload of GET /api/admin/jobs/views. */
/** The `data` payload of GET /api/admin/scraper/week-stats. */
export interface ScraperWeekStatsList {
  weeks: ScraperWeekSummary[]
  total: number
}

/** One week summary in the scraper week-stats list. */
export interface ScraperWeekSummary {
  periodStart: string
  periodEnd: string
  daysWithRuns: number
  runCount: number
  apiHits: number
  itemsFound: number
  itemsInserted: number
  itemsUpdated: number
  itemsSkipped: number
  statusSummary: string
  worstDay: string | null
}

/** The `data` payload of GET /api/admin/scraper/week/{periodStart}. */
export interface ScraperWeekDetail {
  periodStart: string
  periodEnd: string
  periodLabel: string
  daysWithRuns: number
  totalRunCount: number
  totalApiHits: number
  totalItemsFound: number
  totalItemsInserted: number
  totalItemsUpdated: number
  totalItemsSkipped: number
  statusSummary: string
  days: ScraperDay[]
  sources: ScraperWeekSource[]
  topErrors: ScraperErrorSummary[]
  archiveNote: string | null
}

/** One day in a scraper week detail. */
export interface ScraperDay {
  day: string
  status: string
  runCount: number
  apiHits: number
  itemsFound: number
  itemsInserted: number
  itemsUpdated: number
  itemsSkipped: number
  websitesCount: number
  websites: ScraperDayWebsite[]
  runs: ScraperRunSummary[]
}

/** One per-site bucket in a scraper day. */
export interface ScraperDayWebsite {
  source: string
  name: string
  table: string
  logId: string
  status: string
  runCount: number
  apiHits: number
  itemsFound: number
  itemsInserted: number
  itemsUpdated: number
  itemsSkipped: number
}

/** One scrape-run summary in a scraper day. */
export interface ScraperRunSummary {
  run: number
  time: string
  hits: number
  found: number
  inserted: number
  updated: number
  skipped: number
  status: string
  message: string | null
  errors: string | null
}

/** One per-source breakdown in a scraper week. */
export interface ScraperWeekSource {
  source: string
  name: string
  daysActive: number
  runCount: number
  apiHits: number
  itemsFound: number
  itemsInserted: number
  itemsUpdated: number
  itemsSkipped: number
  statusSummary: string
}

/** One error summary in a scraper week. */
export interface ScraperErrorSummary {
  message: string
  count: number
}

export interface JobViewsData {
  items: JobViewItem[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
}

/** One job in the admin job-views list. */
export interface JobViewItem {
  id: string
  title: string
  company: string
  location: string | null
  sourceName: string | null
  viewCount: number
  status: string
  createdAt: string
  publishedAt: string | null
  deadline: string | null
  sectorName: string | null
  salary: string | null
}

/** The `data` payload of GET /api/admin/jobs/views/stats — aggregate view analytics. */
export interface JobViewsStatsResponse {
  totalViews: number
  viewsToday: number
  viewsThisWeek: number
  viewsThisMonth: number
  viewsThisYear: number
  bySource: { name: string; value: number }[]
  bySector: { name: string; value: number }[]
  perDay: { date: string; views: number }[]
  perMonth: { name: string; value: number }[]
  topJobs: {
    id: string
    title: string
    company: string
    sourceName: string | null
    sectorName: string | null
    viewCount: number
    status: string
  }[]
}


// ---------------------------------------------------------------- Admin

export interface AdminUserResponse {
  id: string
  firstName: string
  middleName: string
  lastName: string
  email: string
  userType: string
  isActive: boolean
  emailConfirmed: boolean
  avatarUrl: string | null
  city: string | null
  country: string | null
  createdAt: string
  /** Only present on the detail endpoint — the list leaves it null/absent. */
  activity?: AdminUserActivity | null
}

/**
 * Role-specific engagement summary on the admin user detail page.
 *
 * `activeDaysThisWeek` / `activeDaysThisMonth` are derived from the user's own
 * recorded actions (job views + applications) — there is no login audit table,
 * so "online" means "did something we recorded on that UTC day".
 */
export interface AdminUserActivity {
  role: string
  profileCompletionPercent: number
  profileComplete: boolean
  missingProfileFields: string[]
  activeDaysThisWeek: number
  activeDaysThisMonth: number
  lastActiveAt: string | null
  talent: AdminTalentActivity | null
  recruiter: AdminRecruiterActivity | null
}

/** Job-seeking numbers — present only for Talent users. */
export interface AdminTalentActivity {
  jobsViewed: number
  applicationsSubmitted: number
}

/** Hiring numbers — present only for Recruiter users. */
export interface AdminRecruiterActivity {
  jobsPosted: number
  jobsDraft: number
  jobsPendingApproval: number
  jobsPublished: number
  applicationsReceived: number
  uniqueApplicants: number
}

export interface AdminUserListData {
  items: AdminUserResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
}

export interface AdminStatsTopResponse {
  period: string
  start: string
  end: string
  topSectors: { name: string; count: number }[]
  topWebsites: { slug: string; name: string; itemsFound: number; itemsInserted: number; runCount: number; apiHits: number }[]
}

// ---------------------------------------------------------------- Admin AI classification

/** Token + job-count rollup for one time window (or all time). */
export interface AiClassificationWindowStats {
  jobsProcessed: number
  jobsSuccessful: number
  jobsUncategorized: number
  tokensSent: number
  tokensReceived: number
  totalTokens: number
  latencyMsAvg: number
}

/** All-time cumulative totals. */
export interface AiClassificationSummary {
  totalJobsEver: number
  totalSuccessfulEver: number
  totalUncategorizedEver: number
  totalTokensSentEver: number
  totalTokensReceivedEver: number
  totalTokensEver: number
}

/** One day's LLM usage. */
export interface AiClassificationDay {
  date: string
  jobsProcessed: number
  jobsSuccessful: number
  jobsUncategorized: number
  tokensSent: number
  tokensReceived: number
  totalTokens: number
}

/** The `data` payload of GET /api/admin/ai/classification/stats. */
export interface AiClassificationStatsResponse {
  generatedAt: string
  days: number
  windows: {
    today: AiClassificationWindowStats
    thisWeek: AiClassificationWindowStats
    thisMonth: AiClassificationWindowStats
    allTime: AiClassificationWindowStats
  }
  summary: AiClassificationSummary
  daily: AiClassificationDay[]
  message: string | null
}

/** One classify attempt in the per-job audit trail. */
export interface AiClassificationJobItem {
  logId: number
  jobId: string
  jobTitle: string | null
  jobCompany: string | null
  sectorSlug: string | null
  sectorName: string | null
  originalSectorSlug: string | null
  originalSectorName: string | null
  confidence: number | null
  reasoning: string | null
  categorized: boolean
  aiClassifiedAt: string
  tokensSent: number
  tokensReceived: number
  totalTokens: number
  latencyMs: number | null
}

/** The `data` payload of GET /api/admin/ai/classification/jobs. */
export interface AiClassificationJobsResponse {
  page: number
  pageSize: number
  total: number
  totalPages: number
  items: AiClassificationJobItem[]
  message: string | null
}

// ---------------------------------------------------------------- Admin Stats Overview

export interface AdminStatsOverviewResponse {
  users: {
    total: number
    activeToday: number
    activeThisWeek: number
    activeThisMonth: number
    newToday: number
    newThisWeek: number
    newThisMonth: number
    byRole: { talent: number; recruiter: number; admin: number }
  }
  scraper: {
    day: string | null
    status: string
    totalRunCount: number
    totalApiHits: number
    totalItemsFound: number
    totalItemsInserted: number
    totalItemsUpdated: number
    totalItemsSkipped: number
    sitesScraped: number
    sites: Array<{
      source: string
      name: string
      status: string
      runCount: number
      apiHits: number
      itemsFound: number
      itemsInserted: number
      itemsUpdated: number
      itemsSkipped: number
    }>
  }
  jobs: {
    total: number
    published: number
    pendingApproval: number
    drafts: number
    rejected: number
    totalViews: number
    seragoJobs: number
    viewsToday: number
    viewsThisWeek: number
    viewsThisMonth: number
    viewsThisYear: number
    pendingJobs: Array<{
      id: string
      title: string
      company: string
      sourceName: string | null
      sectorName: string | null
      postedByName: string
      createdAt: string
    }>
  }
  applications: {
    total: number
    pending: number
    reviewed: number
    shortlisted: number
    interview: number
    hired: number
    rejected: number
  }
  lastSync: {
    ranAt: string | null
    inserted: number
    updated: number
    unchanged: number
    uncategorized: number
    deactivated: number
    schedulerEnabled: boolean
  }
}
