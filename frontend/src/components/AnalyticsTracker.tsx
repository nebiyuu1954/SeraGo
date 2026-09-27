import { useEffect } from 'react'
import { matchPath, useLocation } from 'react-router-dom'
import { trackPageView } from '../lib/analytics'

const BASE_TITLE = 'SeraGo'

/**
 * Route → human-readable page title.
 *
 * Listed most-specific first so literal segments never get shadowed by a
 * dynamic one (e.g. `/dashboard/admin/jobs/views` must win over `:jobId`).
 * Kept in sync with the routes in App.tsx.
 */
const ROUTE_TITLES: { pattern: string; title: string }[] = [
  { pattern: '/', title: 'Find jobs that matter' },
  { pattern: '/about', title: 'About' },
  { pattern: '/privacy', title: 'Privacy' },
  { pattern: '/terms', title: 'Terms' },
  { pattern: '/signup', title: 'Create an account' },
  { pattern: '/login', title: 'Log in' },
  { pattern: '/forgot-password', title: 'Reset your password' },
  { pattern: '/reset-password', title: 'Choose a new password' },
  { pattern: '/confirm-email', title: 'Confirm your email' },
  { pattern: '/auth/google/callback', title: 'Signing in' },
  { pattern: '/dashboard/talent/jobs/:jobId', title: 'Job details' },
  { pattern: '/dashboard/talent/saved', title: 'Saved jobs' },
  { pattern: '/dashboard/talent/profile/preview', title: 'Profile preview' },
  { pattern: '/dashboard/talent/profile', title: 'My profile' },
  { pattern: '/dashboard/talent/settings', title: 'Settings' },
  { pattern: '/dashboard/talent', title: 'Find jobs' },
  { pattern: '/dashboard/admin/jobs/views', title: 'Job views' },
  { pattern: '/dashboard/admin/jobs/:jobId/preview', title: 'Job preview' },
  { pattern: '/dashboard/admin/jobs/:jobId/edit', title: 'Edit job' },
  { pattern: '/dashboard/admin/jobs/:jobId', title: 'Job details' },
  { pattern: '/dashboard/admin/jobs', title: 'Manage jobs' },
  {
    pattern: '/dashboard/admin/scraper/weeks/:periodStart',
    title: 'Scraper week',
  },
  { pattern: '/dashboard/admin/scraper', title: 'Scraper' },
  { pattern: '/dashboard/admin/ai', title: 'AI features' },
  { pattern: '/dashboard/admin/users/:userId', title: 'User details' },
  { pattern: '/dashboard/admin/users', title: 'Users' },
  { pattern: '/dashboard/admin/sectors', title: 'Sectors' },
  { pattern: '/dashboard/admin/profile', title: 'My profile' },
  { pattern: '/dashboard/admin/settings', title: 'Settings' },
  { pattern: '/dashboard/admin', title: 'Admin dashboard' },
  { pattern: '/dashboard', title: 'Dashboard' },
]

/** Resolve a pathname to its page title (falls back to the 404 title). */
function titleFor(pathname: string): string {
  const match = ROUTE_TITLES.find((r) => matchPath(r.pattern, pathname))
  return match?.title ?? 'Page not found'
}

/**
 * Owns two router-level side effects:
 *  1. Sets a per-route `document.title` (so browser tabs / history / GA4 page
 *     titles are meaningful — the app is a SPA, so index.html's static title
 *     would otherwise never change).
 *  2. Sends a GA4 `page_view` on every client-side navigation. GA4 does not
 *     detect SPA route changes on its own and automatic page views are
 *     disabled in the analytics wrapper, so this is the single source of
 *     page views.
 *
 * Rendered once inside the router (see App.tsx).
 */
export default function AnalyticsTracker() {
  const location = useLocation()

  useEffect(() => {
    document.title = `${titleFor(location.pathname)} · ${BASE_TITLE}`
    trackPageView(location.pathname + location.search, document.title)
  }, [location.pathname, location.search])

  return null
}
