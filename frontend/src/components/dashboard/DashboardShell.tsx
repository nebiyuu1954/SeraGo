import { useEffect, useState, type ReactNode } from 'react'
import { fetchProfile, fetchAdminStatsOverview, getStoredAuthTokens } from '../../api'
import { useSignOut } from '../../hooks'
import type { RequiredRole } from '../../hooks'
import type { ProfileResponse, WhoAmIResponse } from '../../types'
import DashboardSidebar from './DashboardSidebar.tsx'

interface DashboardShellProps {
  role: RequiredRole
  authUser?: WhoAmIResponse | null
  children: ReactNode
}

/**
 * Signed-in dashboard frame: the role's sidebar (desktop column / mobile
 * drawer) plus the page content. The user chip resolves the display name from
 * the profile when available, falling back to the whoami identity.
 */
export default function DashboardShell({
  role,
  authUser,
  children,
}: DashboardShellProps) {
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [adminBadges, setAdminBadges] = useState<Record<string, number>>({})
  const signOut = useSignOut()


  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    fetchProfile(tokens.accessToken)
      .then((p) => {
        if (!cancelled) setProfile(p)
      })
      .catch(() => {
        // Profile unreadable — the whoami identity is used instead.
      })
    // Admin: fetch pending-approvals count for the sidebar badge.
    if (role === 'Admin') {
      fetchAdminStatsOverview(tokens.accessToken)
        .then((ov) => {
          if (!cancelled && ov.jobs.pendingApproval > 0) {
            setAdminBadges({ Jobs: ov.jobs.pendingApproval })
          }
        })
        .catch(() => { /* non-critical */ })
    }
    return () => {
      cancelled = true
    }
  }, [role])

  const isGuest = !authUser
  const name =
    (profile && [profile.firstName, profile.lastName].filter(Boolean).join(' ')) ||
    authUser?.username ||
    authUser?.email ||
    'Guest'

  return (
    <div className="ui-scale-fill-below-nav flex items-stretch">
      <DashboardSidebar
        role={role}
        userName={name}
        userAvatarUrl={profile?.avatarUrl}
        onSignOut={signOut}
        badges={adminBadges}
        isGuest={isGuest}
      />

      <div className="flex min-w-0 flex-1 flex-col">
        <main className="flex-1 px-4 sm:px-6 md:px-8 lg:px-10 py-6">
          {children}
        </main>
      </div>
    </div>
  )
}
