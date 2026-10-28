import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { JobResponse } from '../../types'
import GoogleButton from '../auth/GoogleButton.tsx'
import TelegramButton from '../auth/TelegramButton.tsx'
import { signIn, storeAuthTokens, ApiError } from '../../api'
import { trackEvent } from '../../lib/analytics.ts'

interface GuestJobAuthModalProps {
  isOpen: boolean
  onClose: () => void
  job: JobResponse | null
  mode?: 'apply' | 'preview'
}

export default function GuestJobAuthModal({
  isOpen,
  onClose,
  job,
}: GuestJobAuthModalProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isOpen) {
      setEmail('')
      setPassword('')
      setError(null)
      setLoading(false)
      return
    }
    if (job) {
      try {
        sessionStorage.setItem('serago_pending_target_job_id', job.id)
        sessionStorage.setItem('serago_pending_target_job_title', job.title)
        if (job.sectorId) {
          sessionStorage.setItem('serago_pending_target_job_sector_id', job.sectorId)
        }
        if (job.sectorName) {
          sessionStorage.setItem('serago_pending_target_job_sector_name', job.sectorName)
        }
        localStorage.setItem('serago_pending_target_job_id', job.id)
        localStorage.setItem('serago_pending_target_job_title', job.title)
        if (job.sectorId) {
          localStorage.setItem('serago_pending_target_job_sector_id', job.sectorId)
        }
        if (job.sectorName) {
          localStorage.setItem('serago_pending_target_job_sector_name', job.sectorName)
        }
      } catch {}
    }
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, job, onClose])

  if (!isOpen || !job) return null

  const targetPath = `/dashboard/talent/jobs/${job.id}`
  const redirectParam = encodeURIComponent(targetPath)

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !password) {
      setError('Please enter both your email and password.')
      return
    }

    setLoading(true)
    setError(null)

    try {
      const tokens = await signIn({
        email: email.trim(),
        password,
      })
      storeAuthTokens(tokens)
      trackEvent('login', { method: 'email', source: 'guest_job_modal' })
      onClose()
      window.location.href = targetPath
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404) {
          setError('No account found with this email. Please sign up first.')
        } else if (err.status === 403) {
          setError('Please confirm your email address before logging in.')
        } else if (err.status === 401 || err.status === 400) {
          setError('Incorrect email or password. Please try again.')
        } else {
          setError(err.message || 'Unable to log in right now. Please try again.')
        }
      } else {
        setError('Something went wrong. Please check your connection.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-scrim/60 backdrop-blur-sm transition-opacity"
        onClick={onClose}
      />

      {/* Modal Dialog */}
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl transition-all">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-surface-variant/50 px-6 py-5">
          <h3 className="font-headline-md text-headline-md font-bold text-on-surface">
            Sign in to SeraGo
          </h3>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-secondary transition-colors hover:bg-surface-container-high hover:text-on-surface"
            aria-label="Close modal"
          >
            <span className="material-symbols-outlined text-[20px]">close</span>
          </button>
        </div>

        {/* Body */}
        <div className="flex flex-col gap-4 p-6">
          <p className="font-body-sm text-body-sm text-secondary">
            Sign in to your account to unlock all matching jobs, apply directly, and save vacancies.
          </p>

          {/* Interactive Login Form */}
          <form onSubmit={handleLoginSubmit} className="flex flex-col gap-3.5">
            {error && (
              <div
                role="alert"
                className="rounded-lg border border-error/30 bg-error-container p-3 font-body-sm text-body-sm text-on-error-container"
              >
                {error}
              </div>
            )}

            <div>
              <label className="block font-label-sm text-label-sm font-medium text-on-surface">
                Email address
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/50 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div>
              <div className="flex items-center justify-between">
                <label className="block font-label-sm text-label-sm font-medium text-on-surface">
                  Password
                </label>
                <Link
                  to="/forgot-password"
                  className="font-label-sm text-label-sm text-secondary hover:text-primary hover:underline"
                >
                  Forgot?
                </Link>
              </div>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/50 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="mt-1 flex w-full items-center justify-center gap-2 rounded-lg bg-accent px-4 py-2.5 font-label-md text-label-md font-semibold text-on-accent transition-colors hover:opacity-90 disabled:opacity-50 cursor-pointer"
            >
              {loading ? (
                <>
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent border-t-transparent" />
                  <span>Signing in…</span>
                </>
              ) : (
                <span>Sign In & Continue</span>
              )}
            </button>

            {/* Social Sign-In: 1 Row, 2 Columns (Signs Only) */}
            <div className="relative my-1 text-center font-label-sm text-label-sm text-outline">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-surface-variant" />
              </div>
              <span className="relative bg-surface-container-lowest px-3">
                or continue with
              </span>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <GoogleButton iconOnly />
              <TelegramButton iconOnly />
            </div>

            {/* 1 Row Text and Button for Signup */}
            <div className="mt-1 flex items-center justify-between rounded-xl border border-surface-variant bg-surface-container-low/70 px-4 py-2.5">
              <span className="font-body-sm text-body-sm text-secondary">
                Don't have an account?
              </span>
              <Link
                to={`/signup?redirect=${redirectParam}`}
                onClick={onClose}
                className="inline-flex items-center justify-center rounded-lg border border-primary/30 bg-surface-container-lowest px-3.5 py-1.5 font-label-sm text-label-sm font-semibold text-primary shadow-xs transition-all hover:border-primary hover:bg-primary hover:text-white active:scale-95 cursor-pointer"
              >
                Sign up
              </Link>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
