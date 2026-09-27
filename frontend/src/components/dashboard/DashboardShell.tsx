import { useEffect, useState, type ReactNode } from 'react'
import { fetchProfile, fetchAdminStatsOverview, getStoredAuthTokens } from '../../api'
import { useSignOut } from '../../hooks'
import type { RequiredRole } from '../../hooks'
import type { ProfileResponse, WhoAmIResponse } from '../../types'
import { useSidebar } from '../../context'
import DashboardSidebar from './DashboardSidebar.tsx'

interface DashboardShellProps {
  role: RequiredRole
  authUser: WhoAmIResponse
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
  const { setDrawerOpen } = useSidebar()
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

  const name =
    (profile && [profile.firstName, profile.lastName].filter(Boolean).join(' ')) ||
    authUser.username ||
    authUser.email ||
    'User'

  return (
    <div className="ui-scale-fill-below-nav flex items-stretch">
      <DashboardSidebar
        role={role}
        userName={name}
        userAvatarUrl={profile?.avatarUrl}
        onSignOut={signOut}
        badges={adminBadges}
      />

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Header — visible on all screen sizes */}
        <div className="flex h-16 shrink-0 items-center border-b border-surface-variant px-margin-mobile md:hidden">
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setDrawerOpen(true)}
              aria-label="Open menu"
              className="text-on-surface-variant transition-colors hover:text-on-surface"
            >
              <span className="material-symbols-outlined">menu</span>
            </button>
            <span className="font-headline-md text-headline-md font-bold tracking-tight text-primary">
              SeraGo
            </span>
          </div>
        </div>

        <main className="flex-1 px-margin-mobile py-6 md:px-8 lg:px-10">
          {children}
        </main>
      </div>
    </div>
  )
}
