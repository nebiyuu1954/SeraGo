import { Link } from 'react-router-dom'
import { Container } from '../ui/Container.tsx'
import { config } from '../../config'

const columns = [
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

export default function Footer() {
  return (
    <footer className="border-t border-outline-variant bg-surface-container-lowest">
      <Container className="py-14">
        <div className="grid gap-10 md:grid-cols-[1.5fr_repeat(3,1fr)]">
          <div>
            <Link
              to="/"
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
                      to={link === 'About' ? '/about' : '#'}
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
            <a href="#" className="transition-colors hover:text-on-surface">
              Privacy
            </a>
            <a href="#" className="transition-colors hover:text-on-surface">
              Terms
            </a>
          </div>
        </div>
      </Container>
    </footer>
  )
}
