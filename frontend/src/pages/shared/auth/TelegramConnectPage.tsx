import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
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

export default function TelegramConnectPage() {
  const [searchParams] = useSearchParams()
  const chatIdParam = searchParams.get('chat_id')
  const token = searchParams.get('token')

  const chatId = chatIdParam ? parseInt(chatIdParam, 10) : null

  const [state, setState] = useState<'idle' | 'connecting' | 'connected' | 'error'>(() => {
    if (!chatId || !token) return 'error'
    const stored = getStoredAuthTokens()
    return stored ? 'connecting' : 'idle'
  })
  const [errorMessage, setErrorMessage] = useState<string | null>(() => {
    if (!chatId || !token) {
      return 'Missing or invalid Telegram connection link. Please request a new link from @My_Serago_Bot.'
    }
    return null
  })

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
          setState('error')
          setErrorMessage(getApiErrorMessage(err))
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
        setState('idle')
        setErrorMessage(getApiErrorMessage(err))
      }
    },
  })

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
