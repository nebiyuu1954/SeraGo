import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import type { JobResponse } from '../../types'
import GoogleButton from '../auth/GoogleButton.tsx'
import TelegramButton from '../auth/TelegramButton.tsx'

interface GuestJobAuthModalProps {
  isOpen: boolean
  onClose: () => void
  job: JobResponse | null
  mode: 'apply' | 'preview'
}

export default function GuestJobAuthModal({
  isOpen,
  onClose,
  job,
  mode,
}: GuestJobAuthModalProps) {
  useEffect(() => {
    if (!isOpen) return
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  if (!isOpen || !job) return null

  const targetPath = `/dashboard/talent/jobs/${job.id}`
  const redirectParam = encodeURIComponent(targetPath)

  // Strip html for clean preview snippet
  const snippet = job.description
    ? job.description
        .replace(/<[^>]*>/g, ' ')
        .replace(/\s+/g, ' ')
        .trim()
        .slice(0, 200)
    : ''

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
      <div className="relative z-10 w-full max-w-lg overflow-hidden rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl transition-all">
        {/* Header */}
        <div className="border-b border-surface-variant/50 p-6">
          <div className="flex items-start justify-between gap-4">
            <div className="flex-1">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-primary/10 px-2.5 py-1 font-label-sm text-label-sm font-semibold text-primary">
                <span className="material-symbols-outlined text-[16px]">
                  {mode === 'preview' ? 'visibility' : 'rocket_launch'}
                </span>
                {mode === 'preview' ? 'Job Preview' : 'Apply for Job'}
              </span>
              <h3 className="mt-2.5 font-headline-md text-headline-md text-on-surface">
                {job.title}
              </h3>
              <p className="font-body-md text-body-md text-secondary">
                {job.company || 'Confidential'} · {job.location || 'Ethiopia'}
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg p-1.5 text-secondary transition-colors hover:bg-surface-container-high hover:text-on-surface"
              aria-label="Close modal"
            >
              <span className="material-symbols-outlined text-[20px]">close</span>
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="flex flex-col gap-5 p-6">
          {mode === 'preview' ? (
            <div className="flex flex-col gap-3 rounded-xl border border-surface-variant/60 bg-surface/50 p-4">
              <div className="flex flex-wrap gap-2 font-label-sm text-label-sm text-secondary">
                {job.sectorName && (
                  <span className="rounded bg-surface-container-low px-2 py-0.5 text-on-surface-variant">
                    {job.sectorName}
                  </span>
                )}
                {job.jobType && (
                  <span className="rounded bg-surface-container-low px-2 py-0.5 text-on-surface-variant">
                    {job.jobType}
                  </span>
                )}
                {job.workMode && (
                  <span className="rounded bg-surface-container-low px-2 py-0.5 text-on-surface-variant">
                    {job.workMode}
                  </span>
                )}
              </div>
              <p className="font-body-sm text-body-sm leading-relaxed text-secondary">
                {snippet ? `${snippet}…` : 'Explore the full requirements and details.'}
              </p>
              <div className="mt-1 flex items-center gap-2 font-label-sm text-label-sm font-medium text-primary">
                <span className="material-symbols-outlined text-[18px]">lock</span>
                <span>Sign in or create a free account to read full details & apply</span>
              </div>
            </div>
          ) : (
            <div className="flex items-start gap-3 rounded-xl border border-primary/20 bg-primary/5 p-4 font-body-sm text-body-sm text-on-surface-variant">
              <span className="material-symbols-outlined mt-0.5 text-primary text-[20px]">
                how_to_reg
              </span>
              <p>
                Create a free account in 30 seconds or sign in to submit your 1-click application on SeraGo.
              </p>
            </div>
          )}

          {/* Social login buttons */}
          <div className="flex flex-col gap-3">
            <GoogleButton label="Continue with Google" />
            <TelegramButton label="Continue with Telegram" />
          </div>

          <div className="relative my-1 text-center font-label-sm text-label-sm text-outline">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-surface-variant" />
            </div>
            <span className="relative bg-surface-container-lowest px-4">
              or continue with email
            </span>
          </div>

          {/* Email buttons */}
          <div className="grid grid-cols-2 gap-3">
            <Link
              to={`/signup?redirect=${redirectParam}`}
              className="flex items-center justify-center rounded-lg bg-accent px-4 py-2.5 font-label-md text-label-md font-semibold text-on-accent transition-colors hover:opacity-90"
            >
              Sign Up Free
            </Link>
            <Link
              to={`/login?redirect=${redirectParam}`}
              className="flex items-center justify-center rounded-lg border border-surface-variant px-4 py-2.5 font-label-md text-label-md font-semibold text-primary transition-colors hover:bg-surface-container-low"
            >
              Log In
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
