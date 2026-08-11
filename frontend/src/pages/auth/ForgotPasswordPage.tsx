import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import { forgotPassword, getApiErrorMessage } from '../../api'
import AuthShell from '../../components/auth/AuthShell.tsx'
import ErrorBanner from '../../components/auth/ErrorBanner.tsx'
import TextInput from '../../components/auth/TextInput.tsx'
import SubmitButton from '../../components/auth/SubmitButton.tsx'
import AuthSuccess from '../../components/auth/AuthSuccess.tsx'

interface ForgotValues {
  email: string
}

const validationSchema = object<ForgotValues>({
  email: string()
    .trim()
    .email('Please enter a valid email address.')
    .required('Please enter your email address.'),
})

export default function ForgotPasswordPage() {
  const [error, setError] = useState<string | null>(null)
  // The endpoint always returns 200 (no account enumeration) — the success
  // state is shown for any submitted email.
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null)

  const formik = useFormik<ForgotValues>({
    initialValues: { email: '' },
    validationSchema,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      try {
        await forgotPassword({ email: values.email })
        setSubmittedEmail(values.email)
      } catch (err) {
        setError(getApiErrorMessage(err))
      }
    },
  })

  const fieldError = (name: keyof ForgotValues) =>
    formik.touched[name] && formik.errors[name]
      ? formik.errors[name]
      : undefined

  return (
    <AuthShell
      title={submittedEmail ? undefined : 'Reset your password'}
      subtitle={
        submittedEmail
          ? undefined
          : 'Enter your email and we\u2019ll send you a secure reset link.'
      }
      footer={
        <p className="font-label-sm text-label-sm text-on-surface-variant">
          Remembered your password?{' '}
          <Link
            to="/login"
            className="font-medium text-primary transition-opacity hover:opacity-80"
          >
            Log in
          </Link>
        </p>
      }
    >
      {submittedEmail ? (
        <AuthSuccess
          title="Check your inbox"
          message={`A password reset link is on its way to ${submittedEmail}. The link expires after a short time.`}
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
            <SubmitButton
              loading={formik.isSubmitting}
              loadingLabel="Sending link…"
              idleLabel="Send reset link"
            />
          </form>
        </>
      )}
    </AuthShell>
  )
}
