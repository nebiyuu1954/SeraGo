import type { RequiredRole } from '../../hooks'

export interface RoleNavItem {
  label: string
  icon: string
  /**
   * Real route when the page exists. Items without one render as placeholder
   * anchors (#) until their page lands.
   */
  to?: string
  /**
   * Shown only in the sidebar, never in the header — e.g. Settings.
   */
  sidebarOnly?: boolean
}

/** Signed-in header + sidebar navigation per role. */
export const ROLE_NAV: Record<RequiredRole, RoleNavItem[]> = {
  Recruiter: [
    { label: 'Jobs', icon: 'work', to: '/dashboard/recruiter' },
    { label: 'Applications', icon: 'description', to: '/dashboard/recruiter/applications' },
    { label: 'Profile', icon: 'person', to: '/dashboard/recruiter/profile' },
    { label: 'Settings', icon: 'settings', to: '/dashboard/recruiter/settings', sidebarOnly: true },
  ],
  Talent: [
    { label: 'Find jobs', icon: 'search', to: '/dashboard/talent' },
    { label: 'Saved jobs', icon: 'bookmark', to: '/dashboard/talent/saved' },
    { label: 'Applications', icon: 'description', to: '/dashboard/talent/applications' },
    { label: 'Profile', icon: 'person', to: '/dashboard/talent/profile' },
    { label: 'Settings', icon: 'settings', to: '/dashboard/talent/settings', sidebarOnly: true },
  ],
  Admin: [
    { label: 'Overview', icon: 'dashboard', to: '/dashboard/admin' },
    { label: 'Jobs', icon: 'work', to: '/dashboard/admin/jobs' },
    { label: 'Users', icon: 'group', to: '/dashboard/admin/users' },
    { label: 'Sectors', icon: 'category', to: '/dashboard/admin/sectors' },
    { label: 'Profile', icon: 'person', to: '/dashboard/admin/profile' },
    { label: 'Settings', icon: 'settings', to: '/dashboard/admin/settings', sidebarOnly: true },
  ],
}

/** The role's dashboard home (its first item with a real route). */
export function roleHome(role: RequiredRole): string {
  return ROLE_NAV[role].find((item) => item.to)?.to ?? '/dashboard'
}

/**
 * Which nav item's route the current path resolves to. Uses the LONGEST
 * prefix match, so a nested page like /dashboard/recruiter/profile highlights
 * Profile — never its parent (/dashboard/recruiter) — while a job-form page
 * under /dashboard/recruiter/jobs still highlights Jobs.
 */
export function activeNavTo(
  nav: RoleNavItem[],
  pathname: string,
): string | undefined {
  return nav
    .filter(
      (item) =>
        item.to !== undefined &&
        (pathname === item.to || pathname.startsWith(`${item.to}/`)),
    )
    .sort((a, b) => (b.to?.length ?? 0) - (a.to?.length ?? 0))[0]?.to
}

/** Maps whoami role names to a dashboard role, or null for unknown roles. */
export function pickRole(roles: string[]): RequiredRole | null {
  if (roles.includes('Admin')) return 'Admin'
  if (roles.includes('Recruiter')) return 'Recruiter'
  if (roles.includes('Talent')) return 'Talent'
  return null
}
