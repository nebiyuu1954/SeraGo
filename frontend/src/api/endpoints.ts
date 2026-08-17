/**
 * Central registry of SeraGo API endpoint paths.
 * Add new endpoints here as the backend grows — never scatter paths
 * across feature code.
 */
export const API_ENDPOINTS = {
  health: '/health',
  auth: {
    signup: '/auth/signup',
    token: '/auth/token',
    tokenRefresh: '/auth/token/refresh',
    signout: '/auth/signout',
    whoami: '/auth/whoami',
    external: {
      /** GET — lists configured OAuth providers: { providers: ['Google'] }. */
      providers: '/auth/external/providers',
      /** GET — starts the OAuth dance: /auth/external/challenge/{provider}?callbackUrl=… */
      challenge: '/auth/external/challenge',
      /** POST (cookie) — completes sign-in for an existing Google account. */
      signIn: '/auth/signin/external',
      /** POST (cookie) — creates the account for a brand-new Google user. */
      signUp: '/auth/signup/external',
    },
  },
  account: {
    info: '/account/info',
    profile: '/account/profile',
    passwordForgot: '/account/password/forgot',
    passwordReset: '/account/password/reset',
    passwordSet: '/account/password/set',
    /** GET — confirm email from the emailed link: ?code=…&userId=… */
    emailConfirm: '/account/email/confirm',
    /** POST — resend the confirmation email: { email } */
    emailConfirmResend: '/account/email/confirm/resend',
  },
  jobs: {
    /** GET/POST /api/jobs — item routes (/api/jobs/{id}, .../submit) are built in api/jobs.ts. */
    list: '/jobs',
    /** GET /api/jobs/locations — distinct locations of the live feed, for the filter dropdown. */
    locations: '/jobs/locations',
  },
  savedJobs: {
    /** GET /api/saved-jobs — my saved jobs with lifecycle status. */
    list: '/saved-jobs',
  },
  sectors: {
    /** GET /api/sectors — the canonical sector list for pickers. */
    list: '/sectors',
    /** Admin management + sync — /api/admin/... (admin only). */
    admin: '/admin/sectors',
    /** POST /api/admin/sync/scraped-jobs — import + normalize scraped jobs. */
    syncScrapedJobs: '/admin/sync/scraped-jobs',
  },
  stats: {
    /** GET /api/admin/stats/top?period=day|week|month|year — top sectors + websites (admin). */
    top: '/admin/stats/top',
  },
} as const

export type ApiEndpoint = (typeof API_ENDPOINTS)[keyof typeof API_ENDPOINTS]
