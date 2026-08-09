import type { ReactNode } from 'react'
import { cn } from '../../lib/cn.ts'

export function Badge({
  children,
  className,
}: {
  children: ReactNode
  className?: string
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-2 rounded-full border border-outline-variant bg-surface-container-low px-4 py-1.5 text-label-sm font-medium text-on-surface-variant',
        className,
      )}
    >
      {children}
    </span>
  )
}
