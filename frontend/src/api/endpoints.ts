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
    passwordForgot: '/account/password/forgot',
    passwordReset: '/account/password/reset',
  },
} as const

export type ApiEndpoint = (typeof API_ENDPOINTS)[keyof typeof API_ENDPOINTS]
