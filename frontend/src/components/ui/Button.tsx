import type { MouseEventHandler, ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { cn } from '../../lib/cn.ts'

export type ButtonVariant = 'primary' | 'secondary' | 'ghost'
export type ButtonSize = 'sm' | 'md' | 'lg'

const variantClasses: Record<ButtonVariant, string> = {
  // Action Orange — the ONLY place primary calls-to-action use the accent.
  primary: 'bg-accent text-on-accent hover:opacity-90',
  secondary:
    'border border-outline-variant bg-surface-container-lowest text-on-surface hover:bg-surface-container-low',
  ghost: 'text-on-surface-variant hover:bg-surface-container-low',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'px-4 py-2 text-label-sm',
  md: 'px-6 py-3 text-label-md',
  lg: 'px-8 py-4 text-label-md',
}

const baseClasses =
  'inline-flex items-center justify-center gap-2 rounded-[0.125rem] font-medium tracking-[0.05em] transition-opacity duration-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:pointer-events-none disabled:opacity-60'

export interface ButtonProps {
  variant?: ButtonVariant
  size?: ButtonSize
  className?: string
  children: ReactNode
  /** Renders a React Router `<Link>` when provided. */
  to?: string
  /** Renders a plain `<a>` anchor when provided. */
  href?: string
  type?: 'button' | 'submit' | 'reset'
  disabled?: boolean
  onClick?: MouseEventHandler<HTMLElement>
  ariaLabel?: string
}

export function Button({
  variant = 'primary',
  size = 'md',
  className,
  children,
  to,
  href,
  type = 'button',
  disabled,
  onClick,
  ariaLabel,
}: ButtonProps) {
  const classes = cn(
    baseClasses,
    variantClasses[variant],
    sizeClasses[size],
    // Links and anchors can't carry the `disabled` attribute, so reflect
    // the state in their classes instead.
    disabled && (to || href) ? 'pointer-events-none opacity-60' : '',
    className,
  )

  if (to) {
    return (
      <Link
        to={to}
        className={classes}
        onClick={onClick}
        aria-label={ariaLabel}
      >
        {children}
      </Link>
    )
  }

  if (href) {
    return (
      <a
        href={href}
        className={classes}
        onClick={onClick}
        aria-label={ariaLabel}
      >
        {children}
      </a>
    )
  }

  return (
    <button
      type={type}
      className={classes}
      onClick={onClick}
      disabled={disabled}
      aria-label={ariaLabel}
    >
      {children}
    </button>
  )
}
