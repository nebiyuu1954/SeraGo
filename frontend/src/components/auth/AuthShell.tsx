import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface AuthShellProps {
  /**
   * Headline under the brand. Omit (along with `subtitle`) on success states
   * so the confirmation card renders alone.
   */
  title?: string
  subtitle?: string
  children: ReactNode
  /** Content rendered below the card (e.g. "Already have an account? Sign in"). */
  footer?: ReactNode
}

/**
 * Split-screen layout shared by all standalone auth pages: the blue brand
 * panel (desktop only) + the centered form column with the mobile brand
 * header on small screens.
 */
export default function AuthShell({
  title,
  subtitle,
  children,
  footer,
}: AuthShellProps) {
  return (
    <div className="flex min-h-svh bg-surface-container-lowest">
      {/* ── Left brand panel (desktop only) ─────────────────────────── */}
      <aside className="relative hidden w-1/2 overflow-hidden bg-primary lg:block">
        {/* Decorative gradient wash + dot grid */}
        <div
          aria-hidden="true"
          className="absolute inset-0"
          style={{
            backgroundImage:
              'radial-gradient(1200px 600px at 85% -10%, rgba(255,102,0,0.35), transparent 60%), radial-gradient(900px 500px at -10% 110%, rgba(255,255,255,0.12), transparent 55%)',
          }}
        />
        <div
          aria-hidden="true"
          className="absolute inset-0 opacity-[0.08]"
          style={{
            backgroundImage:
              'radial-gradient(rgba(255,255,255,0.9) 1px, transparent 1px)',
            backgroundSize: '26px 26px',
          }}
        />

        {/* Content */}
        <div className="relative z-10 flex h-full flex-col justify-between p-10 xl:p-16">
          <Link
            to="/"
            className="font-headline-md text-headline-md font-bold tracking-tight text-on-primary"
            aria-label="SeraGo home"
          >
            Sera<span className="text-accent">Go</span>
          </Link>

          <div>
            <h2 className="font-display-lg-mobile text-display-lg-mobile font-bold leading-tight tracking-tight text-on-primary xl:text-[52px] xl:leading-[60px]">
              Only jobs that <span className="text-accent">matter</span> to you.
            </h2>
            <p className="mt-4 max-w-md font-body-lg text-body-lg text-on-primary/80">
              Smarter search, honest signals, and zero noise. Join the community
              that connects Ethiopian talent with the companies that need them.
            </p>
          </div>

          {/* Floating glass stat cards */}
          <div className="flex gap-4">
            {[
              { icon: 'bolt', value: '2×', label: 'faster job matches' },
              { icon: 'verified', value: '1,200+', label: 'companies hiring' },
              { icon: 'diversity_3', value: '40k+', label: 'active talents' },
            ].map((stat) => (
              <div
                key={stat.label}
                className="flex flex-1 items-center gap-3 rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur-sm transition-transform duration-300 hover:-translate-y-1"
              >
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined text-2xl text-accent"
                >
                  {stat.icon}
                </span>
                <div>
                  <div className="font-headline-md text-headline-md font-bold leading-none text-on-primary">
                    {stat.value}
                  </div>
                  <div className="mt-1 font-label-sm text-label-sm text-on-primary/75">
                    {stat.label}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </aside>

      {/* ── Form panel ──────────────────────────────────────────────── */}
      <main className="flex flex-1 flex-col justify-center px-4 py-10 sm:px-8 lg:w-1/2 lg:px-16 xl:px-24">
        <div className="mx-auto w-full max-w-md">
          {/* Mobile brand header */}
          <div className="mb-8 text-center lg:hidden">
            <Link
              to="/"
              className="font-headline-md text-headline-md font-bold tracking-tight text-primary"
              aria-label="SeraGo home"
            >
              Sera<span className="text-accent">Go</span>
            </Link>
            <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
              Only jobs that matter to you
            </p>
          </div>

          {/* Form header */}
          {title && (
            <div className="mb-8 text-center lg:text-left">
              <h2 className="font-headline-lg text-headline-lg font-semibold text-on-surface">
                {title}
              </h2>
              <p className="mt-1.5 font-body-md text-body-md text-on-surface-variant">
                {subtitle}
              </p>
            </div>
          )}

          {children}
          {footer && <div className="mt-8 text-center">{footer}</div>}
        </div>
      </main>
    </div>
  )
}
