import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../ui/Button.tsx'
import { getStoredAuthTokens } from '../../api'
import { useAuthUser, useSignOut, useNotifications } from '../../hooks'
import type { RequiredRole } from '../../hooks'
import { useSidebar } from '../../context'
import { pickRole, roleHome } from '../dashboard/roleNav.ts'
import { initialsOf } from '../../lib/initials.ts'
import { NotificationBell } from '../notifications'

const publicNavLinks = ['Platform', 'Solutions', 'Developers', 'Pricing']

/**
 * App-wide header. Signed-out visitors get the marketing navbar; signed-in
 * users get a slim role-specific header (logo + avatar menu with sign out) —
 * all navigation lives in the dashboard sidebar, so the header stays clean.
 * The role comes from whoami, so the right chrome follows the user
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
  const { drawerOpen, setDrawerOpen } = useSidebar()
  const { unreadCount, isConnected, markAsRead, markAllAsRead } = useNotifications()

  return (
    <header className="sticky top-0 z-50 w-full border-b border-surface-variant bg-surface-container-lowest">
      {/* Slim bar — logo flush left, avatar (desktop) + menu toggle flush right.
          No nav links: the dashboard sidebar owns all navigation. */}
      <div className="flex h-20 w-full items-center justify-between gap-4 px-margin-mobile md:px-margin-desktop">
        <Link
          to={roleHome(role)}
          className="shrink-0 font-headline-md text-headline-md font-bold tracking-tight text-primary"
          aria-label={`SeraGo — ${role} dashboard`}
        >
          SeraGo
        </Link>

        {/* Right group — bell + avatar (desktop) + mobile menu toggle */}
        <div className="flex shrink-0 items-center gap-3">
          {/* Notification bell (desktop) */}
          <div className="hidden md:block">
            <NotificationBell
              unreadCount={unreadCount}
              isConnected={isConnected}
              onMarkAsRead={markAsRead}
              onMarkAllAsRead={markAllAsRead}
            />
          </div>

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

      {/* Mobile drawer — account info + sign out only (no nav links) */}
      {open && !drawerOpen && (
        <div className="border-t border-surface-variant px-margin-mobile pb-4 pt-4 md:hidden">
          <div className="flex items-center gap-3">
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
            <div className="ml-auto flex items-center gap-1">
              <NotificationBell
                unreadCount={unreadCount}
                isConnected={isConnected}
                onMarkAsRead={markAsRead}
                onMarkAllAsRead={markAllAsRead}
              />
              <button
                type="button"
                onClick={onSignOut}
                className="flex items-center gap-1.5 rounded-lg px-3 py-2 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-error-container/50 hover:text-error"
              >
                <span className="material-symbols-outlined text-lg">logout</span>
                Sign out
              </button>
            </div>
          </div>
        </div>
      )}
    </header>
  )
}
