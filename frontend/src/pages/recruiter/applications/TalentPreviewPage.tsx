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
      {!loading && !error && snap && (
        <TalentProfileView
          snapshot={snap}
          application={application}
          backLabel="Back to applications"
          backTo="/dashboard/recruiter/applications"
        />
      )}
    </DashboardShell>
  )
}
