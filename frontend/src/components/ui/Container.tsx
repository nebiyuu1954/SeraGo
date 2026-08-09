import type { ReactNode } from 'react'
import { cn } from '../../lib/cn.ts'

export function Container({
  children,
  className,
}: {
  children: ReactNode
  className?: string
}) {
  return (
    <div
      className={cn(
        'mx-auto w-full max-w-container-max px-margin-mobile sm:px-margin-desktop',
        className,
      )}
    >
      {children}
    </div>
  )
}
