import { useNavigate } from 'react-router-dom'
import { useProfileQuery, useRequireRole } from '../../../hooks'
import type { ProfileSnapshot } from '../../../types/profileSnapshot.ts'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import TalentProfileView from '../../../components/talent-profile/TalentProfileView.tsx'

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

  // Build a ProfileSnapshot from the talent's own profile data
  const talent = profile?.talent
  const snap: ProfileSnapshot = {
    firstName: profile?.firstName,
    middleName: profile?.middleName,
    lastName: profile?.lastName,
    avatarUrl: profile?.avatarUrl,
    city: profile?.city,
    country: profile?.country,
    // Professional
    headline: talent?.headline ?? undefined,
    about: talent?.about ?? undefined,
    experienceLevel: talent?.experienceLevel ?? undefined,
    yearsOfExperience: talent?.yearsOfExperience ?? undefined,
    skills: talent?.skills,
    currentIndustry: talent?.currentIndustry,
    currentProfession: talent?.currentProfession,
    desiredRoles: talent?.desiredRoles,
    // Education
    educationLevel: talent?.educationLevel,
    educationHistory: talent?.educationHistory,
    // Links
    linkedinUrl: talent?.linkedInUrl,
    githubUrl: talent?.githubUrl,
    portfolioUrl: talent?.portfolioUrl,
    resumeUrl: talent?.resumeUrl,
    // Preferences
    preferredLocations: talent?.preferredLocations,
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
                This is how your profile appears to recruiters.{' '}
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
