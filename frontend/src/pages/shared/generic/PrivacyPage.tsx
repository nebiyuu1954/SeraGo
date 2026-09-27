import { Badge } from '../../../components/ui/Badge.tsx'
import { Button } from '../../../components/ui/Button.tsx'
import { Container } from '../../../components/ui/Container.tsx'

const LAST_UPDATED = 'September 2026'

/**
 * Lightweight privacy notice.
 *
 * Not a legal-grade policy — it exists so the "Privacy Policy" links across the
 * app aren't dead ends and so the Google Analytics 4 usage is disclosed to
 * visitors before any data is collected.
 */
export default function PrivacyPage() {
  return (
    <section className="py-20 sm:py-28">
      <Container>
        <div className="mx-auto max-w-3xl">
          <Badge>
            <span className="size-1.5 rounded-full bg-primary" />
            Privacy
          </Badge>

          <h1 className="mt-6 text-balance font-display-lg-mobile font-bold tracking-tight text-on-surface sm:text-display-lg">
            Your data, <span className="text-primary">explained</span>
          </h1>

          <p className="mt-6 text-pretty text-body-lg leading-relaxed text-on-surface-variant">
            This page explains what SeraGo collects, why we collect it, and the
            choices you have. We keep it short and in plain language.
          </p>
          <p className="mt-4 font-label-sm text-label-sm text-on-surface-variant">
            Last updated: {LAST_UPDATED}
          </p>

          <h2 className="mt-14 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            What we collect
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            When you create an account we store the details you give us — your
            name, email address, and profile content such as your resume, work
            experience, and preferences. We use this to run your account and to
            match you with relevant jobs. We never sell your personal data.
          </p>

          <h2 className="mt-12 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Analytics
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            In production we use{' '}
            <span className="font-medium text-on-surface">
              Google Analytics 4
            </span>{' '}
            to understand how the product is used — for example which pages are
            visited and how often key actions like signing up, saving a job, or
            applying to a job happen. This helps us improve SeraGo.
          </p>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            Analytics is loaded only in production builds. It records the pages
            and actions above along with coarse technical information such as
            your device type and approximate location derived from your IP
            address. Events about your activity refer to jobs and pages by
            identifier — we do{' '}
            <span className="font-medium text-on-surface">
              not send your name or email address
            </span>{' '}
            to Google Analytics. Google may set cookies or use similar
            identifiers to measure this data on our behalf. See Google&rsquo;s{' '}
            <a
              href="https://policies.google.com/technologies/partner-sites"
              target="_blank"
              rel="noreferrer"
              className="font-medium text-primary transition-opacity hover:opacity-80"
            >
              privacy &amp; terms
            </a>{' '}
            for how it handles that data.
          </p>

          <h2 className="mt-12 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Your choices
          </h2>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            You can update or remove most of your information at any time from
            your profile and settings. You can also block analytics cookies and
            scripts in your browser without losing access to the core product.
          </p>

          <div className="mt-16 rounded-2xl border border-outline-variant bg-surface-container-low p-8 text-center">
            <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
              Questions about your data?
            </h2>
            <p className="mx-auto mt-2 max-w-md text-on-surface-variant">
              Reach out and we&rsquo;ll help you understand or remove the data
              we hold about you.
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
