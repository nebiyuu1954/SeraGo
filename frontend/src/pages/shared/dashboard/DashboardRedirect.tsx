import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthUser } from '../../../hooks'

/**
 * Entry point after sign-in. Resolves the user's role via /api/auth/whoami
 * and redirects to the matching dashboard; signed-out visitors go to /login.
 */
export default function DashboardRedirect() {
  const navigate = useNavigate()
  const auth = useAuthUser()

  useEffect(() => {
    if (auth.status === 'loading') return
    if (auth.status === 'unauthenticated') {
      navigate('/login', { replace: true })
      return
    }
    // Account inactive until the email is confirmed — nothing else is usable.
    if (auth.status === 'authenticated' && !auth.user.emailConfirmed) {
      navigate('/confirm-email?required=1', { replace: true })
      return
    }
    const target = auth.user.roles.includes('Admin')
      ? 'admin'
      : auth.user.roles.includes('Talent')
        ? 'talent'
        : null
    // No recognized role (e.g. revoked) — don't default to a page whose
    // guard would bounce us back here forever. Send them to login instead.
    if (!target) {
      navigate('/login', { replace: true })
      return
    }

    const pendingJobId =
      sessionStorage.getItem('serago_pending_target_job_id') ||
      localStorage.getItem('serago_pending_target_job_id')

    if (target === 'talent' && pendingJobId) {
      try {
        sessionStorage.removeItem('serago_pending_target_job_id')
        localStorage.removeItem('serago_pending_target_job_id')
      } catch {}
      navigate(`/dashboard/talent/jobs/${pendingJobId}`, { replace: true })
      return
    }

    navigate(`/dashboard/${target}`, { replace: true })
  }, [auth, navigate])

  return (
    <div className="flex min-h-[50svh] items-center justify-center">
      <span
        aria-hidden="true"
        className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
      />
    </div>
  )
}
