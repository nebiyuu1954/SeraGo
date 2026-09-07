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
  userAvatarUrl?: string
  onSignOut: () => void
  /** Optional badge overrides keyed by nav item label (e.g. { Jobs: 5 }). */
  badges?: Record<string, number>
}

/**
 * Dashboard sidebar. On md+ screens it's a collapsible column: expanded it
 * shows labels, collapsed it shrinks to a narrow icon-only rail (tooltips
 * keep the items discoverable). The collapse/expand toggle lives at the
 * bottom of the nav, right under Settings. On mobile it's a slide-in drawer
 * opened from the shell's menu button and closed via the backdrop or the
 * header's hamburger. The two states are independent so resizing across the
 * md breakpoint never surprises the user.
 */
export default function DashboardSidebar({
  role,
  userName,
  userCompany,
  userAvatarUrl,
  onSignOut,
  badges,
}: DashboardSidebarProps) {
  const { sidebarOpen, toggleSidebar, drawerOpen, setDrawerOpen } = useSidebar()

  return (
    <>
      {/* Desktop sidebar column — always visible on md+, collapses to an icon rail */}
      <aside
        className={cn(
          'hidden flex-shrink-0 flex-col overflow-hidden border-r border-surface-variant bg-surface-container-lowest transition-[width] duration-300 ease-in-out md:flex',
          sidebarOpen ? 'w-60' : 'w-20',
        )}
      >
        <SidebarBody
          role={role}
          userName={userName}
          userCompany={userCompany}
          userAvatarUrl={userAvatarUrl}
          onSignOut={onSignOut}
          collapsed={!sidebarOpen}
          onToggle={toggleSidebar}
          badges={badges}
        />
      </aside>

      {/* Mobile drawer — always shows the full labels */}
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
            userAvatarUrl={userAvatarUrl}
            onSignOut={onSignOut}
            collapsed={false}
            badges={badges}
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
  userAvatarUrl,
  onSignOut,
  collapsed,
  onToggle,
  badges,
}: {
  role: RequiredRole
  userName: string
  userCompany?: string
  userAvatarUrl?: string
  onSignOut: () => void
  collapsed: boolean
  onToggle?: () => void
  badges?: Record<string, number>
}) {
  const location = useLocation()
  // Longest prefix match wins — nested pages highlight their own item, not
  // the role home. E.g. on the profile page only Profile is lit.
  const activeTo = activeNavTo(ROLE_NAV[role], location.pathname)

  const itemClasses = (isActive: boolean) =>
    cn(
      'flex items-center gap-3 rounded-lg px-4 py-3 font-label-md text-label-md transition-colors',
      collapsed && 'justify-center px-0',
      isActive
        ? 'bg-surface-container-low font-semibold text-primary'
        : 'text-on-surface-variant hover:bg-surface-container-low hover:text-primary',
    )

  return (
    <>
      <nav className="flex-1 space-y-1.5 px-4 py-6">
        {ROLE_NAV[role].map((item) => {
          const isActive = item.to === activeTo
          const badgeCount = badges?.[item.label] ?? item.badge
          const content = (
            <>
              <span className="relative">
                <span
                  className={cn(
                    'material-symbols-outlined',
                    isActive && 'fill',
                  )}
                >
                  {item.icon}
                </span>
                {badgeCount !== undefined && badgeCount > 0 && !collapsed && (
                  <span className="absolute -right-2 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-error px-1 font-label-xs text-[10px] font-bold text-on-error">
                    {badgeCount}
                  </span>
                )}
              </span>
              {!collapsed && item.label}
              {!collapsed && badgeCount !== undefined && badgeCount > 0 && (
                <span className="ml-auto flex h-5 min-w-5 items-center justify-center rounded-full bg-error px-1 font-label-xs text-[10px] font-bold text-on-error">
                  {badgeCount}
                </span>
              )}
            </>
          )
          return item.to ? (
            <Link
              key={item.label}
              to={item.to}
              aria-current={isActive ? 'page' : undefined}
              title={collapsed ? item.label : undefined}
              className={itemClasses(isActive)}
            >
              {content}
            </Link>
          ) : (
            <a
              key={item.label}
              href="#"
              onClick={(e) => e.preventDefault()}
              title={collapsed ? item.label : undefined}
              className={itemClasses(false)}
            >
              {content}
            </a>
          )
        })}

        {/* Collapse/expand toggle — sits under Settings (desktop only) */}
        {onToggle && (
          <button
            type="button"
            onClick={onToggle}
            title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            className={cn(
              'flex w-full items-center gap-3 rounded-lg px-4 py-3 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-primary',
              collapsed && 'justify-center px-0',
            )}
          >
            {/* menu_open points left when expanded (collapse); rotated 180° it
                points right when collapsed, signaling "click to expand". */}
            <span
              className={cn(
                'material-symbols-outlined transition-transform duration-300',
                collapsed && 'rotate-180',
              )}
            >
              menu_open
            </span>
            {!collapsed && 'Collapse'}
          </button>
        )}
      </nav>

      <div className="border-t border-surface-variant p-4">
        {collapsed ? (
          <div className="flex flex-col items-center gap-3">
            {userAvatarUrl ? (
              <img src={userAvatarUrl} alt={userName} className="h-9 w-9 rounded-full object-cover" />
            ) : (
              <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary font-label-md text-label-md font-bold text-on-primary">
                {initialsOf(userName)}
              </span>
            )}
            <button
              type="button"
              onClick={onSignOut}
              title="Sign out"
              aria-label="Sign out"
              className="flex h-9 w-9 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-error-container/50 hover:text-error"
            >
              <span className="material-symbols-outlined">logout</span>
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-3 rounded-lg px-2 py-2">
            {userAvatarUrl ? (
              <img src={userAvatarUrl} alt={userName} className="h-9 w-9 rounded-full object-cover" />
            ) : (
              <span className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full bg-primary font-label-md text-label-md font-bold text-on-primary">
                {initialsOf(userName)}
              </span>
            )}
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
        )}
      </div>
    </>
  )
}
