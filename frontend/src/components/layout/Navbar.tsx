import { useState, useEffect, useRef } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Button } from '../ui/Button.tsx'
import { getStoredAuthTokens } from '../../api'
import {
  useAuthUser,
  useSignOut,
  useNotifications,
  useProfileQuery,
} from '../../hooks'
import type { RequiredRole } from '../../hooks'
import { useSidebar } from '../../context'
import { pickRole, roleHome } from '../dashboard/roleNav.ts'
import { initialsOf } from '../../lib/initials.ts'
import { NotificationBell } from '../notifications'

const publicNavLinks = [{ label: 'Jobs', to: '/jobs' }]

/**
 * ★ DESKTOP HEADER WIDTH KNOB ★
 * Change this value (number in px or CSS string) to customize the desktop header width.
 * Examples: 1024 (default), 960, 1140, 1200, or '100%' / '1024px'.
 */
export const DESKTOP_HEADER_MAX_WIDTH = 700

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
  const { profile } = useProfileQuery(auth.status === 'authenticated')

  // No stored tokens → public header, no async work.
  if (!getStoredAuthTokens()) return <PublicNavbar />

  // Tokens exist but the identity is still resolving — render a minimal
  // header so the page doesn't jump; if whoami fails we fall back to public.
  if (auth.status === 'loading') return <LoadingNavbar />
  if (auth.status !== 'authenticated') return <PublicNavbar />

  const role = pickRole(auth.user.roles)
  if (!role) return <PublicNavbar />

  const displayName =
    (profile && [profile.firstName, profile.lastName].filter(Boolean).join(' ')) ||
    auth.user.username ||
    auth.user.email ||
    'Account'

  return (
    <RoleNavbar
      role={role}
      name={displayName}
      email={auth.user.email}
      avatarUrl={profile?.avatarUrl}
      onSignOut={signOut}
    />
  )
}

/* ------------------------------------------------------------ Public */

function PublicNavbar() {
  const [isVisible, setIsVisible] = useState(true)
  const lastScrollY = useRef(0)

  // Checked at render time. Signing in/out happens on standalone auth pages
  // or via the dashboard sign-out, both of which unmount the Layout (and
  // thus this header) before navigating.
  const signedIn = getStoredAuthTokens() !== null

  useEffect(() => {
    let ticking = false

    const handleScroll = () => {
      if (!ticking) {
        window.requestAnimationFrame(() => {
          const currentScrollY = window.scrollY
          const prevScrollY = lastScrollY.current

          // Always visible near top of page
          if (currentScrollY <= 20) {
            setIsVisible(true)
          } else if (Math.abs(currentScrollY - prevScrollY) > 8) {
            // Scroll down -> hide, scroll up -> show
            if (currentScrollY > prevScrollY && currentScrollY > 70) {
              setIsVisible(false)
            } else if (currentScrollY < prevScrollY) {
              setIsVisible(true)
            }
          }

          lastScrollY.current = currentScrollY
          ticking = false
        })
        ticking = true
      }
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const maxWidthStyle =
    typeof DESKTOP_HEADER_MAX_WIDTH === 'number'
      ? `${DESKTOP_HEADER_MAX_WIDTH}px`
      : DESKTOP_HEADER_MAX_WIDTH

  return (
    <>
      {/* Spacer to preserve natural flow spacing for all public pages */}
      <div className="h-20 w-full shrink-0 pointer-events-none" aria-hidden="true" />

      {/* Floating Centered Island Header */}
      <header
        className={`fixed top-3 sm:top-5 left-0 right-0 z-50 flex justify-center px-3.5 sm:px-6 pointer-events-none transition-all duration-300 ease-in-out ${
          isVisible ? 'translate-y-0 opacity-100' : '-translate-y-28 opacity-0'
        }`}
      >
        <div
          style={{ maxWidth: maxWidthStyle }}
          className="relative pointer-events-auto flex w-full items-center justify-between rounded-full border border-slate-200/80 bg-white/95 px-4 py-2 shadow-[0_8px_30px_rgb(0,0,0,0.07)] backdrop-blur-md transition-all sm:px-7 sm:py-3 dark:border-slate-800 dark:bg-slate-900/95"
        >
          {/* Brand Logo: Favicon logo on phone and desktop (with SeraGo text on desktop) */}
          <Link
            to="/"
            className="flex items-center gap-2.5 shrink-0 z-10 transition-transform active:scale-95"
            aria-label="SeraGo home"
          >
            {/* Favicon logo */}
            <img
              src="/sources/logo%20favicon/apple-touch-icon.png"
              alt="SeraGo"
              className="h-8 w-8 rounded-full object-contain"
            />
            {/* Desktop: SeraGo text */}
            <span className="hidden font-headline-md text-headline-md font-extrabold tracking-tight md:inline-block">
              <span className="text-[#1A73E8]">Sera</span>
              <span className="text-[#FF6600]">Go</span>
            </span>
          </Link>

          {/* Center Navigation: "Jobs" link centered in both mobile and desktop */}
          <nav className="absolute left-1/2 -translate-x-1/2 flex items-center justify-center">
            {publicNavLinks.map((link) => (
              <Link
                key={link.to}
                to={link.to}
                className="font-body-md text-sm sm:text-base font-semibold sm:font-medium text-slate-700 transition-colors duration-200 hover:text-primary px-3 py-1.5"
              >
                {link.label}
              </Link>
            ))}
          </nav>

          {/* Right Action Area */}
          <div className="flex items-center gap-2 sm:gap-3 z-10">
            {/* Mobile-only Log in button: Orange bg + Black text (Signup omitted) */}
            <div className="md:hidden">
              {signedIn ? (
                <Link
                  to="/dashboard"
                  className="rounded-full bg-[#FF6600] px-4 py-1.5 text-xs font-bold text-black shadow-xs transition-all hover:bg-[#e65c00] active:scale-95"
                >
                  Dashboard
                </Link>
              ) : (
                <Link
                  to="/login"
                  className="rounded-full bg-[#FF6600] px-4 py-1.5 text-xs font-bold text-black shadow-xs transition-all hover:bg-[#e65c00] active:scale-95"
                >
                  Log in
                </Link>
              )}
            </div>

            {/* Desktop-only Auth Buttons */}
            <div className="hidden items-center gap-2.5 md:flex">
              {signedIn ? (
                <Button to="/dashboard" variant="primary" className="rounded-full px-5 py-2">
                  Dashboard
                </Button>
              ) : (
                <>
                  <Button
                    to="/login"
                    variant="ghost"
                    className="rounded-full px-4 py-2 font-medium text-slate-700 hover:text-primary hover:bg-slate-100"
                  >
                    Log in
                  </Button>
                  <Button
                    to="/signup"
                    variant="primary"
                    className="rounded-full px-5 py-2 font-semibold shadow-sm"
                  >
                    Sign up
                  </Button>
                </>
              )}
            </div>
          </div>
        </div>
      </header>
    </>
  )
}

/* ------------------------------------------------------- Signed in */

/** Slim header shown while whoami resolves (avoids layout jumping). */
function LoadingNavbar() {
  const maxWidthStyle =
    typeof DESKTOP_HEADER_MAX_WIDTH === 'number'
      ? `${DESKTOP_HEADER_MAX_WIDTH}px`
      : DESKTOP_HEADER_MAX_WIDTH

  return (
    <>
      <div className="h-20 w-full shrink-0 pointer-events-none" aria-hidden="true" />
      <header className="fixed top-3 sm:top-5 left-0 right-0 z-50 flex justify-center px-3.5 sm:px-6 pointer-events-none">
        <div
          style={{ maxWidth: maxWidthStyle }}
          className="pointer-events-auto flex w-full items-center justify-between rounded-full border border-slate-200/80 bg-white/95 px-4 py-2 shadow-[0_8px_30px_rgb(0,0,0,0.07)] backdrop-blur-md sm:px-7 sm:py-3 dark:border-slate-800 dark:bg-slate-900/95"
        >
          <Link to="/" className="flex items-center gap-2 shrink-0">
            <img
              src="/sources/logo%20favicon/apple-touch-icon.png"
              alt="SeraGo"
              className="h-8 w-8 rounded-full object-contain md:hidden"
            />
            <span className="hidden font-headline-md text-headline-md font-extrabold tracking-tight md:inline-block">
              <span className="text-[#1A73E8]">Sera</span>
              <span className="text-[#FF6600]">Go</span>
            </span>
          </Link>
          <span
            aria-hidden="true"
            className="h-5 w-5 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      </header>
    </>
  )
}

function RoleNavbar({
  role,
  name,
  email,
  avatarUrl,
  onSignOut,
}: {
  role: RequiredRole
  name: string
  email: string | null
  avatarUrl?: string | null
  onSignOut: () => void
}) {
  const [avatarOpen, setAvatarOpen] = useState(false)
  const { drawerOpen, setDrawerOpen } = useSidebar()
  const { unreadCount, isConnected, markAsRead, markAllAsRead } = useNotifications()
  const location = useLocation()
  const isDashboard = location.pathname.startsWith('/dashboard')

  return (
    <header className="sticky top-0 z-50 w-full border-b border-surface-variant bg-surface-container-lowest">
      {/* Slim bar — logo flush left, notification bell + avatar dropdown flush right */}
      <div className="flex h-16 w-full items-center justify-between gap-3 px-4 md:px-8">
        <div className="flex items-center gap-2">
          {isDashboard && (
            <button
              type="button"
              className="flex items-center justify-center rounded-lg p-1.5 text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-on-surface md:hidden"
              onClick={() => setDrawerOpen(!drawerOpen)}
              aria-label="Toggle navigation menu"
            >
              <span className="material-symbols-outlined text-2xl">
                {drawerOpen ? 'close' : 'menu'}
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

        {/* Right group — notification bell + profile avatar with dropdown */}
        <div className="flex shrink-0 items-center gap-2 md:gap-3">
          {/* Notification bell — always visible on both mobile and desktop */}
          <NotificationBell
            unreadCount={unreadCount}
            isConnected={isConnected}
            onMarkAsRead={markAsRead}
            onMarkAllAsRead={markAllAsRead}
          />

          {/* Profile Picture with Dropdown Button */}
          <div className="relative">
            <button
              type="button"
              onClick={() => setAvatarOpen((o) => !o)}
              className="flex items-center gap-1.5 rounded-full border border-surface-variant bg-surface-container-low py-1 pl-1 pr-2 transition-colors hover:bg-surface-container md:py-1.5 md:pl-1.5 md:pr-3"
              aria-haspopup="menu"
              aria-expanded={avatarOpen}
              aria-label="User account menu"
            >
              {avatarUrl ? (
                <img
                  src={avatarUrl}
                  alt={name}
                  className="h-7 w-7 rounded-full object-cover shrink-0 md:h-8 md:w-8"
                />
              ) : (
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary font-label-md text-label-sm font-bold text-on-primary md:h-8 md:w-8 md:text-label-md">
                  {initialsOf(name)}
                </span>
              )}
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
                  className="fixed inset-0 z-40"
                  onClick={() => setAvatarOpen(false)}
                />
                <div
                  role="menu"
                  className="absolute right-0 z-50 mt-2 w-64 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-lg"
                >
                  <div className="border-b border-surface-variant px-4 py-3">
                    <div className="flex items-center gap-2.5 mb-2">
                      {avatarUrl ? (
                        <img
                          src={avatarUrl}
                          alt={name}
                          className="h-9 w-9 rounded-full object-cover shrink-0"
                        />
                      ) : (
                        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary font-label-md text-label-md font-bold text-on-primary">
                          {initialsOf(name)}
                        </span>
                      )}
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-label-md text-label-md font-semibold text-on-surface">
                          {name}
                        </p>
                        {email && (
                          <p
                            className="truncate font-label-sm text-label-sm text-on-surface-variant"
                            title={email}
                          >
                            {email}
                          </p>
                        )}
                      </div>
                    </div>
                    <span className="inline-block rounded-full bg-primary-container px-2.5 py-0.5 font-label-sm text-label-sm font-medium text-primary">
                      {role}
                    </span>
                  </div>

                  <div className="py-1">
                    <Link
                      to={roleHome(role)}
                      onClick={() => setAvatarOpen(false)}
                      className="flex w-full items-center gap-2.5 px-4 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                    >
                      <span className="material-symbols-outlined text-lg text-on-surface-variant">
                        dashboard
                      </span>
                      Dashboard
                    </Link>
                    <Link
                      to={role === 'Admin' ? '/dashboard/admin/settings' : '/dashboard/talent/settings'}
                      onClick={() => setAvatarOpen(false)}
                      className="flex w-full items-center gap-2.5 px-4 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low"
                    >
                      <span className="material-symbols-outlined text-lg text-on-surface-variant">
                        settings
                      </span>
                      Settings
                    </Link>
                  </div>

                  <div className="border-t border-surface-variant py-1">
                    <button
                      type="button"
                      role="menuitem"
                      onClick={() => {
                        setAvatarOpen(false)
                        onSignOut()
                      }}
                      className="flex w-full items-center gap-2.5 px-4 py-2.5 font-label-md text-label-md text-on-surface transition-colors hover:bg-error-container/50 hover:text-error"
                    >
                      <span className="material-symbols-outlined text-lg">
                        logout
                      </span>
                      Sign out
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </header>
  )
}
