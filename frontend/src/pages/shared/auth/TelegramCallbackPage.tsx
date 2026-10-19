import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import {
  getApiErrorMessage,
  signUpTelegram,
  storeAuthTokens,
  type TelegramAuthPayload,
} from '../../../api'
import TextInput from '../../../components/auth/TextInput.tsx'
import EthiopianPhoneInput from '../../../components/auth/EthiopianPhoneInput.tsx'
import PasswordInput from '../../../components/auth/PasswordInput.tsx'
import { meetsPasswordRules } from '../../../components/auth/passwordRules.ts'
import { isValidEthiopianPhone, normalizeEthiopianPhone } from '../../../lib/phoneUtils.ts'
import { trackEvent } from '../../../lib/analytics'

interface LocationState {
  signupToken?: string
  telegramUser?: {
    id: number
    firstName: string
    lastName: string
    username: string
    photoUrl: string
  }
  rawAuth?: TelegramAuthPayload
}

export default function TelegramCallbackPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const state = (location.state as LocationState) ?? {}

  const [error, setError] = useState<string | null>(null)
  const [role, setRole] = useState<'Talent' | 'Recruiter'>('Talent')
  const redirectTimerRef = useRef<number | null>(null)

  const telegramUser = state.telegramUser
  const signupToken = state.signupToken
  const rawAuth = state.rawAuth

  // If someone lands here directly without state from TelegramButton, bounce them back to login
  useEffect(() => {
    if (!telegramUser && !signupToken && !rawAuth) {
      navigate('/login', { replace: true })
    }
  }, [telegramUser, signupToken, rawAuth, navigate])

  useEffect(() => {
    return () => {
      if (redirectTimerRef.current !== null) {
        window.clearTimeout(redirectTimerRef.current)
      }
    }
  }, [])

  const formik = useFormik({
    initialValues: {
      firstName: telegramUser?.firstName ?? '',
      middleName: '',
      lastName: telegramUser?.lastName ?? '',
      email: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
    },
    validationSchema: object({
      firstName: string().trim().required('First name is required.'),
      lastName: string().trim().required('Last name is required.'),
      email: string().trim().email('Please enter a valid email address.').required('Email is required.'),
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
        .test('passwords-match', 'Passwords must match.', function (value) {
          return this.parent.password === value
        }),
    }),
    onSubmit: async (values, { setSubmitting }) => {
      setError(null)
      try {
        const normalized = normalizeEthiopianPhone(values.phoneNumber)
        if (!normalized) {
          setError('Please enter a valid Ethiopian mobile number.')
          setSubmitting(false)
          return
        }

        const tokens = await signUpTelegram({
          signupToken,
          authPayload: rawAuth,
          email: values.email.trim().toLowerCase(),
          phoneNumber: normalized,
          password: values.password,
          firstName: values.firstName.trim(),
          middleName: values.middleName.trim(),
          lastName: values.lastName.trim(),
          role,
        })

        storeAuthTokens(tokens)
        trackEvent('sign_up', { method: 'telegram', role })

        redirectTimerRef.current = window.setTimeout(() => {
          navigate('/dashboard')
        }, 500)
      } catch (err) {
        setError(getApiErrorMessage(err))
      } finally {
        setSubmitting(false)
      }
    },
  })

  return (
    <main className="flex min-h-screen items-center justify-center bg-surface px-4 py-12">
      <div className="w-full max-w-md rounded-2xl border border-outline-variant bg-surface-container-lowest p-8 shadow-sm">
        {/* Telegram User Header */}
        <div className="mb-6 flex items-center gap-3 border-b border-outline-variant/60 pb-5">
          {telegramUser?.photoUrl ? (
            <img
              src={telegramUser.photoUrl}
              alt={telegramUser.firstName}
              className="h-12 w-12 rounded-full border border-outline-variant object-cover"
            />
          ) : (
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-[#229ED9]/10 text-xl font-bold text-[#229ED9]">
              {(telegramUser?.firstName?.[0] ?? 'T').toUpperCase()}
            </div>
          )}
          <div className="min-w-0 flex-1">
            <h1 className="font-headline-sm text-title-md font-bold text-on-surface truncate">
              Welcome, {telegramUser?.firstName ?? 'there'}!
            </h1>
            <p className="font-label-sm text-label-sm text-on-surface-variant truncate">
              {telegramUser?.username ? `@${telegramUser.username}` : 'Complete your SeraGo account'}
            </p>
          </div>
        </div>

        {error && (
          <div className="mb-4 rounded-xl bg-error-container/40 p-3 text-label-sm text-on-error-container">
            {error}
          </div>
        )}

        {/* Role Toggle */}
        <div className="mb-5">
          <label className="mb-1.5 block font-label-md text-label-md font-semibold text-on-surface">
            I want to join as:
          </label>
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => setRole('Talent')}
              className={`rounded-xl border py-2.5 text-center font-label-md text-label-md font-medium transition-colors ${
                role === 'Talent'
                  ? 'border-primary bg-primary/10 text-primary'
                  : 'border-outline-variant bg-surface text-on-surface-variant hover:bg-surface-variant/40'
              }`}
            >
              Job Seeker
            </button>
            <button
              type="button"
              onClick={() => setRole('Recruiter')}
              className={`rounded-xl border py-2.5 text-center font-label-md text-label-md font-medium transition-colors ${
                role === 'Recruiter'
                  ? 'border-primary bg-primary/10 text-primary'
                  : 'border-outline-variant bg-surface text-on-surface-variant hover:bg-surface-variant/40'
              }`}
            >
              Employer
            </button>
          </div>
        </div>

        <form onSubmit={formik.handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <TextInput
              id="firstName"
              label="First Name"
              name="firstName"
              icon="person"
              value={formik.values.firstName}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={formik.touched.firstName ? formik.errors.firstName : undefined}
            />
            <TextInput
              id="lastName"
              label="Last Name"
              name="lastName"
              icon="badge"
              value={formik.values.lastName}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={formik.touched.lastName ? formik.errors.lastName : undefined}
            />
          </div>

          <TextInput
            id="middleName"
            label="Middle Name (Optional)"
            name="middleName"
            icon="person"
            value={formik.values.middleName}
            onChange={formik.handleChange}
            onBlur={formik.handleBlur}
          />

          <TextInput
            id="email"
            label="Email Address"
            name="email"
            icon="mail"
            type="email"
            placeholder="you@example.com"
            value={formik.values.email}
            onChange={formik.handleChange}
            onBlur={formik.handleBlur}
            error={formik.touched.email ? formik.errors.email : undefined}
          />

          <EthiopianPhoneInput
            id="phoneNumber"
            name="phoneNumber"
            label="Phone number"
            value={formik.values.phoneNumber}
            onChange={formik.handleChange}
            onBlur={formik.handleBlur}
            error={formik.touched.phoneNumber ? formik.errors.phoneNumber : undefined}
          />

          <PasswordInput
            id="password"
            name="password"
            autoComplete="new-password"
            label="Choose a Password"
            value={formik.values.password}
            onChange={formik.handleChange}
            onBlur={formik.handleBlur}
            error={formik.touched.password ? formik.errors.password : undefined}
          />

          <PasswordInput
            id="confirmPassword"
            name="confirmPassword"
            autoComplete="new-password"
            label="Confirm Password"
            value={formik.values.confirmPassword}
            onChange={formik.handleChange}
            onBlur={formik.handleBlur}
            error={formik.touched.confirmPassword ? formik.errors.confirmPassword : undefined}
          />

          <button
            type="submit"
            disabled={formik.isSubmitting}
            className="w-full rounded-xl bg-primary py-3 font-label-md text-label-md font-semibold text-on-primary shadow-sm transition-opacity hover:opacity-90 disabled:opacity-50"
          >
            {formik.isSubmitting ? 'Creating account…' : 'Complete Registration'}
          </button>
        </form>

        <p className="mt-5 text-center font-label-sm text-label-sm text-on-surface-variant">
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-primary hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </main>
  )
}
