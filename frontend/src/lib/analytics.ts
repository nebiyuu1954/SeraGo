import ReactGA from 'react-ga4'
import { config } from '../config'

/**
 * Thin, centralized Google Analytics 4 (gtag.js) wrapper.
 *
 * Design notes:
 *  - Analytics only initialise when a measurement ID is configured AND we're in
 *    a production build. Dev and preview deployments stay completely silent.
 *  - Automatic page views are disabled (`send_page_view: false`); the SPA sends
 *    one `page_view` per route change via `<AnalyticsTracker />`. This avoids
 *    double counting against GA4's "history change" enhanced measurement.
 *  - Every helper is a safe no-op before init, so call sites never need guards.
 *
 * Never send PII (emails, names). Prefer templated route paths over raw ids.
 */

let ready = false

/** Initialise GA4 once. No-op when unconfigured or outside production. */
export function initAnalytics(): void {
  if (ready || !config.gaMeasurementId || !config.isProd) return

  ReactGA.initialize(config.gaMeasurementId, {
    gtagOptions: { send_page_view: false },
  })
  ready = true
}

/** Set a stable User ID to track authenticated users across devices. */
export function setUserId(userId: string | undefined): void {
  if (!ready) return
  ReactGA.set({ userId: userId })
}

/** Send a single page view for an SPA navigation. */
export function trackPageView(path: string, title?: string): void {
  if (!ready) return
  ReactGA.send({ hitType: 'pageview', page: path, title })
}

/** Send a custom event, e.g. trackEvent('apply_job', { job_id: id }). */
export function trackEvent(
  name: string,
  params?: Record<string, unknown>,
): void {
  if (!ready) return
  ReactGA.event(name, params)
}

/** Track client-side exceptions or form errors. */
export function trackException(description: string, fatal = false): void {
  if (!ready) return
  ReactGA.send({ hitType: 'exception', exDescription: description, exFatal: fatal })
}
