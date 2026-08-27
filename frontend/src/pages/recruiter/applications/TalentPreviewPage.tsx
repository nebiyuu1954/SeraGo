import { useParams, useNavigate } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { fetchApplication, getApiErrorMessage, getStoredAuthTokens } from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { ApplicationResponse } from '../../../types'
import type { ProfileSnapshot } from '../../../types/profileSnapshot.ts'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import TalentProfileView from '../../../components/talent-profile/TalentProfileView.tsx'

// ────────────────────── helpers ──────────────────────

function parseSnapshot(raw: string | null): ProfileSnapshot | null {
  if (!raw) return null
  try {
    return JSON.parse(raw) as ProfileSnapshot
  } catch {
    return null
  }
}

// ────────────────────── main page ──────────────────────

export default function TalentPreviewPage() {
  const { applicationId } = useParams<{ applicationId: string }>()
  const navigate = useNavigate()
  const auth = useRequireRole('Recruiter')

  const [application, setApplication] = useState<ApplicationResponse | null>(
    null,
  )
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (auth.status !== 'authenticated' || !applicationId) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    let cancelled = false
    setLoading(true)
    setError(null)

    fetchApplication(applicationId, tokens.accessToken)
      .then((app) => {
        if (!cancelled) setApplication(app)
      })
      .catch((err) => {
        if (!cancelled) setError(getApiErrorMessage(err))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [auth.status, applicationId])

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

  const snap = application ? parseSnapshot(application.profileSnapshot) : null

  return (
    <DashboardShell role="Recruiter" authUser={auth.user}>
      {/* Loading */}
      {loading && (
        <div className="flex min-h-[50svh] items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      )}

      {/* Error */}
      {error && (
        <div
          role="alert"
          className="rounded-xl border border-error/30 bg-error-container px-4 py-8 text-center"
        >
          <p className="font-body-md text-body-md text-on-error-container">
            {error}
          </p>
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="mt-3 rounded-lg border border-on-error-container/30 px-4 py-2 font-label-md text-label-md text-on-error-container transition-colors hover:bg-on-error-container/10"
          >
            Go back
          </button>
        </div>
      )}

      {/* Content */}
      {!loading && !error && (
        snap ? (
          <TalentProfileView
            snapshot={snap}
            application={application}
            backLabel="Back to applications"
            backTo="/dashboard/recruiter/applications"
          />
        ) : application ? (
          <div className="mt-6">
            <button
              type="button"
              onClick={() => navigate(-1)}
              className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
            >
              <span className="material-symbols-outlined text-lg">arrow_back</span>
              Back to applications
            </button>
            <div className="mt-6 rounded-xl border border-surface-variant bg-surface-container-lowest p-8 text-center">
              <span className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-low text-on-surface-variant mx-auto">
                <span className="material-symbols-outlined text-3xl">person_off</span>
              </span>
              <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
                {application.applicantName}
              </h2>
              <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                {application.applicantEmail}
              </p>
              <p className="mt-3 font-label-md text-label-md text-on-surface-variant">
                This applicant did not share their profile details.
              </p>
            </div>
          </div>
        ) : null
      )}
    </DashboardShell>
  )
}
