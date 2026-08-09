/**
 * Centralized, environment-driven configuration.
 *
 * Vite exposes env vars prefixed with `VITE_` on `import.meta.env`.
 * Everything the app needs to know about its environment lives here,
 * so switching hosting providers only means setting new env vars —
 * never touching code.
 */
const env = import.meta.env

export const config = {
  appName: env.VITE_APP_NAME ?? 'SeraGo',
  appVersion: env.VITE_APP_VERSION ?? '0.1.0',
  /**
   * Base URL for the SeraGo API. Override per environment via
   * `VITE_API_BASE_URL` (see `.env.example`).
   */
  apiBaseUrl: env.VITE_API_BASE_URL ?? 'http://localhost:3000/api',
  isDev: import.meta.env.DEV,
  isProd: import.meta.env.PROD,
} as const

export type AppConfig = typeof config
