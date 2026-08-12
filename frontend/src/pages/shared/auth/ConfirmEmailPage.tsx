import { useEffect, useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { useFormik } from 'formik'
import type { FormikProps } from 'formik'
import { object, string } from 'yup'
import {
  ApiError,
  confirmEmail,
  getApiErrorMessage,
  resendConfirmationEmail,
} from '../../../api'
import AuthShell from '../../../components/auth/AuthShell.tsx'
import AuthSuccess from '../../../components/auth/AuthSuccess.tsx'
import ErrorBanner from '../../../components/auth/ErrorBanner.tsx'
import TextInput from '../../../components/auth/TextInput.tsx'
import SubmitButton from '../../../components/auth/SubmitButton.tsx'

type Stage = 'confirming' | 'success' | 'error' | 'resend'

export default function ConfirmEmailPage() {
  const location = useLocation()
  const [searchParams] = useSearchParams()
  // Pre-filled when the login page hits an unconfirmed account and sends the
  // user here to resend (state: { email }).
  const prefillEmail =
    (location.state as { email?: string } | null)?.email ?? ''
  const code = searchParams.get('code')
  const userId = searchParams.get('userId')
  // Set by the dashboard guard: the user is signed in but their account is
  // inactive until the email is confirmed.
  const required = searchParams.get('required') === '1'

  // With a code+userId in the URL the page is the email link destination and
  // confirms right away; without them (or on failure) it becomes the resend
  // form — also reachable directly from the login page.
  const [stage, setStage] = useState<Stage>(() =>
    code && userId ? 'confirming' : 'resend',
  )
  const [error, setError] = useState<string | null>(null)
  const [sentTo, setSentTo] = useState<string | null>(null)
  const [alreadyVerified, setAlreadyVerified] = useState(false)
  // Once the link is sent (or the account turns out to be verified) there's
  // nothing left to submit — swap the form for a status card, like the reset
  // password page does after a successful reset.
  const finished = sentTo !== null || alreadyVerified

  useEffect(() => {
    if (stage !== 'confirming' || !code || !userId) return
    let cancelled = false
    confirmEmail(code, userId)
      .then(() => {
        if (!cancelled) setStage('success')
      })
      .catch((err) => {
        if (cancelled) return
        // Aufy's confirm endpoint returns an EMPTY 404 for bad/expired codes
        // (no ProblemDetails body) — surface a friendly message instead of the
        // raw "Request failed with status 404".
        setError(
          err instanceof ApiError && err.status === 404
            ? 'This confirmation link is invalid or has expired.'
            : getApiErrorMessage(err),
        )
        setStage('error')
      })
    return () => {
      cancelled = true
    }
  }, [stage, code, userId])

  const formik = useFormik({
    initialValues: { email: prefillEmail },
    validationSchema: object({
      email: string()
        .trim()
        .email('Please enter a valid email address.')
        .required('Please enter your email address.'),
    }),
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      setSentTo(null)
      setAlreadyVerified(false)
      try {
        await resendConfirmationEmail(values.email)
        // 200 — the email was sent (account exists and is unconfirmed).
        setSentTo(values.email)
      } catch (err) {
        if (err instanceof ApiError && err.status === 409) {
          // Account exists but the email is already verified — nothing to send.
          setAlreadyVerified(true)
        } else {
          // 404 "No account found…" or any other failure.
          setError(getApiErrorMessage(err))
        }
      }
    },
  })

  return (
    <AuthShell
      title={
        stage === 'resend' && !finished
          ? required
            ? 'Confirm your email'
            : 'Resend confirmation email'
          : undefined
      }
      subtitle={
        stage === 'resend' && !finished
          ? required
            ? 'Your account is inactive until you verify your email address.'
            : 'We\'ll send a new verification link to your inbox.'
          : undefined
      }
      footer={
        <p className="font-label-sm text-label-sm text-on-surface-variant">
          <Link
            to="/login"
            className="font-medium text-primary transition-opacity hover:opacity-80"
          >
            Back to sign in
          </Link>
        </p>
      }
    >
      {stage === 'confirming' && (
        <div
          className="flex flex-col items-center rounded-2xl border border-outline-variant bg-surface-container-lowest p-10 text-center"
          role="status"
        >
          <span className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
          <p className="mt-4 font-body-md text-body-md text-on-surface-variant">
            Confirming your email…
          </p>
        </div>
      )}

      {stage === 'success' && (
        <AuthSuccess
          title="Email confirmed!"
          message="Your email address is verified. You can sign in now."
          action={
            <Link
              to="/login"
              className="inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-opacity hover:opacity-90"
            >
              Sign in
            </Link>
          }
        />
      )}

      {stage === 'error' && (
        <div className="space-y-5">
          <AuthSuccess
            tone="error"
            icon="link_off"
            title="Link is invalid or expired"
            message="Confirmation links are single-use and expire after a short time. Request a new one below."
          />
          <ResendForm
            formik={formik}
            error={error}
            sentTo={sentTo}
            alreadyVerified={alreadyVerified}
          />
        </div>
      )}

      {stage === 'resend' && (
        <div className="space-y-5">
          {required && (
            <div
              role="status"
              className="flex items-start gap-2.5 rounded-xl border border-primary/30 bg-primary-container/60 p-3.5 font-label-md text-label-md text-on-primary-container"
            >
              <span
                aria-hidden="true"
                className="material-symbols-outlined mt-0.5 text-lg"
              >
                mail
              </span>
              <span>
                We sent a confirmation link when you signed up. Enter your email
                to get a fresh one, then click it to activate your account.
              </span>
            </div>
          )}
          <ResendForm
            formik={formik}
            error={error}
            sentTo={sentTo}
            alreadyVerified={alreadyVerified}
          />
        </div>
      )}
    </AuthShell>
  )
}

function ResendForm({
  formik,
  error,
  sentTo,
  alreadyVerified,
}: {
  formik: FormikProps<{ email: string }>
  error: string | null
  sentTo: string | null
  alreadyVerified: boolean
}) {
  // Link sent — mirror the reset-password success state: the input is gone,
  // replaced by a status card.
  if (sentTo) {
    return (
      <AuthSuccess
        icon="mail"
        title="Check your inbox"
        message={`A new confirmation link is on its way to ${sentTo}. Click it to activate your account.`}
        action={
          <Link
            to="/login"
            className="inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-opacity hover:opacity-90"
          >
            Back to sign in
          </Link>
        }
      />
    )
  }

  // Account is already verified — nothing to send, no form to fill.
  if (alreadyVerified) {
    return (
      <AuthSuccess
        icon="verified"
        title="Email already verified"
        message="This email is already verified — you can sign in now."
        action={
          <Link
            to="/login"
            className="inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-opacity hover:opacity-90"
          >
            Go to sign in
          </Link>
        }
      />
    )
  }

  return (
    <div>
      {error && <ErrorBanner message={error} />}
      <form onSubmit={formik.handleSubmit} noValidate className="space-y-5">
        <TextInput
          id="email"
          name="email"
          label="Email address"
          icon="mail"
          type="email"
          autoComplete="email"
          placeholder="you@company.com"
          value={formik.values.email}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          error={formik.touched.email ? formik.errors.email : undefined}
        />
        <SubmitButton
          loading={formik.isSubmitting}
          loadingLabel="Sending…"
          idleLabel="Send confirmation email"
        />
      </form>
    </div>
  )
}
