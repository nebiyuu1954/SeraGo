import { useState } from 'react'
import { cn } from '../../lib/cn'

interface AccordionSectionProps {
  title: string
  description: string
  icon: string
  defaultOpen?: boolean
  completion?: { filled: number; total: number }
  children: React.ReactNode
}

export default function AccordionSection({
  title,
  description,
  icon,
  defaultOpen = false,
  completion,
  children,
}: AccordionSectionProps) {
  const [open, setOpen] = useState(defaultOpen)

  return (
    <section className="rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm overflow-hidden">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex w-full items-center justify-between px-6 py-4 text-left transition-colors hover:bg-surface-container-low"
      >
        <div className="flex items-center gap-3 min-w-0">
          <span className="material-symbols-outlined text-xl text-primary shrink-0">
            {icon}
          </span>
          <div className="min-w-0">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">
              {title}
            </h2>
            <p className="font-label-sm text-label-sm text-on-surface-variant truncate">
              {description}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3 shrink-0 ml-4">
          {completion && (
            <span
              className={cn(
                'font-label-sm text-label-sm px-2 py-0.5 rounded',
                completion.filled === completion.total
                  ? 'bg-success/10 text-success'
                  : 'bg-surface-container-low text-on-surface-variant',
              )}
            >
              {completion.filled}/{completion.total}
            </span>
          )}
          <span
            className={cn(
              'material-symbols-outlined text-xl text-on-surface-variant transition-transform duration-200',
              open && 'rotate-180',
            )}
          >
            expand_more
          </span>
        </div>
      </button>

      {open && (
        <div className="border-t border-surface-variant">
          <div className="grid gap-6 p-6 md:grid-cols-2">{children}</div>
        </div>
      )}
    </section>
  )
}
