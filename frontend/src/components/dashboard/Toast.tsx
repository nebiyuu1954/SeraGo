import {
  createContext,
  useCallback,
  useContext,
  useState,
  type ReactNode,
} from 'react'
import { cn } from '../../lib/cn.ts'

type ToastType = 'success' | 'error'

interface ToastItem {
  id: number
  message: string
  type: ToastType
}

interface ToastContextValue {
  showToast: (message: string, type?: ToastType) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

let nextId = 1

/** Shows a transient toast bottom-right; auto-dismisses after ~3.5s. */
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within <ToastProvider>')
  return ctx
}

/**
 * Global toast host. Wrap an app subtree (e.g. the dashboard shell) with
 * <ToastProvider>, then call `showToast(message, type?)` from any page to pop
 * a notification in the bottom-right corner.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const showToast = useCallback((message: string, type: ToastType = 'success') => {
    const id = nextId++
    setToasts((prev) => [...prev, { id, message, type }])
    window.setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id))
    }, 3500)
  }, [])

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      {/* Toast viewport — fixed bottom-right, above everything */}
      <div
        aria-live="polite"
        className="pointer-events-none fixed right-4 top-4 z-50 flex w-80 max-w-[calc(100vw-2rem)] flex-col gap-2"
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            role="status"
            className={cn(
              'pointer-events-auto flex items-center gap-2.5 rounded-xl border px-4 py-3 font-label-md text-label-md shadow-lg',
              t.type === 'error'
                ? 'border-error/30 bg-error-container text-on-error-container'
                : 'border-primary/30 bg-primary-container text-on-primary-container',
            )}
          >
            <span className="material-symbols-outlined text-lg">
              {t.type === 'error' ? 'error' : 'check_circle'}
            </span>
            <span className="min-w-0 flex-1 truncate">{t.message}</span>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
