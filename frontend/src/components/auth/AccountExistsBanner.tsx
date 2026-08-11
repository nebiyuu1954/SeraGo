import { Link } from 'react-router-dom'

/**
 * Friendly replacement for the generic error banner when signup fails because
 * an account with that email already exists — tells the user to log in to
 * their existing account instead of trying to create a second one.
 */
export default function AccountExistsBanner({ email }: { email: string }) {
  return (
    <div
      role="alert"
      className="mb-5 flex flex-col gap-3 rounded-xl border border-primary/30 bg-primary-container/70 p-4 sm:flex-row sm:items-center"
    >
      <div className="flex min-w-0 flex-1 items-start gap-3">
        <span
          aria-hidden="true"
          className="material-symbols-outlined mt-0.5 shrink-0 text-2xl text-primary"
        >
          account_circle
        </span>
        <div className="min-w-0">
          <p className="font-label-md text-label-md font-semibold text-on-primary-container">
            You already have an account
          </p>
          <p className="font-label-sm text-label-sm leading-relaxed text-on-surface-variant">
            {email} is already registered. Log in to that account instead of
            creating a new one.
          </p>
        </div>
      </div>
      <Link
        to="/login"
        state={{ email }}
        className="shrink-0 rounded-xl bg-primary px-4 py-2.5 text-center font-label-md text-label-md font-semibold text-on-primary shadow-sm transition-all duration-200 hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-primary/40 focus:ring-offset-2"
      >
        Sign in
      </Link>
    </div>
  )
}
