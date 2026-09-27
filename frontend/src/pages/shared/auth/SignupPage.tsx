import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, ref, string } from 'yup'
import {
  ApiError,
  getApiErrorMessage,
  signIn,
  signUp,
  storeAuthTokens,
} from '../../../api'
import AuthShell from '../../../components/auth/AuthShell.tsx'
import AccountExistsBanner from '../../../components/auth/AccountExistsBanner.tsx'
import ErrorBanner from '../../../components/auth/ErrorBanner.tsx'
import TextInput from '../../../components/auth/TextInput.tsx'
import PasswordInput from '../../../components/auth/PasswordInput.tsx'
import SubmitButton from '../../../components/auth/SubmitButton.tsx'
import Divider from '../../../components/auth/Divider.tsx'
import GoogleButton from '../../../components/auth/GoogleButton.tsx'
import AuthSuccess from '../../../components/auth/AuthSuccess.tsx'
import { meetsPasswordRules } from '../../../components/auth/passwordRules.ts'
import { trackEvent } from '../../../lib/analytics'

const FIELD_ORDER = [
  'firstName',
  'lastName',
  'email',
  'password',
  'confirmPassword',
] as const

/**
 * Aufy's SignUpEndpoint returns a 400 for duplicate emails; the envelope
 * middleware flattens the ProblemDetails into the Failed envelope's `message`.
 */
function isDuplicateEmailError(err: unknown): boolean {
  if (!(err instanceof ApiError) || err.status !== 400) return false
  return err.payload?.message === 'Account with this email already exists'
}

type SignUpValues = {
  firstName: string
  lastName: string
  email: string
  password: string
  confirmPassword: string
}

const initialValues: SignUpValues = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
}

/** Single source of truth for field validation — Formik runs this (Yup). */
const validationSchema = object<SignUpValues>({
  firstName: string().trim().required('Please enter your first name.'),
  lastName: string().trim().required('Please enter your last name.'),
  email: string()
    .trim()
    .email('Please enter a valid email address.')
    .required('Please enter your email address.'),
  password: string()
    .required('Please choose a password.')
    .test(
      'password-rules',
      'Password does not meet all requirements.',
      (value) => (value ? meetsPasswordRules(value) : false),
    ),
  confirmPassword: string()
    .required('Please confirm your password.')
    .oneOf([ref('password')], 'Passwords do not match.'),
})

export default function SignupPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const successTimerRef = useRef<number | null>(null)

  // Pre-filled when the login page detects no account for the typed email and
  // sends the user here to create one (state: { email }).
  const prefillEmail =
    (location.state as { email?: string } | null)?.email ?? ''

  const [error, setError] = useState<string | null>(null)
  const [accountExists, setAccountExists] = useState(false)
  const [success, setSuccess] = useState(false)
  // True when the backend requires email confirmation — the user must click
  // the emailed link before the account works, so we DON'T auto sign in.
  const [needsConfirmation, setNeedsConfirmation] = useState(false)

  const formik = useFormik<SignUpValues>({
    initialValues: { ...initialValues, email: prefillEmail },
    validationSchema,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      setAccountExists(false)
      try {
        const signUpResponse = await signUp({
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          password: values.password,
          role: 'Talent',
        })
        trackEvent('sign_up', { method: 'email' })

        if (signUpResponse.requiresEmailConfirmation) {
          // The account is created but inactive until the emailed link is
          // clicked — show "check your inbox", never auto sign in (Identity
          // would refuse it anyway).
          setNeedsConfirmation(true)
          setSuccess(true)
          return
        }

        // Confirmation not required — the account is usable immediately, so
        // sign the user straight in. Best-effort: the account is already
        // created, so even a transient sign-in failure still celebrates.
        try {
          const tokens = await signIn({
            email: values.email,
            password: values.password,
          })
          storeAuthTokens(tokens)
        } catch {
          // Account created but the auto sign-in failed — still celebrate success.
        }

        setSuccess(true)
        successTimerRef.current = window.setTimeout(
          () => navigate('/dashboard'),
          1600,
        )
      } catch (err) {
        if (isDuplicateEmailError(err)) {
          setAccountExists(true)
        } else {
          setError(getApiErrorMessage(err))
        }
      }
      // No manual setSubmitting(false) — Formik resets isSubmitting itself
      // when onSubmit settles.
    },
  })

  // After a failed submit, move focus to the first field with a validation error.
  useEffect(() => {
    if (formik.submitCount === 0) return
    const firstInvalid = FIELD_ORDER.find((name) => formik.errors[name])
    if (firstInvalid) document.getElementById(firstInvalid)?.focus()
    // Deliberately not depending on `formik.errors` — that would re-focus the
    // field on every keystroke while the user is fixing it after a failed submit.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formik.submitCount])

  // Clear the success-redirect timer if the user navigates away mid-countdown.
  useEffect(() => {
    return () => {
      if (successTimerRef.current !== null) {
        window.clearTimeout(successTimerRef.current)
      }
    }
  }, [])

  const fieldError = (name: (typeof FIELD_ORDER)[number]) =>
    formik.touched[name] && formik.errors[name]
      ? formik.errors[name]
      : undefined

  return (
    <AuthShell
      title={success ? undefined : 'Create an account'}
      subtitle={
        success ? undefined : 'Join SeraGo to manage your professional journey.'
      }
      footer={
        <p className="font-label-sm text-label-sm text-on-surface-variant">
          Already have an account?{' '}
          <Link
            to="/login"
            className="font-medium text-primary transition-opacity hover:opacity-80"
          >
            Sign in
          </Link>
        </p>
      }
    >
      {success ? (
        needsConfirmation ? (
          <AuthSuccess
            title="Check your inbox"
            message={`We sent a confirmation link to ${formik.values.email}. Click it to activate your account before signing in.`}
            action={
              <Link
                to="/confirm-email"
                className="inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-opacity hover:opacity-90"
              >
                Resend confirmation email
              </Link>
            }
          />
        ) : (
          <AuthSuccess
            title="Welcome to SeraGo!"
            message="Your account was created successfully. Taking you home…"
          />
        )
      ) : (
        <>
          {accountExists && <AccountExistsBanner email={formik.values.email} />}
          {error && <ErrorBanner message={error} />}

          <form onSubmit={formik.handleSubmit} noValidate className="space-y-5">
            {/* Name fields */}
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <TextInput
                id="firstName"
                name="firstName"
                label="First name"
                icon="person"
                autoComplete="given-name"
                placeholder="John"
                value={formik.values.firstName}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                error={fieldError('firstName')}
              />
              <TextInput
                id="lastName"
                name="lastName"
                label="Last name"
                icon="badge"
                autoComplete="family-name"
                placeholder="Doe"
                value={formik.values.lastName}
                onChange={formik.handleChange}
                onBlur={formik.handleBlur}
                error={fieldError('lastName')}
              />
            </div>

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
              error={fieldError('email')}
            />

            <PasswordInput
              id="password"
              name="password"
              label="Password"
              autoComplete="new-password"
              placeholder="••••••••"
              showStrength
              value={formik.values.password}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={fieldError('password')}
            />

            <PasswordInput
              id="confirmPassword"
              name="confirmPassword"
              label="Confirm password"
              autoComplete="new-password"
              placeholder="••••••••"
              value={formik.values.confirmPassword}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={fieldError('confirmPassword')}
            />

            <SubmitButton
              loading={formik.isSubmitting}
              loadingLabel="Creating your account…"
              idleLabel="Sign Up"
            />

            <Divider />

            <GoogleButton label="Sign up with Google" />
          </form>

          <p className="mt-5 text-center font-label-sm text-label-sm leading-relaxed text-on-surface-variant">
            By signing up, you agree to our{' '}
            <Link
              to="/terms"
              className="font-medium text-primary transition-opacity hover:opacity-80"
            >
              Terms of Service
            </Link>{' '}
            and{' '}
            <Link
              to="/privacy"
              className="font-medium text-primary transition-opacity hover:opacity-80"
            >
              Privacy Policy
            </Link>
            .
          </p>
        </>
      )}
    </AuthShell>
  )
}
