import PasswordSetupCard from '../../../components/dashboard/PasswordSetupCard.tsx'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { useRequireRole } from '../../../hooks'
import type { RequiredRole } from '../../../hooks'
import { cn } from '../../../lib/cn.ts'

const ROLE_META: Record<RequiredRole, { icon: string; badgeClass: string }> = {
  Talent: { icon: 'work', badgeClass: 'bg-primary-container/60 text-primary' },
  Admin: {
    icon: 'admin_panel_settings',
    badgeClass: 'bg-error-container text-error',
  },
}

interface RoleDashboardProps {
  role: RequiredRole
  blurb: string
  features: string[]
}

/**
 * Shared "you're signed in" screen used by the talent and admin dashboards.
 * Resolves the current user (via the stored token + /api/auth/whoami),
 * redirects signed-out/wrong-role visitors, and renders the greeting inside
 * the standard sidebar shell.
 */
export default function RoleDashboard({
  role,
  blurb,
  features,
}: RoleDashboardProps) {
  const auth = useRequireRole(role)
  const meta = ROLE_META[role]

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

  const displayName = auth.user.username ?? auth.user.email ?? 'there'

  return (
    <DashboardShell role={role} authUser={auth.user}>
      <div className="rounded-2xl border border-outline-variant bg-surface-container-lowest p-8 shadow-sm sm:p-10">
        {/* Role badge */}
        <span
          className={cn(
            'inline-flex items-center gap-1.5 rounded-full px-3 py-1 font-label-sm text-label-sm font-medium',
            meta.badgeClass,
          )}
        >
          <span
            aria-hidden="true"
            className="material-symbols-outlined text-base"
          >
            {meta.icon}
          </span>
          {role}
        </span>

        {/* The greeting — the point of these pages */}
        <h1 className="mt-5 font-display-lg-mobile text-display-lg-mobile font-bold tracking-tight text-on-surface xl:text-[40px] xl:leading-[48px]">
          Logged in as <span className="text-primary">{displayName}</span>
        </h1>

        <p className="mt-3 max-w-xl font-body-md text-body-md text-on-surface-variant">
          {blurb}
        </p>

        <ul className="mt-6 space-y-2.5">
          {features.map((feature) => (
            <li
              key={feature}
              className="flex items-center gap-2.5 font-body-md text-body-md text-on-surface"
            >
              <span
                aria-hidden="true"
                className="material-symbols-outlined text-lg text-success"
              >
                check_circle
              </span>
              {feature}
            </li>
          ))}
        </ul>

        <PasswordSetupCard />

        <p className="mt-8 border-t border-outline-variant pt-6 font-label-sm text-label-sm text-on-surface-variant">
          Your role-based workspace is under construction — more coming soon.
        </p>
      </div>
    </DashboardShell>
  )
}
