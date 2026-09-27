import { Link } from 'react-router-dom'
import { Container } from '../ui/Container.tsx'
import { config } from '../../config'
import { getStoredAuthTokens } from '../../api'
import { useAuthUser } from '../../hooks'
import type { RequiredRole } from '../../hooks'
import { pickRole, roleHome } from '../dashboard/roleNav.ts'

const publicColumns = [
  {
    title: 'Product',
    links: ['Job search', 'Smart matches', 'Job alerts', 'Salary insights'],
  },
  {
    title: 'Company',
    links: ['About', 'Blog', 'Careers', 'Press'],
  },
  {
    title: 'Resources',
    links: ['Help center', 'Community', 'Status', 'Changelog'],
  },
]

/** Columns shared by every signed-in footer. */
const sharedColumns = [
  { title: 'Company', links: ['About', 'Blog', 'Careers', 'Press'] },
  { title: 'Legal', links: ['Privacy', 'Terms'] },
]

/** The role-specific first column — each role sees its own footer. */
const roleColumns: Record<RequiredRole, { title: string; links: string[] }[]> = {
  Talent: [
    {
      title: 'For talent',
      links: ['Find jobs', 'Saved jobs', 'Job alerts', 'Resume tips'],
    },
  ],
  Admin: [
    {
      title: 'Operations',
      links: ['Job moderation', 'Users', 'Reports', 'Settings'],
    },
  ],
}

/** Real destinations for footer links; the rest are placeholders. */
const LINK_ROUTES: Record<string, string> = {
  About: '/about',
  Privacy: '/privacy',
  Terms: '/terms',
}

function linkTo(link: string): string {
  return LINK_ROUTES[link] ?? '#'
}

/**
 * Page footer. Public visitors get the marketing footer; signed-in users get
 * a role-specific footer (their own column + shared company/legal columns).
 */
export default function Footer() {
  const auth = useAuthUser()
  const role = auth.status === 'authenticated' ? pickRole(auth.user.roles) : null
  const columns =
    getStoredAuthTokens() && role
      ? [...roleColumns[role], ...sharedColumns]
      : publicColumns

  return (
    <footer className="border-t border-outline-variant bg-surface-container-lowest">
      <Container className="py-14">
        <div className="grid gap-10 md:grid-cols-[1.5fr_repeat(3,1fr)]">
          <div>
            <Link
              to={role ? roleHome(role) : '/'}
              className="font-headline-md text-headline-md font-bold tracking-tight text-primary"
              aria-label="SeraGo home"
            >
              SeraGo
            </Link>
            <p className="mt-4 max-w-xs text-body-md leading-relaxed text-on-surface-variant">
              Only jobs that matter to you. Smarter search, honest signals, and
              zero noise.
            </p>
          </div>
          {columns.map((col) => (
            <div key={col.title}>
              <h3 className="font-label-md text-label-md font-medium text-on-surface">
                {col.title}
              </h3>
              <ul className="mt-4 space-y-3">
                {col.links.map((link) => (
                  <li key={link}>
                    <Link
                      to={linkTo(link)}
                      className="text-body-md text-on-surface-variant transition-colors hover:text-on-surface"
                    >
                      {link}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <div className="mt-12 flex flex-col items-center justify-between gap-4 border-t border-outline-variant pt-6 text-body-md text-on-surface-variant sm:flex-row">
          <p>
            © {new Date().getFullYear()} {config.appName} v{config.appVersion}.
            All rights reserved.
          </p>
          <div className="flex gap-6">
            <Link
              to="/privacy"
              className="transition-colors hover:text-on-surface"
            >
              Privacy
            </Link>
            <Link
              to="/terms"
              className="transition-colors hover:text-on-surface"
            >
              Terms
            </Link>
          </div>
        </div>
      </Container>
    </footer>
  )
}
