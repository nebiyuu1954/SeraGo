import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { mixed, object, string } from 'yup'
import {
  ApiError,
  getApiErrorMessage,
  signInExternal,
  signUpExternal,
  storeAuthTokens,
} from '../../api'
import type { SignUpRole } from '../../types'
import { cn } from '../../lib/cn.ts'
import TextInput from '../../components/auth/TextInput.tsx'

type Stage = 'loading' | 'profile' | 'error' | 'done'

const ROLE_OPTIONS: { value: SignUpRole; title: string; caption: string }[] = [
  {
    value: 'Talent',
    title: 'I\u2019m a Talent',
    caption: 'Looking for my next role',
  },
  {
    value: 'Recruiter',
    title: 'I\u2019m a Recruiter',
    caption: 'Hiring great people',
  },
]

export default function GoogleCallbackPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const redirectTimerRef = useRef<number | null>(null)
  // Guards against a duplicate auto sign-in (React StrictMode double-mounts
  // effects in dev). Reset in the cleanup so the second mount still runs.
  const signInInFlightRef = useRef(false)

  const failed = searchParams.get('failed') === 'true'
  // Aufy appends ?signup=true when the OAuth identity has no SeraGo account yet.
  const needsSignup = searchParams.get('signup') === 'true'

  // Initial stage is derived from the URL (no state-to-state transition needed):
  // a failed handshake or a new-user redirect lands directly in its screen.
  const [stage, setStage] = useState<Stage>(() =>
    failed ? 'error' : needsSignup ? 'profile' : 'loading',
  )
  const [error, setError] = useState<string | null>(() =>
    failed ? 'Google sign-in did not complete. Please try again.' : null,
  )

  // Attempt the automatic Google sign-in on mount (only when we didn't land
  // directly in the error/profile screens above).
  useEffect(() => {
    if (stage !== 'loading' || signInInFlightRef.current) return
    signInInFlightRef.current = true

    let cancelled = false
    signInExternal()
      .then((tokens) => {
        if (cancelled) return
        storeAuthTokens(tokens)
        setStage('done')
        redirectTimerRef.current = window.setTimeout(
          () => navigate('/dashboard'),
          1200,
        )
      })
      .catch((err) => {
        if (cancelled) return
        // A brand-new Google user gets 400 "User not found" from /signin/external
        // (custom external signup flow) — fall through to the profile step.
        if (err instanceof ApiError && err.status === 400) {
          setStage('profile')
        } else {
          setError(getApiErrorMessage(err))
          setStage('error')
        }
      })
    return () => {
      cancelled = true
      signInInFlightRef.current = false
    }
  }, [stage, navigate])

  // Clear the redirect timer if the user navigates away mid-countdown.
  useEffect(() => {
    return () => {
      if (redirectTimerRef.current !== null) {
        window.clearTimeout(redirectTimerRef.current)
      }
    }
  }, [])

  const formik = useFormik({
    initialValues: {
      firstName: '',
      lastName: '',
      role: 'Talent' as SignUpRole,
    },
    validationSchema: object({
      firstName: string().trim().required('Please enter your first name.'),
      lastName: string().trim().required('Please enter your last name.'),
      role: mixed<SignUpRole>()
        .oneOf(['Talent', 'Recruiter'], 'Please choose a role.')
        .required('Please choose a role.'),
    }),
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      try {
        const tokens = await signUpExternal({
          firstName: values.firstName,
          lastName: values.lastName,
          role: values.role,
        })
        storeAuthTokens(tokens)
        setStage('done')
        redirectTimerRef.current = window.setTimeout(
          () => navigate('/dashboard'),
          1200,
        )
      } catch (err) {
        setError(getApiErrorMessage(err))
      }
    },
  })

  const fieldError = (name: 'firstName' | 'lastName') =>
    formik.touched[name] && formik.errors[name]
      ? formik.errors[name]
      : undefined

  return (
    <div className="flex min-h-svh items-center justify-center bg-surface px-4 py-10">
      <div className="mx-auto w-full max-w-md">
        <div className="rounded-2xl border border-outline-variant bg-surface-container-lowest p-8 shadow-sm sm:p-10">
          {/* Brand */}
          <div className="mb-6 text-center">
            <Link
              to="/"
              className="font-headline-md text-headline-md font-bold tracking-tight text-primary"
              aria-label="SeraGo home"
            >
              Sera<span className="text-accent">Go</span>
            </Link>
          </div>

          {stage === 'loading' && (
            <div
              className="flex flex-col items-center py-8 text-center"
              role="status"
            >
              <span className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
              <p className="mt-4 font-body-md text-body-md text-on-surface-variant">
                Finishing your Google sign-in…
              </p>
            </div>
          )}

          {stage === 'done' && (
            <div
              className="flex flex-col items-center py-8 text-center"
              role="status"
            >
              <span className="flex h-16 w-16 items-center justify-center rounded-full bg-success/15">
                <span className="material-symbols-outlined text-4xl text-success">
                  check_circle
                </span>
              </span>
              <h2 className="mt-5 font-headline-lg text-headline-lg font-semibold text-on-surface">
                You&rsquo;re signed in!
              </h2>
              <p className="mt-2 font-body-md text-body-md text-on-surface-variant">
                Taking you home…
              </p>
            </div>
          )}

          {stage === 'error' && (
            <div className="py-4 text-center">
              <span
                aria-hidden="true"
                className="material-symbols-outlined text-4xl text-error"
              >
                error
              </span>
              <h2 className="mt-4 font-headline-md text-headline-md font-semibold text-on-surface">
                Sign-in didn&rsquo;t complete
              </h2>
              <p className="mt-2 font-body-md text-body-md text-on-surface-variant">
                {error ?? 'Something went wrong during the Google sign-in.'}
              </p>
              <Link
                to="/signup"
                className="mt-6 inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent transition-opacity hover:opacity-90"
              >
                Back to sign up
              </Link>
            </div>
          )}

          {stage === 'profile' && (
            <div>
              <div className="mb-6 text-center">
                <span
                  aria-hidden="true"
                  className="material-symbols-outlined text-3xl text-primary"
                >
                  account_circle
                </span>
                <h2 className="mt-2 font-headline-md text-headline-md font-semibold text-on-surface">
                  One last step
                </h2>
                <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                  Your Google account is verified — tell us how you&rsquo;ll use
                  SeraGo.
                </p>
              </div>

              {error && (
                <div
                  role="alert"
                  className="mb-5 flex items-start gap-2.5 rounded-xl border border-error/30 bg-error-container p-3.5 font-label-md text-label-md text-on-error-container"
                >
                  <span
                    aria-hidden="true"
                    className="material-symbols-outlined mt-0.5 text-lg"
                  >
                    error
                  </span>
                  <span>{error}</span>
                </div>
              )}

              <form
                onSubmit={formik.handleSubmit}
                noValidate
                className="space-y-5"
              >
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
                            'flex cursor-pointer items-center gap-2.5 rounded-xl border p-3 transition-all duration-200 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary/40',
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

                <button
                  type="submit"
                  disabled={formik.isSubmitting}
                  className="flex w-full items-center justify-center gap-2 rounded-xl bg-accent py-3 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-all duration-200 hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-accent/40 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-70"
                >
                  {formik.isSubmitting && (
                    <span
                      aria-hidden="true"
                      className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/40 border-t-on-accent"
                    />
                  )}
                  {formik.isSubmitting
                    ? 'Creating your account…'
                    : 'Finish sign up'}
                </button>
              </form>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
