import type { ChangeEvent, FocusEvent, ReactNode } from 'react'
import { cn } from '../../lib/cn.ts'

export const inputBaseClasses =
  'w-full rounded-xl border border-outline-variant bg-surface-container-lowest py-2.5 pl-11 pr-3 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 transition-all duration-200 focus:border-primary focus:ring-2 focus:ring-primary/25 focus:outline-none'

/**
 * Leading icon inside a field.
 *
 * NOTE: `.material-symbols-outlined` sets `display: inline-block` in plain
 * (un-layered) CSS, which beats Tailwind's layered `flex` utility — so this
 * must NOT rely on `flex items-center`. Centering is done with
 * `top-1/2 -translate-y-1/2` instead.
 */
function InputIcon({ icon, className }: { icon: string; className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        'material-symbols-outlined pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-2xl leading-none text-on-surface-variant',
        className,
      )}
    >
      {icon}
    </span>
  )
}

export interface TextInputProps {
  id: string
  name: string
  label: string
  icon?: string
  hideIconMobile?: boolean
  error?: string
  type?: string
  autoComplete?: string
  placeholder?: string
  value: string
  onChange: (event: ChangeEvent<HTMLInputElement>) => void
  onBlur?: (event: FocusEvent<HTMLInputElement>) => void
  onFocus?: () => void
  /** Rendered inside the input wrapper (e.g. the password visibility toggle). */
  suffix?: ReactNode
  /** Rendered after the error message (e.g. the password strength block). */
  hint?: ReactNode
  className?: string
}

export default function TextInput({
  id,
  name,
  label,
  icon,
  hideIconMobile = false,
  error,
  type = 'text',
  autoComplete,
  placeholder,
  value,
  onChange,
  onBlur,
  onFocus,
  suffix,
  hint,
  className,
}: TextInputProps) {
  return (
    <div className="space-y-1.5 min-w-0">
      <label
        htmlFor={id}
        className={cn(
          'block font-label-md text-label-md font-medium text-on-surface',
          hideIconMobile && 'text-xs sm:text-label-md truncate',
        )}
      >
        {label}
      </label>
      <div className="relative">
        {icon && (
          <InputIcon
            icon={icon}
            className={hideIconMobile ? 'hidden sm:inline-block' : undefined}
          />
        )}
        <input
          id={id}
          name={name}
          type={type}
          autoComplete={autoComplete}
          placeholder={placeholder}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          onFocus={onFocus}
          aria-invalid={Boolean(error)}
          className={cn(
            inputBaseClasses,
            hideIconMobile &&
              'pl-3 sm:pl-11 pr-2 sm:pr-3 text-sm sm:text-body-md py-2 sm:py-2.5',
            error && 'border-error focus:border-error focus:ring-error/25',
            className,
          )}
        />
        {suffix}
      </div>
      {error && (
        <p
          role="alert"
          className="font-label-sm text-xs sm:text-label-sm text-error leading-tight"
        >
          {error}
        </p>
      )}
      {hint}
    </div>
  )
}
