import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, ref, string } from 'yup'
import { getApiErrorMessage, resetPassword } from '../../api'
import AuthShell from '../../components/auth/AuthShell.tsx'
import AuthSuccess from '../../components/auth/AuthSuccess.tsx'
import ErrorBanner from '../../components/auth/ErrorBanner.tsx'
import TextInput from '../../components/auth/TextInput.tsx'
import PasswordInput from '../../components/auth/PasswordInput.tsx'
import SubmitButton from '../../components/auth/SubmitButton.tsx'
import { meetsPasswordRules } from '../../components/auth/passwordRules.ts'

interface ResetValues {
  email: string
  password: string
  confirmPassword: string
}

const initialValues: ResetValues = {
  email: '',
  password: '',
  confirmPassword: '',
}

const validationSchema = object<ResetValues>({
  email: string()
    .trim()
    .email('Please enter a valid email address.')
    .required('Please enter your email address.'),
  password: string()
    .required('Please choose a new password.')
    .test(
      'password-rules',
      'Password does not meet all requirements.',
      (value) => (value ? meetsPasswordRules(value) : false),
    ),
  confirmPassword: string()
    .required('Please confirm your password.')
    .oneOf([ref('password')], 'Passwords do not match.'),
})

const footer = (
  <p className="font-label-sm text-label-sm text-on-surface-variant">
    Remembered your password?{' '}
    <Link
      to="/login"
      className="font-medium text-primary transition-opacity hover:opacity-80"
    >
      Log in
    </Link>
  </p>
)

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  // The emailed link carries only the token: /reset-password?code=…
  const code = searchParams.get('code') ?? ''

  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  const formik = useFormik<ResetValues>({
    initialValues,
    validationSchema,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      try {
        await resetPassword({
          code,
          email: values.email,
          password: values.password,
        })
        setDone(true)
      } catch (err) {
        setError(getApiErrorMessage(err))
      }
    },
  })

  const fieldError = (name: keyof ResetValues) =>
    formik.touched[name] && formik.errors[name]
      ? formik.errors[name]
      : undefined

  // No token in the URL — the link is stale or was opened wrong.
  if (!code) {
    return (
      <AuthShell footer={footer}>
        <AuthSuccess
          tone="error"
          icon="link_off"
          title="This link is invalid or expired"
          message="Password reset links are single-use and expire quickly. Request a fresh one and try again."
          action={
            <Link
              to="/forgot-password"
              className="inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent transition-opacity hover:opacity-90"
            >
              Request a new link
            </Link>
          }
        />
      </AuthShell>
    )
  }

  return (
    <AuthShell
      title={done ? undefined : 'Set a new password'}
      subtitle={
        done
          ? undefined
          : 'Your new password must meet the same requirements as your old one.'
      }
      footer={footer}
    >
      {done ? (
        <AuthSuccess
          title="Password updated"
          message="Your password has been changed. You can now log in with your new password."
          action={
            <Link
              to="/login"
              className="inline-block rounded-xl bg-accent px-6 py-3 font-label-md text-label-md font-semibold text-on-accent transition-opacity hover:opacity-90"
            >
              Log in
            </Link>
          }
        />
      ) : (
        <>
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
              error={fieldError('email')}
            />

            <PasswordInput
              id="password"
              name="password"
              label="New password"
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
              label="Confirm new password"
              autoComplete="new-password"
              placeholder="••••••••"
              value={formik.values.confirmPassword}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={fieldError('confirmPassword')}
            />

            <SubmitButton
              loading={formik.isSubmitting}
              loadingLabel="Updating password…"
              idleLabel="Update password"
            />
          </form>
        </>
      )}
    </AuthShell>
  )
}
