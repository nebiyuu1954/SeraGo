import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useFormik } from 'formik'
import { object, string } from 'yup'
import { getApiErrorMessage, signIn, storeAuthTokens } from '../../api'
import AuthShell from '../../components/auth/AuthShell.tsx'
import ErrorBanner from '../../components/auth/ErrorBanner.tsx'
import TextInput from '../../components/auth/TextInput.tsx'
import PasswordInput from '../../components/auth/PasswordInput.tsx'
import SubmitButton from '../../components/auth/SubmitButton.tsx'
import Divider from '../../components/auth/Divider.tsx'
import GoogleButton from '../../components/auth/GoogleButton.tsx'

interface LoginValues {
  email: string
  password: string
}

const initialValues: LoginValues = {
  email: '',
  password: '',
}

const validationSchema = object<LoginValues>({
  email: string()
    .trim()
    .email('Please enter a valid email address.')
    .required('Please enter your email address.'),
  password: string().required('Please enter your password.'),
})

export default function LoginPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const formik = useFormik<LoginValues>({
    initialValues,
    validationSchema,
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      try {
        const tokens = await signIn({
          email: values.email,
          password: values.password,
        })
        storeAuthTokens(tokens)
        // /dashboard resolves the user's role and lands them on their page.
        navigate('/dashboard')
      } catch (err) {
        setError(getApiErrorMessage(err))
      }
    },
  })

  const fieldError = (name: keyof LoginValues) =>
    formik.touched[name] && formik.errors[name]
      ? formik.errors[name]
      : undefined

  return (
    <AuthShell
      title="Welcome back"
      subtitle="Log in to keep your job search moving."
      footer={
        <p className="font-label-sm text-label-sm text-on-surface-variant">
          Don&rsquo;t have an account?{' '}
          <Link
            to="/signup"
            className="font-medium text-primary transition-opacity hover:opacity-80"
          >
            Sign up
          </Link>
        </p>
      }
    >
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
          label="Password"
          autoComplete="current-password"
          placeholder="••••••••"
          value={formik.values.password}
          onChange={formik.handleChange}
          onBlur={formik.handleBlur}
          error={fieldError('password')}
        />

        <div className="-mt-2 flex justify-end">
          <Link
            to="/forgot-password"
            className="font-label-sm text-label-sm font-medium text-primary transition-opacity hover:opacity-80"
          >
            Forgot your password?
          </Link>
        </div>

        <SubmitButton
          loading={formik.isSubmitting}
          loadingLabel="Logging in…"
          idleLabel="Log in"
        />

        <Divider />

        <GoogleButton label="Log in with Google" />
      </form>
    </AuthShell>
  )
}
