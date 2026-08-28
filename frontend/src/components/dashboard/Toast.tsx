import { toast as sonnerToast } from 'sonner'

type ToastType = 'success' | 'error' | 'warning' | 'info'

interface ToastContextValue {
  showToast: (message: string, type?: ToastType) => void
}

/** Thin wrapper around Sonner so existing callers keep working. */
export function useToast(): ToastContextValue {
  const showToast = (message: string, type: ToastType = 'success') => {
    switch (type) {
      case 'success':
        sonnerToast.success(message)
        break
      case 'error':
        sonnerToast.error(message)
        break
      case 'warning':
        sonnerToast.warning(message)
        break
      default:
        sonnerToast.info(message)
        break
    }
  }

  return { showToast }
}

/**
 * Re-export Toaster from Sonner — mount this once in main.tsx.
 * The old <ToastProvider> wrapper is no longer needed.
 */
export { Toaster as ToastProvider } from 'sonner'
