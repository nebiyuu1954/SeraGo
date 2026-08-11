import { useEffect, useState } from 'react'
import { useFormik } from 'formik'
import { object, ref, string } from 'yup'
import {
  fetchProfile,
  getApiErrorMessage,
  getStoredAuthTokens,
  setPassword,
} from '../../api'
import { meetsPasswordRules } from '../auth/passwordRules.ts'
import PasswordInput from '../auth/PasswordInput.tsx'
import SubmitButton from '../auth/SubmitButton.tsx'

type CardState = 'loading' | 'hidden' | 'hasPassword' | 'needsPassword'

/**
 * Security card shown on the dashboards. Accounts created with Google have no
 * password, so this lets them create one — then they can sign in with email +
 * password on any device and use the regular forgot-password flow.
 */
export default function PasswordSetupCard() {
  // No stored tokens → never show the card (no synchronous setState in the
  // effect below, mirroring the useAuthUser pattern).
  const [state, setState] = useState<CardState>(() =>
    getStoredAuthTokens() ? 'loading' : 'hidden',
  )
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    fetchProfile(tokens.accessToken)
      .then((profile) =>
        setState(profile.hasPassword ? 'hasPassword' : 'needsPassword'),
      )
      .catch(() => {
        // Profile unreadable — don't show the card rather than mislead.
        setState('hidden')
      })
  }, [])

  const formik = useFormik({
    initialValues: { password: '', confirmPassword: '' },
    validationSchema: object({
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
    }),
    validateOnBlur: true,
    validateOnChange: true,
    onSubmit: async (values) => {
      setError(null)
      const tokens = getStoredAuthTokens()
      if (!tokens) {
        setError('You are not signed in.')
        return
      }
      try {
        await setPassword(tokens.accessToken, { password: values.password })
        setDone(true)
      } catch (err) {
        setError(getApiErrorMessage(err))
      }
    },
  })

  if (state === 'loading' || state === 'hidden') return null

  return (
    <section className="mt-8 rounded-xl border border-outline-variant bg-surface-container-lowest p-5 sm:p-6">
      {state === 'hasPassword' ? (
        <div className="flex items-start gap-3">
          <span
            aria-hidden="true"
            className="material-symbols-outlined mt-0.5 text-xl text-success"
          >
            check_circle
          </span>
          <div>
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">
              Password set
            </h2>
            <p className="mt-1 font-label-sm text-label-sm leading-relaxed text-on-surface-variant">
              You have a password, so you can sign in with your email and
              password on any device.
            </p>
          </div>
        </div>
      ) : done ? (
        <div className="flex items-start gap-3">
          <span
            aria-hidden="true"
            className="material-symbols-outlined mt-0.5 text-xl text-success"
          >
            check_circle
          </span>
          <div>
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">
              Password created
            </h2>
            <p className="mt-1 font-label-sm text-label-sm leading-relaxed text-on-surface-variant">
              You can now sign in with your email and password on any device.
            </p>
          </div>
        </div>
      ) : (
        <>
          <div className="flex items-start gap-3">
            <span
              aria-hidden="true"
              className="material-symbols-outlined mt-0.5 text-xl text-primary"
            >
              lock
            </span>
            <div>
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">
                Set a password
              </h2>
              <p className="mt-1 font-label-sm text-label-sm leading-relaxed text-on-surface-variant">
                You signed up with Google. Add a password so you can also log
                in with your email and password on any device.
              </p>
            </div>
          </div>

          {error && (
            <div
              role="alert"
              className="mt-4 flex items-start gap-2.5 rounded-xl border border-error/30 bg-error-container p-3.5 font-label-md text-label-md text-on-error-container"
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
            className="mt-5 space-y-5"
          >
            <PasswordInput
              id="set-password"
              name="password"
              label="New password"
              autoComplete="new-password"
              placeholder="••••••••"
              showStrength
              value={formik.values.password}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={
                formik.touched.password && formik.errors.password
                  ? formik.errors.password
                  : undefined
              }
            />
            <PasswordInput
              id="confirm-set-password"
              name="confirmPassword"
              label="Confirm password"
              autoComplete="new-password"
              placeholder="••••••••"
              value={formik.values.confirmPassword}
              onChange={formik.handleChange}
              onBlur={formik.handleBlur}
              error={
                formik.touched.confirmPassword && formik.errors.confirmPassword
                  ? formik.errors.confirmPassword
                  : undefined
              }
            />
            <SubmitButton
              loading={formik.isSubmitting}
              loadingLabel="Setting password…"
              idleLabel="Set password"
            />
          </form>
        </>
      )}
    </section>
  )
}
