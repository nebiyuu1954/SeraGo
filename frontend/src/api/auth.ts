import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'
import { config } from '../config'
import type {
  AuthTokenResponse,
  ExternalAuthInfo,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  SignInRequest,
  SignUpExternalRequest,
  SignUpRequest,
  SignUpResponse,
  WhoAmIResponse,
} from '../types'

/** localStorage key holding the signed-in user's token pair. */
export const AUTH_TOKENS_KEY = 'serago-auth-tokens'

/** Normalized, persisted copy of the Aufy token response. */
export interface StoredAuthTokens {
  tokenType: string
  accessToken: string
  /**
   * Nullable: the external (Google) flows deliver the refresh token as the
   * httpOnly `Aufy.RefreshToken` cookie, not in the body.
   */
  refreshToken: string | null
  /** Access token lifetime in seconds. */
  expiresIn: number
  /** Unix ms when the tokens were issued — for client-side expiry checks. */
  issuedAt: number
}

/**
 * Creates an account. With `RequireConfirmedEmail = false` the account is
 * usable immediately — callers typically follow up with `signIn`.
 *
 * POST /api/auth/signup
 */
export function signUp(payload: SignUpRequest): Promise<SignUpResponse> {
  return request<SignUpResponse>(API_ENDPOINTS.auth.signup, {
    method: 'POST',
    body: payload,
  })
}

/**
 * Exchanges email + password for an access/refresh token pair.
 *
 * POST /api/auth/token
 */
export function signIn(payload: SignInRequest): Promise<AuthTokenResponse> {
  return request<AuthTokenResponse>(API_ENDPOINTS.auth.token, {
    method: 'POST',
    body: payload,
  })
}

/**
 * Requests a password-reset email. Returns 200 whether or not the account
 * exists (no user enumeration) and the response body is EMPTY.
 *
 * POST /api/account/password/forgot
 */
export function forgotPassword(payload: ForgotPasswordRequest): Promise<void> {
  return request<void>(API_ENDPOINTS.account.passwordForgot, {
    method: 'POST',
    body: payload,
  })
}

/**
 * Confirms the account email using the code + userId from the emailed link.
 * Returns 200 on success; a bad/expired code returns 404. Body is EMPTY.
 *
 * GET /api/account/email/confirm?code=…&userId=…
 */
export function confirmEmail(code: string, userId: string): Promise<void> {
  const params = new URLSearchParams({ code, userId })
  return request<void>(`${API_ENDPOINTS.account.emailConfirm}?${params.toString()}`)
}

/**
 * Resends the email-confirmation link. Always returns 200 (no user
 * enumeration — the email is only sent when the account exists and is
 * unconfirmed). Body is EMPTY.
 *
 * POST /api/account/email/confirm/resend
 */
export function resendConfirmationEmail(email: string): Promise<void> {
  return request<void>(API_ENDPOINTS.account.emailConfirmResend, {
    method: 'POST',
    body: { email },
  })
}

/**
 * Completes the password reset using the code from the emailed link.
 * Response body is EMPTY; a bad/expired code returns a 400 ProblemDetails.
 *
 * POST /api/account/password/reset
 */
export function resetPassword(payload: ResetPasswordRequest): Promise<void> {
  return request<void>(API_ENDPOINTS.account.passwordReset, {
    method: 'POST',
    body: payload,
  })
}

/**
 * Returns the currently authenticated user's identity.
 *
 * GET /api/auth/whoami
 */
export function fetchWhoAmI(accessToken: string): Promise<WhoAmIResponse> {
  return request<WhoAmIResponse>(API_ENDPOINTS.auth.whoami, {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * Revokes the current refresh token server-side (Bearer authed). Best-effort:
 * callers should clear local tokens regardless of the outcome.
 *
 * POST /api/auth/signout
 */
export function signOut(accessToken: string): Promise<void> {
  return request<void>(API_ENDPOINTS.auth.signout, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * Exchanges the httpOnly `Aufy.RefreshToken` cookie for a fresh access token
 * pair (the cookie is rotated server-side). No body or Bearer header needed.
 *
 * Called automatically by the API client on 401 — see client.ts.
 *
 * POST /api/auth/token/refresh (cookie auth)
 */
export function refreshAccessToken(): Promise<AuthTokenResponse> {
  return request<AuthTokenResponse>(API_ENDPOINTS.auth.tokenRefresh, {
    method: 'POST',
    credentials: 'include',
  })
}

/**
 * Completes the Google OAuth handshake for an EXISTING account: the backend
 * signed the user in via the short-lived OAuth cookie it set during the
 * callback redirect, and this call exchanges it for a token pair.
 *
 * Returns 400 "User not found" for brand-new users (custom external signup
 * flow) — callers should then show the role/name step and use
 * {@link signUpExternal}.
 *
 * POST /api/auth/signin/external (cookie auth)
 */
export function signInExternal(): Promise<AuthTokenResponse> {
  return request<AuthTokenResponse>(API_ENDPOINTS.auth.external.signIn, {
    method: 'POST',
    credentials: 'include',
  })
}

/**
 * Creates the account for a brand-new Google user (role + display name are
 * collected on the frontend after the OAuth handshake). Returns the token pair.
 *
 * POST /api/auth/signup/external (cookie auth)
 */
export function signUpExternal(
  payload: SignUpExternalRequest,
): Promise<AuthTokenResponse> {
  return request<AuthTokenResponse>(API_ENDPOINTS.auth.external.signUp, {
    method: 'POST',
    body: payload,
    credentials: 'include',
  })
}

/**
 * Fetches the verified external user info (email, name, avatar) from Google
 * using the external identity cookie.
 *
 * GET /api/auth/external/info (cookie auth)
 */
export function fetchExternalInfo(): Promise<ExternalAuthInfo> {
  return request<ExternalAuthInfo>(API_ENDPOINTS.auth.external.info, {
    method: 'GET',
    credentials: 'include',
  })
}

/**
 * Lists the OAuth providers the backend has registered (has non-blank
 * credentials), e.g. { providers: ['Google', 'Telegram'], telegramBotUsername: 'serago_bot' }.
 *
 * GET /api/auth/external/providers
 */
export function fetchExternalProviders(): Promise<{
  providers: string[]
  telegramBotUsername?: string | null
}> {
  return request<{ providers: string[]; telegramBotUsername?: string | null }>(
    API_ENDPOINTS.auth.external.providers,
  )
}

export interface TelegramAuthPayload {
  id: number
  first_name?: string
  last_name?: string
  username?: string
  photo_url?: string
  auth_date: number
  hash: string
}

export interface TelegramSignUpRequest {
  signupToken?: string
  authPayload?: TelegramAuthPayload
  email: string
  phoneNumber: string
  password: string
  firstName?: string
  middleName?: string
  lastName?: string
  role?: string
}

export interface ConnectedAccountItem {
  provider: 'Google' | 'Telegram' | string
  displayName: string
  connected: boolean
  email?: string | null
  username?: string | null
  id?: string | null
}

export interface ConnectedAccountsResponse {
  hasPassword: boolean
  accounts: ConnectedAccountItem[]
}

/**
 * Sign in using verified Telegram widget payload.
 *
 * POST /api/auth/telegram/login
 */
export function signInTelegram(payload: TelegramAuthPayload): Promise<AuthTokenResponse> {
  return request<AuthTokenResponse>(API_ENDPOINTS.auth.telegram.login, {
    method: 'POST',
    body: payload,
  })
}

/**
 * Complete sign up for brand-new Telegram user.
 *
 * POST /api/auth/telegram/signup
 */
export function signUpTelegram(payload: TelegramSignUpRequest): Promise<AuthTokenResponse> {
  return request<AuthTokenResponse>(API_ENDPOINTS.auth.telegram.signup, {
    method: 'POST',
    body: payload,
  })
}

/**
 * Fetches third-party connected accounts (Google, Telegram) for the current user.
 *
 * GET /api/account/connected-accounts
 */
export function fetchConnectedAccounts(accessToken: string): Promise<ConnectedAccountsResponse> {
  return request<ConnectedAccountsResponse>(API_ENDPOINTS.account.connectedAccounts, {
    method: 'GET',
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * Links a Telegram account to the signed-in user.
 *
 * POST /api/account/connected-accounts/telegram/link
 */
export function linkTelegramAccount(
  accessToken: string,
  payload: TelegramAuthPayload,
): Promise<{ message: string; username?: string; id?: string }> {
  return request(API_ENDPOINTS.account.linkTelegram, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: payload,
  })
}

/**
 * Disconnects the linked Telegram account.
 *
 * POST /api/account/connected-accounts/telegram/unlink
 */
export function unlinkTelegramAccount(accessToken: string): Promise<{ message: string }> {
  return request(API_ENDPOINTS.account.unlinkTelegram, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
  })
}

/**
 * URL that starts the Google OAuth dance. The browser navigates here; after
 * Google consent the backend redirects back to `callbackUrl` with the OAuth
 * cookie set.
 */
export function buildGoogleChallengeUrl(callbackUrl: string): string {
  const params = new URLSearchParams({ callbackUrl })
  return `${config.apiBaseUrl}${API_ENDPOINTS.auth.external.challenge}/Google?${params.toString()}`
}

/** Persists a token pair to localStorage. */
export function storeAuthTokens(tokens: AuthTokenResponse): StoredAuthTokens {
  const stored: StoredAuthTokens = {
    tokenType: tokens.TokenType,
    accessToken: tokens.AccessToken,
    refreshToken: tokens.RefreshToken,
    expiresIn: tokens.ExpiresIn,
    issuedAt: Date.now(),
  }
  try {
    window.localStorage.setItem(AUTH_TOKENS_KEY, JSON.stringify(stored))
  } catch {
    // Storage unavailable (private mode, quota) — the session just won't persist.
  }
  return stored
}

/** Reads the persisted token pair, or null when not signed in. */
export function getStoredAuthTokens(): StoredAuthTokens | null {
  try {
    const raw = window.localStorage.getItem(AUTH_TOKENS_KEY)
    return raw ? (JSON.parse(raw) as StoredAuthTokens) : null
  } catch {
    return null
  }
}

/** Removes the persisted token pair (sign out). */
export function clearStoredAuthTokens(): void {
  try {
    window.localStorage.removeItem(AUTH_TOKENS_KEY)
  } catch {
    // Storage unavailable — nothing to clear.
  }
}
