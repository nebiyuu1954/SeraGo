import { useNavigate } from 'react-router-dom'
import { useProfileQuery, useRequireRole } from '../../../hooks'
import type { ProfileSnapshot } from '../../../types/profileSnapshot.ts'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import TalentProfileView from '../../../components/talent-profile/TalentProfileView.tsx'

// ────────────────────── helpers ──────────────────────

/** Parse the talent's profileVisibility JSON into a map. */
function parseVisibility(raw: string | null | undefined): Record<string, boolean> {
  if (!raw || raw === '{}') return {}
  try {
    return JSON.parse(raw) as Record<string, boolean>
  } catch {
    return {}
  }
}

/** Parse the talent's skillVisibility JSON into a map. */
function parseSkillVisibility(raw: string | null | undefined): Record<string, boolean> {
  if (!raw || raw === '{}') return {}
  try {
    return JSON.parse(raw) as Record<string, boolean>
  } catch {
    return {}
  }
}

/** Check if a visibility key is on (defaults to true when missing). */
function isVisible(vis: Record<string, boolean>, key: string): boolean {
  return !(key in vis) || vis[key]
}

// ────────────────────── page ──────────────────────

export default function TalentProfilePreviewPage() {
  const navigate = useNavigate()
  const auth = useRequireRole('Talent')
  const { profile, isLoading } = useProfileQuery(auth.status === 'authenticated')

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

  // Build a ProfileSnapshot respecting the talent's visibility settings.
  const talent = profile?.talent
  const vis = parseVisibility(talent?.profileVisibility)
  const skillVis = parseSkillVisibility(talent?.skillVisibility)

  const snap: ProfileSnapshot = {
    firstName: profile?.firstName,
    // Identity — only include if the talent opted in
    middleName: isVisible(vis, 'middleName') ? (profile?.middleName ?? undefined) : undefined,
    lastName: profile?.lastName,
    avatarUrl: isVisible(vis, 'avatar') ? (profile?.avatarUrl ?? undefined) : undefined,
    city: isVisible(vis, 'city') ? (profile?.city ?? undefined) : undefined,
    country: isVisible(vis, 'country') ? (profile?.country ?? undefined) : undefined,
    phone: isVisible(vis, 'phone') ? (talent?.phoneNumber ?? undefined) : undefined,
    dateOfBirth: isVisible(vis, 'dateOfBirth') ? (talent?.dateOfBirth ?? undefined) : undefined,
    address: isVisible(vis, 'address') ? (talent?.address ?? undefined) : undefined,
    // Professional
    about: isVisible(vis, 'about') ? (talent?.about ?? undefined) : undefined,
    experienceLevel: isVisible(vis, 'experience') ? (talent?.experienceLevel ?? undefined) : undefined,
    yearsOfExperience: isVisible(vis, 'experience') ? (talent?.yearsOfExperience ?? undefined) : undefined,
    workMode: isVisible(vis, 'workMode') ? (talent?.workMode ?? undefined) : undefined,
    availability: isVisible(vis, 'availability') ? (talent?.availability ?? undefined) : undefined,
    currentIndustry: isVisible(vis, 'currentIndustry') ? (talent?.currentIndustry ?? undefined) : undefined,
    currentProfession: isVisible(vis, 'currentProfession') ? (talent?.currentProfession ?? undefined) : undefined,
    // Skills — filtered by per-skill visibility
    skills: isVisible(vis, 'skills')
      ? talent?.skills?.filter((s) => !('skillVisibility' in skillVis) || (skillVis[s] ?? true))
      : undefined,
    desiredRoles: isVisible(vis, 'desiredRoles') ? (talent?.desiredRoles ?? undefined) : undefined,
    // Work experience
    workExperience: isVisible(vis, 'experience') ? (talent?.workExperience ?? undefined) : undefined,
    // Education
    educationLevel: isVisible(vis, 'education') ? (talent?.educationLevel ?? undefined) : undefined,
    educationHistory: isVisible(vis, 'education') ? (talent?.educationHistory ?? undefined) : undefined,
    // Links
    linkedinUrl: isVisible(vis, 'linkedin') ? (talent?.linkedInUrl ?? undefined) : undefined,
    githubUrl: isVisible(vis, 'github') ? (talent?.githubUrl ?? undefined) : undefined,
    portfolioUrl: isVisible(vis, 'portfolio') ? (talent?.portfolioUrl ?? undefined) : undefined,
    resumeUrl: isVisible(vis, 'resume') ? (talent?.resumeUrl ?? undefined) : undefined,
    // Preferences
    preferredLocations: isVisible(vis, 'preferredLocations') ? (talent?.preferredLocations ?? undefined) : undefined,
  }

  return (
    <DashboardShell role="Talent" authUser={auth.user}>
      {isLoading && (
        <div className="flex min-h-[50svh] items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      )}

      {!isLoading && profile && (
        <>
          <TalentProfileView
            snapshot={snap}
            headerBadge="Available for hire"
            backLabel="Back to profile"
            backTo="/dashboard/talent/profile"
            topRight={
              <span className="font-body-md text-body-md text-on-surface-variant">
                This is how your profile appears to employers.{' '}
                <button
                  type="button"
                  onClick={() => navigate('/dashboard/talent/profile')}
                  className="font-label-md text-label-md text-primary underline underline-offset-4 hover:opacity-80 transition-opacity"
                >
                  Edit profile
                </button>
              </span>
            }
          />
        </>
      )}
    </DashboardShell>
  )
}
