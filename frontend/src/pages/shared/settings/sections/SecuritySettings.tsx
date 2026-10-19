import { useEffect, useRef, useState } from 'react'
import { useFormik } from 'formik'
import { object, ref, string } from 'yup'
import AccordionSection from '../../../../components/ui/AccordionSection.tsx'
import PasswordInput from '../../../../components/auth/PasswordInput.tsx'
import {
  fetchConnectedAccounts,
  fetchExternalProviders,
  fetchTelegramLinkUrl,
  getApiErrorMessage,
  getStoredAuthTokens,
  linkTelegramAccount,
  setPassword,
  unlinkTelegramAccount,
  type ConnectedAccountItem,
} from '../../../../api'
import { meetsPasswordRules } from '../../../../components/auth/passwordRules.ts'
import type { SecuritySettings as SecuritySettingsType } from '../../../../types/settings.ts'

interface SecuritySettingsProps {
  value: SecuritySettingsType
  onChange: (next: SecuritySettingsType) => void
}

export default function SecuritySettings({ value, onChange }: SecuritySettingsProps) {
  return (
    <AccordionSection
      title="Security"
      description="Password, two-factor authentication, and connected accounts"
      icon="shield"
      completion={{ filled: value.hasPassword ? 1 : 0, total: 2 }}
    >
      {/* ── Change Password ── */}
      <div className="md:col-span-2">
        <ChangePasswordSection
          hasPassword={value.hasPassword}
          onPasswordSet={() => onChange({ ...value, hasPassword: true })}
        />
      </div>

      {/* ── Two-Factor Authentication (placeholder) ── */}
      <div className="md:col-span-2">
        <div className="flex items-center justify-between gap-4 py-3">
          <div className="min-w-0">
            <p className="font-label-md text-label-md text-on-surface">
              Two-factor authentication
            </p>
            <p className="font-label-sm text-label-sm text-on-surface-variant/70">
              Add an extra layer of security to your account.{' '}
              <span className="inline-flex items-center gap-1 rounded-full bg-surface-container-low px-2 py-0.5 text-label-xs font-medium text-on-surface-variant">
                Coming soon
              </span>
            </p>
          </div>
          <button
            type="button"
            disabled
            aria-label="Two-factor authentication"
            className="relative inline-flex h-5 w-9 shrink-0 cursor-not-allowed rounded-full bg-surface-variant opacity-50"
          >
            <span className="inline-block h-4 w-4 translate-x-0.5 transform rounded-full bg-white shadow-sm transition-transform mt-0.5" />
          </button>
        </div>
      </div>

      {/* ── Connected Accounts ── */}
      <div className="md:col-span-2">
        <ConnectedAccountsSection />
      </div>
    </AccordionSection>
  )
}

/* -------------------------------------------------------- Change Password */

function ChangePasswordSection({
  hasPassword,
  onPasswordSet,
}: {
  hasPassword: boolean
  onPasswordSet: () => void
}) {
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  const formik = useFormik({
    initialValues: { password: '', confirmPassword: '' },
    validationSchema: object({
      password: string()
        .required('Please choose a password.')
        .test(
          'password-rules',
          'Password does not meet all requirements.',
          (v) => (v ? meetsPasswordRules(v) : false),
        ),
      confirmPassword: string()
        .required('Please confirm your password.')
        .oneOf([ref('password')], 'Passwords do not match.'),
    }),
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      const tokens = getStoredAuthTokens()
      if (!tokens) {
        setError('You are not signed in.')
        return
      }
      try {
        await setPassword(tokens.accessToken, { password: values.password })
        setDone(true)
        onPasswordSet()
      } catch (err) {
        setError(getApiErrorMessage(err))
      }
    },
  })

  if (done) {
    return (
      <div className="flex items-start gap-3 rounded-xl border border-success/30 bg-success/5 p-4">
        <span className="material-symbols-outlined mt-0.5 text-xl text-success">
          check_circle
        </span>
        <div>
          <p className="font-label-md text-label-md font-semibold text-on-surface">
            Password updated
          </p>
          <p className="font-label-sm text-label-sm text-on-surface-variant">
            You can now sign in with your email and password.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="rounded-xl border border-surface-variant p-5">
      <div className="flex items-start gap-3 mb-4">
        <span className="material-symbols-outlined mt-0.5 text-xl text-primary">
          lock
        </span>
        <div>
          <p className="font-label-md text-label-md font-semibold text-on-surface">
            {hasPassword ? 'Change password' : 'Set a password'}
          </p>
          <p className="font-label-sm text-label-sm text-on-surface-variant">
            {hasPassword
              ? 'Update your account password.'
              : 'Add a password so you can also sign in with email and password.'}
          </p>
        </div>
      </div>

      {error && (
        <div
          role="alert"
          className="mb-4 flex items-start gap-2.5 rounded-xl border border-error/30 bg-error-container p-3.5 font-label-md text-label-md text-on-error-container"
        >
          <span className="material-symbols-outlined mt-0.5 text-lg">error</span>
          <span>{error}</span>
        </div>
      )}

      <form onSubmit={formik.handleSubmit} noValidate className="space-y-4">
        <PasswordInput
          id="settings-password"
          name="password"
          label={hasPassword ? 'New password' : 'Password'}
          autoComplete="new-password"
          placeholder="••••••••"
          showStrength
          value={formik.values.password}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          error={
            formik.touched.password && formik.errors.password
              ? formik.errors.password
              : undefined
          }
        />
        <PasswordInput
          id="settings-confirm-password"
          name="confirmPassword"
          label="Confirm password"
          autoComplete="new-password"
          placeholder="••••••••"
          value={formik.values.confirmPassword}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          error={
            formik.touched.confirmPassword && formik.errors.confirmPassword
              ? formik.errors.confirmPassword
              : undefined
          }
        />
        <button
          type="submit"
          disabled={formik.isSubmitting}
          className="inline-flex items-center justify-center gap-2 rounded-[0.125rem] bg-accent px-6 py-2.5 font-label-md text-label-md font-medium tracking-[0.05em] text-on-accent transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
        >
          {formik.isSubmitting ? (
            <>
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent" />
              Saving…
            </>
          ) : hasPassword ? (
            'Update password'
          ) : (
            'Set password'
          )}
        </button>
      </form>
    </div>
  )
}

/* --------------------------------------------------- Connected Accounts */

function ConnectedAccountsSection() {
  const [accounts, setAccounts] = useState<ConnectedAccountItem[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [telegramAvailable, setTelegramAvailable] = useState(false)
  const [botUsername, setBotUsername] = useState<string | null>(null)
  const scriptLoadedRef = useRef(false)

  const loadAccounts = () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setLoading(false)
      return
    }

    fetchConnectedAccounts(tokens.accessToken)
      .then((data) => {
        setAccounts(data.accounts)
      })
      .catch(() => {})
      .finally(() => {
        setLoading(false)
      })
  }

  useEffect(() => {
    loadAccounts()
    fetchExternalProviders()
      .then((data) => {
        const hasTg = data.providers.includes('Telegram') && Boolean(data.telegramBotUsername)
        setTelegramAvailable(hasTg)
        setBotUsername(data.telegramBotUsername ?? null)
      })
      .catch(() => {})
  }, [])

  // Lazily inject Telegram widget JS once bot is known
  useEffect(() => {
    if (!botUsername || scriptLoadedRef.current) return
    if (document.getElementById('telegram-widget-script')) {
      scriptLoadedRef.current = true
      return
    }

    const script = document.createElement('script')
    script.id = 'telegram-widget-script'
    script.src = 'https://telegram.org/js/telegram-widget.js?22'
    script.async = true
    script.onload = () => {
      scriptLoadedRef.current = true
    }
    document.head.appendChild(script)
  }, [botUsername])

  const googleAccount = accounts.find((a) => a.provider === 'Google')
  const telegramAccount = accounts.find((a) => a.provider === 'Telegram')
  const isGoogleConnected = Boolean(googleAccount?.connected)
  const isTelegramConnected = Boolean(telegramAccount?.connected)

  const handleConnectTelegram = () => {
    setError(null)
    setBusy(true)

    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setError('Please sign in again.')
      setBusy(false)
      return
    }

    if (window.Telegram?.Login?.auth && botUsername) {
      window.Telegram.Login.auth(
        { bot_id: botUsername, request_access: true },
        async (user) => {
          if (!user) {
            setBusy(false)
            return
          }
          try {
            await linkTelegramAccount(tokens.accessToken, user)
            loadAccounts()
          } catch (err) {
            setError(getApiErrorMessage(err))
          } finally {
            setBusy(false)
          }
        },
      )
    } else {
      // Fallback: deep-link to bot start handshake
      fetchTelegramLinkUrl(tokens.accessToken)
        .then(({ url }) => {
          window.open(url, '_blank', 'noopener,noreferrer')
        })
        .catch((err) => {
          setError(getApiErrorMessage(err))
        })
        .finally(() => {
          setBusy(false)
        })
    }
  }

  const handleDisconnectTelegram = async () => {
    setError(null)
    setBusy(true)
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setError('Please sign in again.')
      setBusy(false)
      return
    }

    try {
      await unlinkTelegramAccount(tokens.accessToken)
      loadAccounts()
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="rounded-xl border border-surface-variant p-5">
      <div className="flex items-start gap-3">
        <span className="material-symbols-outlined mt-0.5 text-xl text-primary">
          link
        </span>
        <div className="flex-1">
          <p className="font-label-md text-label-md font-semibold text-on-surface">
            Connected accounts
          </p>
          <p className="font-label-sm text-label-sm text-on-surface-variant mb-4">
            Manage third-party accounts linked to your SeraGo profile.
          </p>

          {error && (
            <div className="mb-4 rounded-lg bg-error-container/40 p-2.5 font-label-sm text-label-sm text-on-error-container">
              {error}
            </div>
          )}

          <div className="space-y-3">
            {/* ── Google ── */}
            <div className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-surface-variant p-3.5">
              <div className="flex items-center gap-3">
                <svg className="h-5 w-5 shrink-0" viewBox="0 0 24 24" aria-hidden="true">
                  <path
                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z"
                    fill="#4285F4"
                  />
                  <path
                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                    fill="#34A853"
                  />
                  <path
                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                    fill="#FBBC05"
                  />
                  <path
                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                    fill="#EA4335"
                  />
                </svg>
                <div>
                  <p className="font-label-md text-label-md font-medium text-on-surface">Google</p>
                  <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                    {isGoogleConnected ? (googleAccount?.email ?? 'OAuth connected') : 'Not connected'}
                  </p>
                </div>
              </div>

              {isGoogleConnected ? (
                <span className="inline-flex items-center gap-1 rounded-full bg-success/10 px-2.5 py-1 text-label-sm font-medium text-success">
                  <span className="material-symbols-outlined text-sm">check</span>
                  Connected
                </span>
              ) : (
                <span className="rounded-full bg-surface-variant/60 px-2.5 py-1 text-label-xs text-on-surface-variant/70">
                  Not connected
                </span>
              )}
            </div>

            {/* ── Telegram ── */}
            <div className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-surface-variant p-3.5">
              <div className="flex items-center gap-3">
                <svg
                  className="h-5 w-5 shrink-0"
                  viewBox="0 0 24 24"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                  aria-hidden="true"
                >
                  <path
                    d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm4.64 6.8c-.15 1.58-.8 5.42-1.13 7.19-.14.75-.42 1-.68 1.03-.58.05-1.02-.38-1.58-.75-.88-.58-1.38-.94-2.23-1.5-.99-.65-.35-1.01.22-1.59.15-.15 2.71-2.48 2.76-2.69a.2.2 0 00-.05-.18c-.06-.05-.14-.03-.21-.02-.09.02-1.49.95-4.22 2.79-.4.27-.76.41-1.08.4-.36-.01-1.04-.2-1.55-.37-.63-.2-1.12-.31-1.08-.66.02-.18.27-.36.74-.55 2.92-1.27 4.86-2.11 5.83-2.52 2.78-1.16 3.35-1.36 3.73-1.36.08 0 .27.02.39.12.1.08.13.19.14.27-.01.06.01.24 0 .37z"
                    fill="#229ED9"
                  />
                </svg>
                <div>
                  <p className="font-label-md text-label-md font-medium text-on-surface">Telegram</p>
                  <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                    {isTelegramConnected
                      ? (telegramAccount?.username
                          ? `@${telegramAccount.username}`
                          : `Connected (ID: ${telegramAccount?.id ?? 'linked'})`)
                      : 'Link your Telegram account for 1-click login and alerts'}
                  </p>
                </div>
              </div>

              <div className="flex items-center gap-2">
                {isTelegramConnected ? (
                  <>
                    <span className="inline-flex items-center gap-1 rounded-full bg-success/10 px-2.5 py-1 text-label-sm font-medium text-success">
                      <span className="material-symbols-outlined text-sm">check</span>
                      Connected
                    </span>
                    <button
                      type="button"
                      onClick={handleDisconnectTelegram}
                      disabled={busy}
                      className="rounded-full border border-surface-variant px-3 py-1 font-label-sm text-label-sm text-on-surface transition-colors hover:bg-surface-variant/50 disabled:opacity-50"
                    >
                      {busy ? 'Disconnecting…' : 'Disconnect'}
                    </button>
                  </>
                ) : (
                  <>
                    <span className="rounded-full bg-surface-variant/60 px-2.5 py-1 text-label-xs text-on-surface-variant/70">
                      Not connected
                    </span>
                    <button
                      type="button"
                      onClick={handleConnectTelegram}
                      disabled={busy || loading}
                      className="rounded-full bg-primary px-3 py-1 font-label-sm text-label-sm text-on-primary transition-opacity hover:opacity-90 disabled:opacity-50"
                    >
                      {busy ? 'Connecting…' : 'Connect Telegram'}
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
