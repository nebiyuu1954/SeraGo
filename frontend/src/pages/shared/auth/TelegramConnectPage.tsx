import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
  ApiError,
  clearStoredAuthTokens,
  connectTelegramChat,
  getApiErrorMessage,
  getStoredAuthTokens,
  signIn,
  storeAuthTokens,
} from '../../../api'
import AuthShell from '../../../components/auth/AuthShell.tsx'
import ErrorBanner from '../../../components/auth/ErrorBanner.tsx'
import TextInput from '../../../components/auth/TextInput.tsx'
import PasswordInput from '../../../components/auth/PasswordInput.tsx'
import SubmitButton from '../../../components/auth/SubmitButton.tsx'

interface LoginValues {
  email: string
  password: string
}

const validationSchema = object<LoginValues>({
  email: string()
    .trim()
    .email('Please enter a valid email address.')
    .required('Please enter your email address.'),
  password: string().required('Please enter your password.'),
})

type ConflictType =
  | 'ACCOUNT_ALREADY_CONNECTED'
  | 'ALREADY_CONNECTED_SAME_CHAT'
  | 'CHAT_ALREADY_CONNECTED_TO_ANOTHER'
  | null

export default function TelegramConnectPage() {
  const [searchParams] = useSearchParams()
  const chatIdParam = searchParams.get('chat_id')
  const token = searchParams.get('token')

  const chatId = chatIdParam ? parseInt(chatIdParam, 10) : null

  const [state, setState] = useState<'idle' | 'connecting' | 'connected' | 'already_connected' | 'error'>(() => {
    if (!chatId || !token) return 'error'
    const stored = getStoredAuthTokens()
    return stored ? 'connecting' : 'idle'
  })
  const [conflictType, setConflictType] = useState<ConflictType>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(() => {
    if (!chatId || !token) {
      return 'Missing or invalid Telegram connection link. Please request a new link from @My_Serago_Bot.'
    }
    return null
  })

  const handleConnectionError = (err: unknown) => {
    if (err instanceof ApiError && err.status === 409) {
      const code = err.payload?.messageCode
      if (code === 'ALREADY_CONNECTED_SAME_CHAT') {
        setConflictType('ALREADY_CONNECTED_SAME_CHAT')
      } else if (code === 'CHAT_ALREADY_CONNECTED_TO_ANOTHER') {
        setConflictType('CHAT_ALREADY_CONNECTED_TO_ANOTHER')
      } else {
        setConflictType('ACCOUNT_ALREADY_CONNECTED')
      }
      setState('already_connected')
      setErrorMessage(getApiErrorMessage(err))
      return
    }

    setState('idle')
    setErrorMessage(getApiErrorMessage(err))
  }

  // Auto-connect if already signed in
  useEffect(() => {
    const stored = getStoredAuthTokens()
    if (!stored || !chatId || !token) return

    let cancelled = false
    setState('connecting')
    setErrorMessage(null)

    connectTelegramChat(stored.accessToken, chatId, token)
      .then(() => {
        if (!cancelled) setState('connected')
      })
      .catch((err) => {
        if (!cancelled) {
          handleConnectionError(err)
        }
      })

    return () => {
      cancelled = true
    }
  }, [chatId, token])

  const formik = useFormik<LoginValues>({
    initialValues: { email: '', password: '' },
    validationSchema,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      if (!chatId || !token) {
        setErrorMessage('Missing connection parameters. Please use the link provided by the bot.')
        return
      }
      setErrorMessage(null)
      setState('connecting')
      try {
        const tokens = await signIn({
          email: values.email,
          password: values.password,
        })
        storeAuthTokens(tokens)
        await connectTelegramChat(tokens.AccessToken, chatId, token)
        setState('connected')
      } catch (err) {
        handleConnectionError(err)
      }
    },
  })

  const handleSwitchAccount = () => {
    clearStoredAuthTokens()
    formik.resetForm()
    setConflictType(null)
    setErrorMessage(null)
    setState('idle')
  }

  // ── Connected Success View ──
  if (state === 'connected') {
    return (
      <AuthShell>
        <div className="text-center py-6">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10 text-success">
            <span className="material-symbols-outlined text-4xl">check_circle</span>
          </div>
          <h1 className="font-headline-sm text-headline-sm font-bold text-on-surface">
            Telegram Connected!
          </h1>
          <p className="mt-2 font-body-md text-body-md text-on-surface-variant max-w-sm mx-auto">
            Your Telegram chat has been successfully linked to your SeraGo account. Personalized job alerts will now be sent directly to your bot chat.
          </p>

          <div className="mt-8 flex flex-col gap-3">
            <a
              href="https://t.me/My_Serago_Bot"
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-primary transition-opacity hover:opacity-90 shadow-sm"
            >
              <span className="material-symbols-outlined text-xl">send</span>
              Return to Telegram Bot
            </a>
            <Link
              to="/dashboard/talent"
              className="inline-flex w-full items-center justify-center rounded-xl border border-surface-variant px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-surface transition-colors hover:bg-surface-variant/30"
            >
              Go to Dashboard
            </Link>
          </div>
        </div>
      </AuthShell>
    )
  }

  // ── Already Connected Conflict View ──
  if (state === 'already_connected') {
    if (conflictType === 'ALREADY_CONNECTED_SAME_CHAT') {
      return (
        <AuthShell>
          <div className="text-center py-6">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-primary">
              <span className="material-symbols-outlined text-4xl">verified</span>
            </div>
            <h1 className="font-headline-sm text-headline-sm font-bold text-on-surface">
              Already Connected!
            </h1>
            <p className="mt-2 font-body-md text-body-md text-on-surface-variant max-w-sm mx-auto">
              Your SeraGo account is already connected to this Telegram chat. Matched job alerts are active and delivered directly to your chat.
            </p>

            <div className="mt-8 flex flex-col gap-3">
              <a
                href="https://t.me/My_Serago_Bot"
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-primary transition-opacity hover:opacity-90 shadow-sm"
              >
                <span className="material-symbols-outlined text-xl">send</span>
                Return to Telegram Bot
              </a>
              <Link
                to="/dashboard/talent"
                className="inline-flex w-full items-center justify-center rounded-xl border border-surface-variant px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-surface transition-colors hover:bg-surface-variant/30"
              >
                Go to Dashboard
              </Link>
            </div>
          </div>
        </AuthShell>
      )
    }

    if (conflictType === 'CHAT_ALREADY_CONNECTED_TO_ANOTHER') {
      return (
        <AuthShell>
          <div className="text-center py-6">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-amber-500/10 text-amber-500">
              <span className="material-symbols-outlined text-4xl">link_off</span>
            </div>
            <h1 className="font-headline-sm text-headline-sm font-bold text-on-surface">
              Telegram Chat Already Linked
            </h1>
            <p className="mt-2 font-body-md text-body-md text-on-surface-variant max-w-sm mx-auto">
              This Telegram chat is already connected to a different SeraGo account. Only one account can be connected per bot.
            </p>
            <p className="mt-3 font-body-sm text-body-sm text-on-surface-variant/80 max-w-xs mx-auto">
              To link a different account, open @My_Serago_Bot in Telegram, send <code>/unlink</code> to disconnect your previous account, and try again.
            </p>

            <div className="mt-8 flex flex-col gap-3">
              <a
                href="https://t.me/My_Serago_Bot"
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-primary transition-opacity hover:opacity-90 shadow-sm"
              >
                <span className="material-symbols-outlined text-xl">send</span>
                Open Telegram Bot
              </a>
              <button
                type="button"
                onClick={handleSwitchAccount}
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-surface-variant px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-surface transition-colors hover:bg-surface-variant/30"
              >
                <span className="material-symbols-outlined text-xl">switch_account</span>
                Log In with a Different Account
              </button>
            </div>
          </div>
        </AuthShell>
      )
    }

    // Default: ACCOUNT_ALREADY_CONNECTED
    return (
      <AuthShell>
        <div className="text-center py-6">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-amber-500/10 text-amber-500">
            <span className="material-symbols-outlined text-4xl">warning</span>
          </div>
          <h1 className="font-headline-sm text-headline-sm font-bold text-on-surface">
            Account Already Has a Bot Connected
          </h1>
          <p className="mt-2 font-body-md text-body-md text-on-surface-variant max-w-sm mx-auto">
            The SeraGo account you signed in with already has a Telegram bot connected. Only <b>one bot per account</b> is allowed.
          </p>

          {/* Settings Card Preview */}
          <div className="my-6 rounded-xl border border-surface-variant bg-surface-variant/20 p-4 text-left">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-label-md text-label-md font-semibold text-on-surface flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-lg text-primary">smart_toy</span>
                  Telegram bot
                </p>
                <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant/70">
                  Connected — matched jobs can be sent to your Telegram chat.
                </p>
              </div>

              <span className="shrink-0 rounded-full bg-primary/10 px-2.5 py-1 text-label-xs font-semibold text-primary">
                Linked
              </span>
            </div>
          </div>

          <p className="font-body-sm text-body-sm text-on-surface-variant/80 max-w-sm mx-auto">
            If you wish to connect this new Telegram chat to this account, open your SeraGo Settings and click <b>Disconnect</b> under Telegram bot first.
          </p>

          <div className="mt-8 flex flex-col gap-3">
            <Link
              to="/dashboard/settings#telegram"
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-primary transition-opacity hover:opacity-90 shadow-sm"
            >
              <span className="material-symbols-outlined text-xl">settings</span>
              Manage Telegram in Settings
            </Link>

            <button
              type="button"
              onClick={handleSwitchAccount}
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-surface-variant px-6 py-3.5 font-label-lg text-label-lg font-semibold text-on-surface transition-colors hover:bg-surface-variant/30"
            >
              <span className="material-symbols-outlined text-xl">switch_account</span>
              Log In with a Different Account
            </button>

            <a
              href="https://t.me/My_Serago_Bot"
              className="inline-flex w-full items-center justify-center gap-1.5 pt-2 font-label-sm text-label-sm text-on-surface-variant hover:text-on-surface transition-colors"
            >
              <span className="material-symbols-outlined text-base">arrow_back</span>
              Return to Telegram Bot
            </a>
          </div>
        </div>
      </AuthShell>
    )
  }

  // ── Connecting State View ──
  if (state === 'connecting') {
    return (
      <AuthShell title="Connecting Telegram…" subtitle="Linking your chat to your SeraGo account">
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <div className="h-10 w-10 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          <p className="mt-4 font-body-md text-body-md text-on-surface-variant">
            Please wait while we establish your Telegram connection…
          </p>
        </div>
      </AuthShell>
    )
  }

  // ── Sign-in to Connect View ──
  return (
    <AuthShell
      title="Connect Telegram Alerts"
      subtitle="Sign in to your SeraGo account to finish connecting @My_Serago_Bot."
      footer={
        <p className="font-body-md text-body-md text-on-surface-variant text-center">
          Don&apos;t have an account?{' '}
          <Link
            to={`/signup?redirect=${encodeURIComponent(window.location.pathname + window.location.search)}`}
            className="font-medium text-primary underline-offset-4 hover:underline"
          >
            Create one free
          </Link>
        </p>
      }
    >
      <form onSubmit={formik.handleSubmit} noValidate className="space-y-4">
        {errorMessage && <ErrorBanner message={errorMessage} />}

        <TextInput
          id="email"
          name="email"
          type="email"
          label="Email address"
          icon="mail"
          placeholder="you@example.com"
          autoComplete="email"
          value={formik.values.email}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          error={formik.touched.email ? formik.errors.email : undefined}
        />

        <PasswordInput
          id="password"
          name="password"
          label="Password"
          placeholder="Enter your password"
          autoComplete="current-password"
          value={formik.values.password}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          error={formik.touched.password ? formik.errors.password : undefined}
        />

        <SubmitButton
          loading={formik.isSubmitting}
          loadingLabel="Connecting…"
          idleLabel="Sign In & Connect Telegram"
        />
      </form>
    </AuthShell>
  )
}
