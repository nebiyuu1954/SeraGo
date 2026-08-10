import type { ReactNode } from 'react'
import { cn } from '../../lib/cn.ts'

interface AuthSuccessProps {
  title: string
  message: string
  /** Optional CTA rendered below the message (e.g. "Back to sign in"). */
  action?: ReactNode
  /** 'error' renders a destructive variant (e.g. an invalid reset link). */
  tone?: 'success' | 'error'
  icon?: string
}

/** Centered confirmation/status card shown after an auth action completes. */
export default function AuthSuccess({
  title,
  message,
  action,
  tone = 'success',
  icon,
}: AuthSuccessProps) {
  const iconName = icon ?? (tone === 'success' ? 'check_circle' : 'error')
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className="flex flex-col items-center rounded-2xl border border-outline-variant bg-surface-container-lowest p-10 text-center"
    >
      <span
        className={cn(
          'flex h-16 w-16 items-center justify-center rounded-full',
          tone === 'success' ? 'bg-success/15' : 'bg-error/15',
        )}
      >
        <span
          className={cn(
            'material-symbols-outlined text-4xl',
            tone === 'success' ? 'text-success' : 'text-error',
          )}
        >
          {iconName}
        </span>
      </span>
      <h2 className="mt-5 font-headline-lg text-headline-lg font-semibold text-on-surface">
        {title}
      </h2>
      <p className="mt-2 font-body-md text-body-md text-on-surface-variant">
        {message}
      </p>
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
