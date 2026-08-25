/**
 * Shape of the JSON `profileSnapshot` stored on each application.
 *
 * Built by `ApplicationEndpoints.BuildProfileSnapshot` — only fields the
 * talent opted into sharing (via `profileVisibility`) are present.
 * All fields are optional because the talent controls visibility.
 */
export interface ProfileSnapshot {
  // Identity
  firstName?: string
  middleName?: string
  lastName?: string
  phone?: string
  dateOfBirth?: string
  avatarUrl?: string
  address?: string
  city?: string
  country?: string

  // Professional
  headline?: string
  about?: string
  experienceLevel?: string
  yearsOfExperience?: number
  skills?: string[]
  currentIndustry?: string
  currentProfession?: string
  desiredRoles?: string[]

  // Education
  educationLevel?: string
  /** JSON-encoded `EducationEntry[]` — parse with `JSON.parse()`. */
  educationHistory?: string

  // Links
  linkedinUrl?: string
  githubUrl?: string
  portfolioUrl?: string
  resumeUrl?: string

  // Preferences
  /** JSON-encoded `string[]` — parse with `JSON.parse()`. */
  preferredLocations?: string
}
