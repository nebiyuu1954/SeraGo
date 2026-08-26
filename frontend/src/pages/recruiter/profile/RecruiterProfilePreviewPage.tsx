import { useNavigate } from 'react-router-dom'
import { useProfileQuery, useRequireRole } from '../../../hooks'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import RecruiterProfileView from '../../../components/recruiter-profile/RecruiterProfileView.tsx'
import type { RecruiterProfileData } from '../../../components/recruiter-profile/RecruiterProfileView.tsx'

// ────────────────────── page ──────────────────────

export default function RecruiterProfilePreviewPage() {
  const navigate = useNavigate()
  const auth = useRequireRole('Recruiter')
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

  // Build the view model from the recruiter's own profile data
  const recruiter = profile?.recruiter
  const data: RecruiterProfileData = {
    firstName: profile?.firstName,
    lastName: profile?.lastName,
    avatarUrl: profile?.avatarUrl,
    city: profile?.city,
    country: profile?.country,
    companyName: recruiter?.companyName ?? undefined,
    industry: recruiter?.industry ?? undefined,
    companySize: recruiter?.companySize ?? undefined,
    websiteUrl: recruiter?.websiteUrl ?? undefined,
    about: recruiter?.about ?? undefined,
    // New attributes
    foundedYear: recruiter?.foundedYear ?? null,
    headquarters: recruiter?.headquarters ?? undefined,
    phoneNumber: recruiter?.phoneNumber ?? undefined,
    email: recruiter?.email ?? undefined,
    companyType: recruiter?.companyType ?? null,
    linkedInUrl: recruiter?.linkedInUrl ?? undefined,
    twitterUrl: recruiter?.twitterUrl ?? undefined,
    // Privacy
    companyVisibility: recruiter?.companyVisibility ?? undefined,
    isCompanyPrivate: recruiter?.isCompanyPrivate ?? false,
  }

  return (
    <DashboardShell role="Recruiter" authUser={auth.user}>
      {isLoading && (
        <div className="flex min-h-[50svh] items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      )}

      {!isLoading && profile && (
        <RecruiterProfileView
          data={data}
          backLabel="Back to profile"
          backTo="/dashboard/recruiter/profile"
          topRight={
            <span className="font-body-md text-body-md text-on-surface-variant">
              This is how your profile appears to talents.{' '}
              <button
                type="button"
                onClick={() => navigate('/dashboard/recruiter/profile')}
                className="font-label-md text-label-md text-primary underline underline-offset-4 hover:opacity-80 transition-opacity"
              >
                Edit profile
              </button>
            </span>
          }
        />
      )}
    </DashboardShell>
  )
}
