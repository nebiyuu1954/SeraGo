import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useFormik } from 'formik'
import { mixed, object, ref, string } from 'yup'
import {
  ApiError,
  getApiErrorMessage,
  signIn,
  signUp,
  storeAuthTokens,
} from '../../../api'
import type { SignUpRole } from '../../../types'
import { cn } from '../../../lib/cn.ts'
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

const ROLE_OPTIONS: {
  value: SignUpRole
  title: string
  caption: string
  icon: string
}[] = [
  {
    value: 'Talent',
    title: 'I\u2019m a Talent',
    caption: 'Looking for my next role',
    icon: 'work',
  },
  {
    value: 'Recruiter',
    title: 'I\u2019m a Recruiter',
    caption: 'Hiring great people',
    icon: 'groups',
  },
]

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
  role: SignUpRole
}

const initialValues: SignUpValues = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  role: 'Talent',
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
  role: mixed<SignUpRole>()
    .oneOf(['Talent', 'Recruiter'], 'Please choose a role.')
    .required('Please choose a role.'),
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
          role: values.role,
        })

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
            {/* Role selector — required by the API */}
            <fieldset>
              <legend className="mb-1.5 block font-label-md text-label-md font-medium text-on-surface">
                I am joining as
              </legend>
              <div className="grid grid-cols-2 gap-3">
                {ROLE_OPTIONS.map((option) => {
                  const selected = formik.values.role === option.value
                  return (
                    <label
                      key={option.value}
                      className={cn(
                        'flex cursor-pointer items-center gap-3 rounded-xl border p-3.5 transition-all duration-200 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary/40',
                        selected
                          ? 'border-primary bg-primary-container/60 shadow-sm ring-2 ring-primary/30'
                          : 'border-outline-variant bg-surface-container-lowest hover:border-primary/50 hover:bg-surface-container-low',
                      )}
                    >
                      <input
                        type="radio"
                        name="role"
                        value={option.value}
                        checked={selected}
                        onChange={formik.handleChange}
                        className="sr-only"
                      />
                      <span
                        aria-hidden="true"
                        className={cn(
                          'material-symbols-outlined text-xl',
                          selected ? 'text-primary' : 'text-on-surface-variant',
                        )}
                      >
                        {option.icon}
                      </span>
                      <span className="min-w-0">
                        <span className="block truncate font-label-md text-label-md font-semibold text-on-surface">
                          {option.title}
                        </span>
                        <span className="block truncate font-label-sm text-label-sm text-on-surface-variant">
                          {option.caption}
                        </span>
                      </span>
                    </label>
                  )
                })}
              </div>
            </fieldset>

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
            <a
              href="#"
              className="font-medium text-primary transition-opacity hover:opacity-80"
            >
              Terms of Service
            </a>{' '}
            and{' '}
            <a
              href="#"
              className="font-medium text-primary transition-opacity hover:opacity-80"
            >
              Privacy Policy
            </a>
            .
          </p>
        </>
      )}
    </AuthShell>
  )
}
