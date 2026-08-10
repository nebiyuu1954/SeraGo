import type { ReactNode } from 'react'

/** Horizontal "Or continue with email" divider used between the email form and OAuth. */
export default function Divider({
  children = 'Or continue with email',
}: {
  children?: ReactNode
}) {
  return (
    <div className="relative py-1">
      <div aria-hidden="true" className="absolute inset-0 flex items-center">
        <div className="w-full border-t border-outline-variant" />
      </div>
      <div className="relative flex justify-center">
        <span className="bg-surface-container-lowest px-3 font-label-sm text-label-sm text-on-surface-variant">
          {children}
        </span>
      </div>
    </div>
  )
}
