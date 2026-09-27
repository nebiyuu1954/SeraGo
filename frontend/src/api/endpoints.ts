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
    /** GET/PUT/PATCH /api/account/settings — user settings (versioned JSON blob). */
    settings: '/account/settings',
    /** GET — confirm email from the emailed link: ?code=…&userId=… */
    emailConfirm: '/account/email/confirm',
    /** POST — resend the confirmation email: { email } */
    emailConfirmResend: '/account/email/confirm/resend',
    /**
     * POST — extract profile fields from the user's stored resume PDF.
     * Parse-only: nothing is persisted, the user reviews before saving.
     */
    profileResumeParse: '/account/profile/parse-resume',
  },
  jobs: {
    /** GET/POST /api/jobs — item routes (/api/jobs/{id}, .../submit) are built in api/jobs.ts. */
    list: '/jobs',
    /** GET /api/jobs/locations — distinct locations of the live feed, for the filter dropdown. */
    locations: '/jobs/locations',
    /** POST /api/jobs/for-you/match — run AI matching on the For You feed now. */
    forYouMatch: '/jobs/for-you/match',
  },
  savedJobs: {
    /** GET /api/saved-jobs — my saved jobs with lifecycle status. */
    list: '/saved-jobs',
  },
  applications: {
    /** POST /api/applications — apply to a Serago job. */
    create: '/applications',
    /** GET /api/applications — my applications (talent). */
    list: '/applications',
    /** GET /api/applications/{id} — single application detail. */
    detail: (id: string) => `/applications/${id}`,
  },
  sectors: {
    /** GET /api/sectors — the canonical sector list for pickers. */
    list: '/sectors',
    /** Admin management + sync — /api/admin/... (admin only). */
    admin: '/admin/sectors',
    /** POST /api/admin/sync/scraped-jobs — import + normalize scraped jobs. */
    syncScrapedJobs: '/admin/sync/scraped-jobs',
  },
  adminUsers: {
    list: '/admin/users',
    detail: (id: string) => `/admin/users/${id}`,
  },
  adminJobs: {
    /** GET /api/admin/jobs/views — admin paginated list of jobs by view count. */
    views: '/admin/jobs/views',
    /** GET /api/admin/jobs/views/stats — aggregate view analytics (admin). */
    viewsStats: '/admin/jobs/views/stats',
  },
  adminStats: {
    top: '/admin/stats/top',
    overview: '/admin/stats/overview',
  },
  adminAi: {
    /** GET /api/admin/ai/classification/stats — LLM usage + token totals (admin). */
    classificationStats: '/admin/ai/classification/stats',
    /** GET /api/admin/ai/classification/jobs — per-job classify audit trail (admin). */
    classificationJobs: '/admin/ai/classification/jobs',
  },
  notifications: {
    /** GET /api/notifications — paginated notification list. */
    list: '/notifications',
    /** GET /api/notifications/unread-count — badge count (Redis-backed). */
    unreadCount: '/notifications/unread-count',
    /** PATCH /api/notifications/read — mark specific notifications as read. */
    read: '/notifications/read',
    /** PATCH /api/notifications/read-all — mark all as read. */
    readAll: '/notifications/read-all',
  },
  scraper: {
    /** GET /api/admin/scraper/week-stats — list of scraper weeks. */
    weekStats: '/admin/scraper/week-stats',
    /** GET /api/admin/scraper/week/{periodStart} — full scraper week detail. */
    weekDetail: (periodStart: string) => `/admin/scraper/week/${periodStart}`,
  },
} as const

export type ApiEndpoint = (typeof API_ENDPOINTS)[keyof typeof API_ENDPOINTS]
