import type { ChangeEvent, FocusEvent, ReactNode } from 'react'
import { cn } from '../../lib/cn.ts'

export interface EthiopianPhoneInputProps {
  id: string
  name: string
  label?: string
  error?: string
  placeholder?: string
  value: string
  onChange: (event: ChangeEvent<HTMLInputElement>) => void
  onBlur?: (event: FocusEvent<HTMLInputElement>) => void
  onFocus?: () => void
  hint?: ReactNode
  className?: string
  required?: boolean
}

export default function EthiopianPhoneInput({
  id,
  name,
  label = 'Phone number',
  error,
  placeholder = '0910 10 10 10 or 910 10 10 10',
  value,
  onChange,
  onBlur,
  onFocus,
  hint,
  className,
  required = true,
}: EthiopianPhoneInputProps) {
  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between">
        <label
          htmlFor={id}
          className="block font-label-md text-label-md font-medium text-on-surface"
        >
          {label} {required && <span className="text-primary">*</span>}
        </label>
      </div>

      <div
        className={cn(
          'relative flex rounded-xl border border-outline-variant bg-surface-container-lowest transition-all duration-200 focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/25',
          error &&
            'border-error focus-within:border-error focus-within:ring-error/25',
        )}
      >
        {/* Country code prefix */}
        <div
          aria-hidden="true"
          className="flex select-none items-center gap-1.5 rounded-l-xl border-r border-outline-variant/60 bg-surface-container-low px-3 py-2.5 font-label-md text-label-md font-semibold text-on-surface"
        >
          <span className="text-base leading-none">🇪🇹</span>
          <span>+251</span>
        </div>

        {/* Input */}
        <input
          id={id}
          name={name}
          type="tel"
          autoComplete="tel-national"
          placeholder={placeholder}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          onFocus={onFocus}
          aria-invalid={Boolean(error)}
          className={cn(
            'w-full rounded-r-xl bg-transparent py-2.5 pl-3 pr-3 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:outline-none',
            className,
          )}
        />
      </div>

      {error ? (
        <p role="alert" className="font-label-sm text-label-sm text-error">
          {error}
        </p>
      ) : (
        <p className="font-label-sm text-label-sm text-on-surface-variant/70">
          Enter 9 or 10 digits (e.g. 0910101010 or 910101010)
        </p>
      )}

      {hint}
    </div>
  )
}
