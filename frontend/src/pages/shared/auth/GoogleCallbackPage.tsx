import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
  ApiError,
  fetchExternalInfo,
  getApiErrorMessage,
  signInExternal,
  signUpExternal,
  storeAuthTokens,
} from '../../../api'
import TextInput from '../../../components/auth/TextInput.tsx'
import EthiopianPhoneInput from '../../../components/auth/EthiopianPhoneInput.tsx'
import PasswordInput from '../../../components/auth/PasswordInput.tsx'
import { meetsPasswordRules } from '../../../components/auth/passwordRules.ts'
import { isValidEthiopianPhone, normalizeEthiopianPhone } from '../../../lib/phoneUtils.ts'
import { trackEvent } from '../../../lib/analytics'

type Stage = 'loading' | 'profile' | 'error' | 'done'

interface GoogleUserInfo {
  email: string
  firstName: string
  lastName: string
  avatarUrl?: string
}

export default function GoogleCallbackPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const redirectTimerRef = useRef<number | null>(null)
  // Guards against a duplicate auto sign-in (React StrictMode double-mounts
  // effects in dev). Reset in the cleanup so the second mount still runs.
  const signInInFlightRef = useRef(false)

  const failed = searchParams.get('failed') === 'true'
  // When OAuth identity has no SeraGo account yet.
  const needsSignup = searchParams.get('signup') === 'true'

  // Initial stage is derived from the URL:
  const [stage, setStage] = useState<Stage>(() =>
    failed ? 'error' : needsSignup ? 'profile' : 'loading',
  )
  const [error, setError] = useState<string | null>(() =>
    failed ? 'Google sign-in did not complete. Please try again.' : null,
  )
  const [googleUser, setGoogleUser] = useState<GoogleUserInfo | null>(null)

  // Fetch Google info if we landed directly on the signup screen
  useEffect(() => {
    if (stage === 'profile' && !googleUser) {
      fetchExternalInfo()
        .then((info) => setGoogleUser(info))
        .catch(() => {
          // Ignore if external cookie expired or not found
        })
    }
  }, [stage, googleUser])

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
        // An existing Google account was found — this is a login, not a signup.
        trackEvent('login', { method: 'google' })
        setStage('done')
        redirectTimerRef.current = window.setTimeout(
          () => navigate('/dashboard'),
          1200,
        )
      })
      .catch((err) => {
        if (cancelled) return
        // A brand-new Google user gets 400 "User not found" from /signin/external
        if (err instanceof ApiError && err.status === 400) {
          const payload = err.payload as
            | {
                email?: string
                firstName?: string
                lastName?: string
                avatarUrl?: string
              }
            | undefined
          if (payload?.email) {
            setGoogleUser({
              email: payload.email,
              firstName: payload.firstName ?? '',
              lastName: payload.lastName ?? '',
              avatarUrl: payload.avatarUrl,
            })
          } else {
            fetchExternalInfo()
              .then((info) => setGoogleUser(info))
              .catch(() => {})
          }
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
      middleName: '',
      lastName: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
    },
    validationSchema: object({
      phoneNumber: string()
        .trim()
        .required('Please enter your phone number.')
        .test(
          'ethiopian-phone',
          'Please enter a valid Ethiopian mobile number (e.g. 0910101010 or 910101010).',
          (value) => (value ? isValidEthiopianPhone(value) : false),
        ),
      password: string()
        .required('Please choose a password.')
        .test(
          'password-rules',
          'Password does not meet all requirements.',
          (value) => (value ? meetsPasswordRules(value) : false),
        ),
      confirmPassword: string()
        .required('Please confirm your password.')
        .test(
          'passwords-match',
          'Passwords do not match.',
          (value, ctx) => value === ctx.parent.password,
        ),
    }),
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      try {
        const normalizedPhone =
          normalizeEthiopianPhone(values.phoneNumber) ?? values.phoneNumber
        const tokens = await signUpExternal({
          role: 'Talent',
          firstName: values.firstName.trim() || (googleUser?.firstName ?? ''),
          middleName: values.middleName.trim(),
          lastName: values.lastName.trim() || (googleUser?.lastName ?? ''),
          phoneNumber: normalizedPhone,
          password: values.password,
        })
        storeAuthTokens(tokens)
        trackEvent('sign_up', { method: 'google' })
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

  // Prepopulate name fields when google user info is loaded
  useEffect(() => {
    if (googleUser) {
      if (!formik.values.firstName && googleUser.firstName) {
        formik.setFieldValue('firstName', googleUser.firstName)
      }
      if (!formik.values.lastName && googleUser.lastName) {
        formik.setFieldValue('lastName', googleUser.lastName)
      }
    }
  }, [googleUser])

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
              <span
                aria-hidden="true"
                className="material-symbols-outlined text-4xl text-primary"
              >
                check_circle
              </span>
              <p className="mt-4 font-body-md text-body-md font-medium text-on-surface">
                Signed in! Taking you home…
              </p>
            </div>
          )}

          {stage === 'error' && (
            <div className="text-center">
              <span
                aria-hidden="true"
                className="material-symbols-outlined text-4xl text-error"
              >
                error
              </span>
              <h2 className="mt-3 font-headline-md text-headline-md font-semibold text-on-surface">
                Sign-in failed
              </h2>
              <p className="mt-2 font-body-md text-body-md text-on-surface-variant">
                {error ?? 'Something went wrong during the Google sign-in.'}
              </p>
              <div className="mt-6 flex flex-col gap-3">
                <Link
                  to="/login"
                  className="rounded-xl bg-accent py-3 text-center font-label-md text-label-md font-semibold text-on-accent transition-opacity hover:opacity-90"
                >
                  Back to login
                </Link>
                <Link
                  to="/signup"
                  className="rounded-xl border border-outline-variant py-3 text-center font-label-md text-label-md font-semibold text-on-surface transition-colors hover:bg-surface-container"
                >
                  Create account with email
                </Link>
              </div>
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
                  Complete your talent profile
                </h2>
                <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                  Add your phone number and password to complete signing up with Google.
                </p>
              </div>

              {googleUser?.email && (
                <div className="mb-5 flex items-center gap-3 rounded-xl border border-outline-variant/60 bg-surface-container-low p-3">
                  {googleUser.avatarUrl ? (
                    <img
                      src={googleUser.avatarUrl}
                      alt=""
                      className="h-10 w-10 rounded-full object-cover"
                    />
                  ) : (
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                      <span className="material-symbols-outlined text-xl">
                        verified_user
                      </span>
                    </div>
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-label-md text-label-md font-semibold text-on-surface">
                      {googleUser.email}
                    </p>
                    <p className="font-label-sm text-label-sm text-primary">
                      ✓ Verified Google Account
                    </p>
                  </div>
                </div>
              )}

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
                className="space-y-4"
              >
                {/* Names */}
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                  <TextInput
                    id="firstName"
                    name="firstName"
                    label="First name"
                    icon="person"
                    placeholder="Abebe"
                    value={formik.values.firstName}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                  />
                  <TextInput
                    id="middleName"
                    name="middleName"
                    label="Middle name"
                    icon="person"
                    placeholder="Kebede"
                    value={formik.values.middleName}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                  />
                  <TextInput
                    id="lastName"
                    name="lastName"
                    label="Last name"
                    icon="badge"
                    placeholder="Balcha"
                    value={formik.values.lastName}
                    onChange={formik.handleChange}
                    onBlur={formik.handleBlur}
                  />
                </div>

                {/* Ethiopian Phone */}
                <EthiopianPhoneInput
                  id="phoneNumber"
                  name="phoneNumber"
                  label="Phone number"
                  value={formik.values.phoneNumber}
                  onChange={formik.handleChange}
                  onBlur={formik.handleBlur}
                  error={
                    formik.touched.phoneNumber
                      ? formik.errors.phoneNumber
                      : undefined
                  }
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
                  error={
                    formik.touched.password ? formik.errors.password : undefined
                  }
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
                  error={
                    formik.touched.confirmPassword
                      ? formik.errors.confirmPassword
                      : undefined
                  }
                />

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
