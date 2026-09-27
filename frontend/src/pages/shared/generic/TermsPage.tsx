import { Link } from 'react-router-dom'
import { Badge } from '../../../components/ui/Badge.tsx'
import { Button } from '../../../components/ui/Button.tsx'
import { Container } from '../../../components/ui/Container.tsx'

const LAST_UPDATED = 'September 2026'

/**
 * Lightweight Terms of Service.
 *
 * Not a legal-grade agreement — it exists so the "Terms of Service" links
 * across the app aren't dead ends and so the basics of using SeraGo (accounts,
 * acceptable use, aggregated listings) are stated plainly.
 */
export default function TermsPage() {
  return (
    <section className="py-20 sm:py-28">
      <Container>
        <div className="mx-auto max-w-3xl">
          <Badge>
            <span className="size-1.5 rounded-full bg-primary" />
            Terms
          </Badge>

          <h1 className="mt-6 text-balance font-display-lg-mobile font-bold tracking-tight text-on-surface sm:text-display-lg">
            Terms of <span className="text-primary">Service</span>
          </h1>

          <p className="mt-6 text-pretty text-body-lg leading-relaxed text-on-surface-variant">
            These terms explain the ground rules for using SeraGo. By creating an
            account or using the service, you agree to them. We&rsquo;ve kept the
            language plain.
          </p>
          <p className="mt-4 font-label-sm text-label-sm text-on-surface-variant">
            Last updated: {LAST_UPDATED}
          </p>

          <h2 className="mt-14 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Your account
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            You need an account to save jobs, apply, and track your
            applications. Keep your login details secure and give us accurate
            information — you&rsquo;re responsible for activity that happens
            under your account.
          </p>

          <h2 className="mt-12 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Job listings
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            SeraGo brings together listings from multiple job websites alongside
            roles posted directly on the platform. We aim to keep listings
            accurate and up to date, but we don&rsquo;t control third-party
            sites and can&rsquo;t guarantee that every listing is still open or
            that its details are complete. Always confirm the role with the
            employer before making decisions based on it.
          </p>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            When you apply to a job, the information you choose to share (for
            example your resume and cover letter) may be passed to the employer
            or the source site the listing came from.
          </p>

          <h2 className="mt-12 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Using SeraGo fairly
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            Don&rsquo;t misuse the service. That means no scraping or bulk
            copying of listings, no automated or fraudulent applications, no
            attempts to break into or overload our systems, and no content that
            is unlawful or misleading. We may suspend accounts that break these
            rules.
          </p>

          <h2 className="mt-12 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Availability and changes
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            SeraGo is provided as-is while it continues to grow, so features may
            change or be interrupted. We may update these terms from time to
            time; when we do, we&rsquo;ll revise the date at the top. You can
            stop using the service and delete your account at any time.
          </p>

          <div className="mt-16 rounded-2xl border border-outline-variant bg-surface-container-low p-8 text-center">
            <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
              Questions about these terms?
            </h2>
            <p className="mx-auto mt-2 max-w-md text-on-surface-variant">
              Reach out and we&rsquo;ll be glad to clarify anything here.              See also our{' '}
              <Link
                to="/privacy"
                className="font-medium text-primary transition-opacity hover:opacity-80"
              >
                Privacy notice
              </Link>
              .
            </p>
            <Button to="/" className="mt-6">
              Back to home
            </Button>
          </div>
        </div>
      </Container>
    </section>
  )
}
