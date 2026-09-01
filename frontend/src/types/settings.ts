/** Settings API response — the full settings blob + version for optimistic concurrency. */
export interface SettingsResponse {
  settings: string // JSON string — parsed by the frontend
  version: number
}

/** Parsed settings shape — the versioned structure the frontend uses. */
export interface UserSettings {
  version: number
  account: AccountSettings
  notifications: NotificationSettings
  security: SecuritySettings
  ai: AISettings
}

/** Account-level preferences. */
export interface AccountSettings {
  /** BCP 47 language tag, e.g. "en", "am". Default: "en". */
  language: string
}

/** Per-channel toggles for a single notification type. */
export interface NotificationChannelToggles {
  email: boolean
  inApp: boolean
  telegram: boolean
}

/** Notification preferences — each type has email / in-app / telegram toggles. */
export interface NotificationSettings {
  // Talent notifications
  /** New jobs matching the talent's profile. */
  jobAlerts: NotificationChannelToggles
  /** Application status changed (accepted, rejected, etc.). */
  applicationUpdates: NotificationChannelToggles
  // Recruiter notifications
  /** New applications received. */
  newApplications: NotificationChannelToggles
  /** Job post status changes (approved, rejected). */
  jobStatusChanges: NotificationChannelToggles
  // Admin notifications
  /** System-wide alerts. */
  systemAlerts: NotificationChannelToggles
  // Email-only extras
  /** Weekly digest of new jobs / activity. */
  weeklyDigest: boolean
  /** Weekly summary of activity (recruiter). */
  weeklySummary: boolean
  /** Marketing / product update emails. */
  marketing: boolean
  /** Weekly platform report (admin). */
  weeklyReport: boolean
}

/** Security-related settings. */
export interface SecuritySettings {
  /** Whether the user has a password set (Google-only accounts don't). */
  hasPassword: boolean
  /** Whether two-factor authentication is enabled (placeholder). */
  twoFactorEnabled: boolean
}

/** AI feature toggles — placeholder for when the AI service is integrated. */
export interface AISettings {
  /** Enable AI-powered job matching. */
  enableAiMatching: boolean
  /** Enable AI cover letter generation. */
  enableCoverLetter: boolean
  /** Enable AI resume parsing. */
  enableResumeParsing: boolean
}

/** Helper to create channel toggles with defaults. */
function channels(
  email = true,
  inApp = true,
  telegram = false,
): NotificationChannelToggles {
  return { email, inApp, telegram }
}

/** Default settings applied when a user has no saved settings. */
export const DEFAULT_SETTINGS: UserSettings = {
  version: 1,
  account: {
    language: 'en',
  },
  notifications: {
    // Talent
    jobAlerts: channels(true, true, false),
    applicationUpdates: channels(true, true, false),
    // Recruiter
    newApplications: channels(true, true, false),
    jobStatusChanges: channels(true, true, false),
    // Admin
    systemAlerts: channels(true, true, false),
    // Email-only
    weeklyDigest: true,
    weeklySummary: true,
    marketing: false,
    weeklyReport: true,
  },
  security: {
    hasPassword: false,
    twoFactorEnabled: false,
  },
  ai: {
    enableAiMatching: true,
    enableCoverLetter: true,
    enableResumeParsing: true,
  },
}

/** Available languages for the language selector. */
export const LANGUAGE_OPTIONS = [
  { value: 'en', label: 'English' },
  { value: 'am', label: 'አማርኛ (Amharic)' },
] as const
