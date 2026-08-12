import { Link, useLocation } from 'react-router-dom'
import { cn } from '../../lib/cn.ts'
import { initialsOf } from '../../lib/initials.ts'
import { ROLE_NAV, activeNavTo } from './roleNav.ts'
import { useSidebar } from '../../context'
import type { RequiredRole } from '../../hooks'

interface DashboardSidebarProps {
  role: RequiredRole
  userName: string
  userCompany?: string
  onSignOut: () => void
}

/**
 * Dashboard sidebar. On md+ screens it's a collapsible column whose state is
 * shared with the header (open → header hides its nav links, and the header's
 * toggle is the only open/close control); on mobile it's a slide-in drawer
 * opened from the shell's menu button and closed via the backdrop or the
 * header's hamburger. The two states are independent so resizing across the
 * md breakpoint never surprises the user.
 */
export default function DashboardSidebar({
  role,
  userName,
  userCompany,
  onSignOut,
}: DashboardSidebarProps) {
  const { sidebarOpen, drawerOpen, setDrawerOpen } = useSidebar()

  return (
    <>
      {/* Desktop sidebar column — only while the sidebar is open */}
      <aside
        className={cn(
          'w-64 flex-shrink-0 flex-col border-r border-surface-variant bg-surface-container-lowest',
          sidebarOpen ? 'hidden md:flex' : 'hidden',
        )}
      >
        <SidebarBody
          role={role}
          userName={userName}
          userCompany={userCompany}
          onSignOut={onSignOut}
        />
      </aside>

      {/* Mobile drawer */}
      {drawerOpen && (
        <>
          <div
            className="fixed inset-0 z-40 bg-inverse-surface/50 md:hidden"
            onClick={() => setDrawerOpen(false)}
            aria-hidden="true"
          />
          <aside className="fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-surface-variant bg-surface-container-lowest md:hidden">
            <SidebarBody
              role={role}
              userName={userName}
              userCompany={userCompany}
              onSignOut={onSignOut}
            />
          </aside>
        </>
      )}
    </>
  )
}

function SidebarBody({
  role,
  userName,
  userCompany,
  onSignOut,
}: {
  role: RequiredRole
  userName: string
  userCompany?: string
  onSignOut: () => void
}) {
  const location = useLocation()
  // Longest prefix match wins — nested pages highlight their own item, not
  // the role home. E.g. on the profile page only Profile is lit.
  const activeTo = activeNavTo(ROLE_NAV[role], location.pathname)

  return (
    <>
      <nav className="flex-1 space-y-1.5 px-4 py-6">
        {ROLE_NAV[role].map((item) => {
          const classes = (isActive: boolean) =>
            cn(
              'flex items-center gap-3 rounded-lg px-4 py-3 font-label-md text-label-md transition-colors',
              isActive
                ? 'bg-surface-container-low font-semibold text-primary'
                : 'text-on-surface-variant hover:bg-surface-container-low hover:text-primary',
            )
          const isActive = item.to === activeTo
          return item.to ? (
            <Link
              key={item.label}
              to={item.to}
              aria-current={isActive ? 'page' : undefined}
              className={classes(isActive)}
            >
              <span
                className={cn('material-symbols-outlined', isActive && 'fill')}
              >
                {item.icon}
              </span>
              {item.label}
            </Link>
          ) : (
            <a
              key={item.label}
              href="#"
              onClick={(e) => e.preventDefault()}
              className={classes(false)}
            >
              <span className="material-symbols-outlined">{item.icon}</span>
              {item.label}
            </a>
          )
        })}
      </nav>

      <div className="border-t border-surface-variant p-4">
        <div className="flex items-center gap-3 rounded-lg px-2 py-2">
          <span className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full bg-primary font-label-md text-label-md font-bold text-on-primary">
            {initialsOf(userName)}
          </span>
          <div className="min-w-0">
            <p className="truncate font-label-sm text-label-sm font-medium text-on-surface">
              {userName}
            </p>
            {userCompany && (
              <p className="truncate font-label-sm text-label-sm text-on-surface-variant">
                {userCompany}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={onSignOut}
            title="Sign out"
            aria-label="Sign out"
            className="ml-auto flex h-9 w-9 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-error-container/50 hover:text-error"
          >
            <span className="material-symbols-outlined">logout</span>
          </button>
        </div>
      </div>
    </>
  )
}
