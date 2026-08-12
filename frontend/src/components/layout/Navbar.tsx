import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Button } from '../ui/Button.tsx'
import { getStoredAuthTokens } from '../../api'
import { useAuthUser, useSignOut } from '../../hooks'
import type { RequiredRole } from '../../hooks'
import { useSidebar } from '../../context'
import {
  ROLE_NAV,
  activeNavTo,
  pickRole,
  roleHome,
} from '../dashboard/roleNav.ts'
import { initialsOf } from '../../lib/initials.ts'
import { cn } from '../../lib/cn.ts'

const publicNavLinks = ['Platform', 'Solutions', 'Developers', 'Pricing']

/**
 * App-wide header. Signed-out visitors get the marketing navbar; signed-in
 * users get a role-specific header (their own nav links + an avatar menu with
 * sign out). The role comes from whoami, so the right chrome follows the user
 * everywhere — including on public pages.
 */
export default function Navbar() {
  const auth = useAuthUser()
  const signOut = useSignOut()

  // No stored tokens → public header, no async work.
  if (!getStoredAuthTokens()) return <PublicNavbar />

  // Tokens exist but the identity is still resolving — render a minimal
  // header so the page doesn't jump; if whoami fails we fall back to public.
  if (auth.status === 'loading') return <LoadingNavbar />
  if (auth.status !== 'authenticated') return <PublicNavbar />

  const role = pickRole(auth.user.roles)
  if (!role) return <PublicNavbar />

  return (
    <RoleNavbar
      role={role}
      name={auth.user.username ?? auth.user.email ?? 'Account'}
      email={auth.user.email}
      onSignOut={signOut}
    />
  )
}

/* ------------------------------------------------------------ Public */

function PublicNavbar() {
  const [open, setOpen] = useState(false)
  // Checked at render time. Signing in/out happens on standalone auth pages
  // or via the dashboard sign-out, both of which unmount the Layout (and
  // thus this header) before navigating.
  const signedIn = getStoredAuthTokens() !== null

  return (
    <header className="sticky top-0 z-50 w-full bg-surface-container-lowest">
      <div className="mx-auto flex h-20 max-w-container-max items-center justify-between px-margin-mobile md:px-margin-desktop">
        <Link
          to="/"
          className="font-headline-md text-headline-md font-bold tracking-tight text-primary"
          aria-label="SeraGo home"
        >
          SeraGo
        </Link>

        {/* Desktop links */}
        <nav className="hidden gap-8 md:flex">
          {publicNavLinks.map((link) => (
            <a
              key={link}
              href="#"
              className="font-body-md text-body-md text-secondary transition-colors duration-200 hover:text-primary"
            >
              {link}
            </a>
          ))}
        </nav>

        <div className="hidden items-center gap-3 md:flex">
          {signedIn ? (
            <Button to="/dashboard" variant="primary">
              Dashboard
            </Button>
          ) : (
            <>
              <Button to="/login" variant="secondary">
                Log in
              </Button>
              <Button to="/signup" variant="primary">
                Sign up
              </Button>
            </>
          )}
        </div>

        {/* Mobile toggle */}
        <button
          type="button"
          className="text-primary md:hidden"
          onClick={() => setOpen((o) => !o)}
          aria-expanded={open}
          aria-label="Toggle navigation menu"
        >
          <span className="material-symbols-outlined">
            {open ? 'close' : 'menu'}
          </span>
        </button>
      </div>

      {/* Mobile menu */}
      {open && (
        <div className="border-t border-outline-variant px-margin-mobile pb-4 pt-2 md:hidden">
          {publicNavLinks.map((link) => (
            <a
              key={link}
              href="#"
              onClick={() => setOpen(false)}
              className="block rounded-lg px-3 py-2.5 font-body-md text-body-md text-secondary transition-colors hover:text-primary"
            >
              {link}
            </a>
          ))}
          {signedIn ? (
            <Button
              to="/dashboard"
              variant="primary"
              className="mt-2 w-full"
              onClick={() => setOpen(false)}
            >
              Dashboard
            </Button>
          ) : (
            <>
              <Button
                to="/login"
                variant="secondary"
                className="mt-2 w-full"
                onClick={() => setOpen(false)}
              >
                Log in
              </Button>
              <Button
                to="/signup"
                variant="primary"
                className="mt-2 w-full"
                onClick={() => setOpen(false)}
              >
                Sign up
              </Button>
            </>
          )}
        </div>
      )}
    </header>
  )
}

/* ------------------------------------------------------- Signed in */

/** Slim header shown while whoami resolves (avoids layout jumping). */
function LoadingNavbar() {
  return (
    <header className="sticky top-0 z-50 w-full border-b border-surface-variant bg-surface-container-lowest">
      <div className="mx-auto flex h-20 max-w-container-max items-center justify-between px-margin-mobile md:px-margin-desktop">
        <Link
          to="/"
          className="font-headline-md text-headline-md font-bold tracking-tight text-primary"
        >
          SeraGo
        </Link>
        <span
          aria-hidden="true"
          className="h-5 w-5 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
        />
      </div>
    </header>
  )
}

function RoleNavbar({
  role,
  name,
  email,
  onSignOut,
}: {
  role: RequiredRole
  name: string
  email: string | null
  onSignOut: () => void
}) {
  const [open, setOpen] = useState(false)
  const [avatarOpen, setAvatarOpen] = useState(false)
  const { sidebarOpen, toggleSidebar, drawerOpen, setDrawerOpen } = useSidebar()
  const location = useLocation()
  const nav = ROLE_NAV[role]
  // Settings lives only in the sidebar — never in the header.
  const headerNav = nav.filter((item) => !item.sidebarOnly)
  // The header carries the nav only when the sidebar isn't doing the job
  // (on a public page, or on a dashboard whose sidebar is collapsed).
  const onDashboard = location.pathname.startsWith('/dashboard')
  const showHeaderNav = !onDashboard || !sidebarOpen
  // Longest prefix match wins — same rule as the sidebar, so header and
  // sidebar always agree on which item is active.
  const activeTo = activeNavTo(headerNav, location.pathname)

  return (
    <header className="sticky top-0 z-50 w-full border-b border-surface-variant bg-surface-container-lowest">
      {/* Full-width bar — left (toggle + logo) flush left, nav centered,
          right (avatar + menu) flush right */}
      <div className="grid h-20 w-full grid-cols-[1fr_auto_1fr] items-center gap-4 px-margin-mobile md:px-margin-desktop">
        {/* Explicit columns: when the centered nav is hidden (sidebar open),
            auto-placement would otherwise pull the right group into the
            middle column — col-start pins each group in place. */}
        <div className="col-start-1 flex min-w-0 items-center gap-2">
          {/* Sidebar toggle — only where a sidebar exists (desktop dashboards) */}
          {onDashboard && (
            <button
              type="button"
              onClick={toggleSidebar}
              className="hidden h-10 w-10 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-on-surface md:flex"
              aria-label={sidebarOpen ? 'Close sidebar' : 'Open sidebar'}
              title={sidebarOpen ? 'Close sidebar' : 'Open sidebar'}
            >
              <span className="material-symbols-outlined">
                {sidebarOpen ? 'menu_open' : 'menu'}
              </span>
            </button>
          )}

          <Link
            to={roleHome(role)}
            className="shrink-0 font-headline-md text-headline-md font-bold tracking-tight text-primary"
            aria-label={`SeraGo — ${role} dashboard`}
          >
            SeraGo
          </Link>
        </div>

        {/* Centered role nav — visible when the sidebar is collapsed */}
        {showHeaderNav && (
          <nav className="col-start-2 hidden items-center gap-1 md:flex">
            {headerNav.map((item) => {
              const isActive = item.to === activeTo
              return item.to ? (
                <Link
                  key={item.label}
                  to={item.to}
                  aria-current={isActive ? 'page' : undefined}
                  className={cn(
                    'rounded-lg px-4 py-2 font-label-md text-label-md font-medium transition-colors',
                    isActive
                      ? 'bg-primary-container text-primary'
                      : 'text-on-surface-variant hover:bg-surface-container-low hover:text-primary',
                  )}
                >
                  {item.label}
                </Link>
              ) : (
                <a
                  key={item.label}
                  href="#"
                  onClick={(e) => e.preventDefault()}
                  className="rounded-lg px-4 py-2 font-label-md text-label-md font-medium text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-primary"
                >
                  {item.label}
                </a>
              )
            })}
          </nav>
        )}

        {/* Right group — avatar (desktop) + mobile menu toggle */}
        <div className="col-start-3 flex shrink-0 items-center justify-end gap-3">
          {/* Avatar menu (desktop) */}
          <div className="relative hidden md:block">
            <button
              type="button"
              onClick={() => setAvatarOpen((o) => !o)}
              className="flex items-center gap-2 rounded-full border border-surface-variant bg-surface-container-low py-1.5 pl-1.5 pr-3 transition-colors hover:bg-surface-container"
              aria-haspopup="menu"
              aria-expanded={avatarOpen}
            >
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-primary font-label-md text-label-md font-bold text-on-primary">
                {initialsOf(name)}
              </span>
              <span className="hidden max-w-36 truncate font-label-md text-label-md font-medium text-on-surface lg:block">
                {name}
              </span>
              <span className="material-symbols-outlined text-base text-on-surface-variant">
                {avatarOpen ? 'expand_less' : 'expand_more'}
              </span>
            </button>

            {avatarOpen && (
              <>
                <div
                  className="fixed inset-0 z-10"
                  onClick={() => setAvatarOpen(false)}
                />
                <div
                  role="menu"
                  className="absolute right-0 z-20 mt-2 w-64 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-lg"
                >
                  <div className="border-b border-surface-variant px-4 py-3">
                    <p className="truncate font-label-md text-label-md font-medium text-on-surface">
                      {name}
                    </p>
                    {email && (
                      <p className="mt-0.5 truncate font-label-sm text-label-sm text-on-surface-variant">
                        {email}
                      </p>
                    )}
                    <span className="mt-2 inline-block rounded-full bg-primary-container px-2.5 py-0.5 font-label-sm text-label-sm font-medium text-primary">
                      {role}
                    </span>
                  </div>
                  <button
                    type="button"
                    role="menuitem"
                    onClick={onSignOut}
                    className="flex w-full items-center gap-2.5 px-4 py-3 font-label-md text-label-md text-on-surface transition-colors hover:bg-error-container/50 hover:text-error"
                  >
                    <span className="material-symbols-outlined text-lg">
                      logout
                    </span>
                    Sign out
                  </button>
                </div>
              </>
            )}
          </div>

          {/* Mobile menu toggle — closes the sidebar drawer when it's open */}
          <button
            type="button"
            className="text-on-surface-variant md:hidden"
            onClick={() => {
              if (drawerOpen) setDrawerOpen(false)
              else setOpen((o) => !o)
            }}
            aria-expanded={open}
            aria-label="Toggle navigation menu"
          >
            <span className="material-symbols-outlined">
              {drawerOpen ? 'close' : open ? 'close' : 'menu'}
            </span>
          </button>
        </div>
      </div>

      {/* Mobile drawer */}
      {open && !drawerOpen && (
        <div className="border-t border-surface-variant px-margin-mobile pb-4 pt-2 md:hidden">
          {headerNav.map((item) => {
            const isActive = item.to === activeTo
            return item.to ? (
              <Link
                key={item.label}
                to={item.to}
                onClick={() => setOpen(false)}
                aria-current={isActive ? 'page' : undefined}
                className={cn(
                  'block rounded-lg px-3 py-2.5 font-body-md text-body-md transition-colors',
                  isActive
                    ? 'bg-primary-container font-medium text-primary'
                    : 'text-secondary hover:text-primary',
                )}
              >
                {item.label}
              </Link>
            ) : (
              <a
                key={item.label}
                href="#"
                onClick={(e) => e.preventDefault()}
                className="block rounded-lg px-3 py-2.5 font-body-md text-body-md text-secondary transition-colors hover:text-primary"
              >
                {item.label}
              </a>
            )
          })}
          <div className="mt-3 flex items-center gap-3 border-t border-surface-variant pt-4">
            <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary font-label-md text-label-md font-bold text-on-primary">
              {initialsOf(name)}
            </span>
            <div className="min-w-0">
              <p className="truncate font-label-md text-label-md font-medium text-on-surface">
                {name}
              </p>
              {email && (
                <p className="truncate font-label-sm text-label-sm text-on-surface-variant">
                  {email}
                </p>
              )}
            </div>
            <button
              type="button"
              onClick={onSignOut}
              className="ml-auto flex items-center gap-1.5 rounded-lg px-3 py-2 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-error-container/50 hover:text-error"
            >
              <span className="material-symbols-outlined text-lg">logout</span>
              Sign out
            </button>
          </div>
        </div>
      )}
    </header>
  )
}
