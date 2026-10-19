import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  ApiError,
  fetchExternalProviders,
  getApiErrorMessage,
  signInTelegram,
  storeAuthTokens,
  type TelegramAuthPayload,
} from '../../api'
import { trackEvent } from '../../lib/analytics'

declare global {
  interface Window {
    Telegram?: {
      Login?: {
        auth: (
          options: { bot_id: string; request_access?: boolean; lang?: string },
          callback: (user: TelegramAuthPayload | false) => void,
        ) => void
      }
    }
  }
}

interface TelegramButtonProps {
  /** Label on the button, e.g. "Log in with Telegram" / "Sign up with Telegram". */
  label: string
  /** Optional custom handler called upon successful Telegram authentication. */
  onAuth?: (user: TelegramAuthPayload) => void
  /** Optional CSS class overrides. */
  className?: string
}

/**
 * Telegram authentication button matching GoogleButton's design tokens.
 * Launches the official Telegram Login popup and dispatches auth to the backend.
 */
export default function TelegramButton({ label, onAuth, className }: TelegramButtonProps) {
  const navigate = useNavigate()
  const [available, setAvailable] = useState(false)
  const [botUsername, setBotUsername] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const scriptLoadedRef = useRef(false)

  useEffect(() => {
    let cancelled = false
    fetchExternalProviders()
      .then((data) => {
        if (cancelled) return
        const isConfigured = data.providers.includes('Telegram') && Boolean(data.telegramBotUsername)
        setAvailable(isConfigured)
        setBotUsername(data.telegramBotUsername ?? null)
      })
      .catch(() => {
        // External auth / Telegram unavailable
      })

    return () => {
      cancelled = true
    }
  }, [])

  // Lazily inject Telegram widget JS once available
  useEffect(() => {
    if (!botUsername || scriptLoadedRef.current) return
    if (document.getElementById('telegram-widget-script')) {
      scriptLoadedRef.current = true
      return
    }

    const script = document.createElement('script')
    script.id = 'telegram-widget-script'
    script.src = 'https://telegram.org/js/telegram-widget.js?22'
    script.async = true
    script.onload = () => {
      scriptLoadedRef.current = true
    }
    document.head.appendChild(script)
  }, [botUsername])

  const handleClick = () => {
    if (!available || !botUsername) return
    setError(null)
    setLoading(true)

    const triggerAuth = () => {
      if (!window.Telegram?.Login?.auth) {
        setLoading(false)
        setError('Telegram login widget is still loading. Please try again.')
        return
      }

      window.Telegram.Login.auth(
        { bot_id: botUsername, request_access: true },
        (user) => {
          setLoading(false)
          if (!user) {
            // User cancelled or closed the Telegram popup
            return
          }

          if (onAuth) {
            onAuth(user)
            return
          }

          // Default login flow
          setLoading(true)
          signInTelegram(user)
            .then((tokens) => {
              storeAuthTokens(tokens)
              trackEvent('login', { method: 'telegram' })
              navigate('/dashboard')
            })
            .catch((err) => {
              setLoading(false)
              if (err instanceof ApiError && err.status === 400) {
                const payload = err.payload as
                  | {
                      message?: string
                      signupToken?: string
                      telegramUser?: {
                        id: number
                        firstName: string
                        lastName: string
                        username: string
                        photoUrl: string
                      }
                    }
                  | undefined

                // Brand-new Telegram user -> route to profile completion page
                navigate('/auth/telegram/callback', {
                  state: {
                    signupToken: payload?.signupToken,
                    telegramUser: payload?.telegramUser ?? {
                      id: user.id,
                      firstName: user.first_name ?? '',
                      lastName: user.last_name ?? '',
                      username: user.username ?? '',
                      photoUrl: user.photo_url ?? '',
                    },
                    rawAuth: user,
                  },
                })
              } else {
                setError(getApiErrorMessage(err))
              }
            })
        },
      )
    }

    if (window.Telegram?.Login?.auth) {
      triggerAuth()
    } else {
      // Small timeout if script was just injected
      setTimeout(triggerAuth, 300)
    }
  }

  return (
    <div className="w-full">
      <button
        type="button"
        onClick={handleClick}
        disabled={!available || loading}
        aria-disabled={!available || loading}
        title={
          available
            ? label
            : 'Telegram sign-in is not configured yet — add TELEGRAM_BOT_TOKEN in the backend .env'
        }
        className={
          className ??
          'flex w-full cursor-pointer items-center justify-center gap-3 rounded-xl border border-outline-variant bg-surface-container-lowest py-2.5 font-label-md text-label-md font-medium text-on-surface shadow-sm transition-colors duration-200 hover:border-[#229ED9]/50 hover:bg-surface-container-low disabled:cursor-not-allowed disabled:opacity-60'
        }
      >
        <svg
          className="h-5 w-5 shrink-0"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden="true"
        >
          <path
            d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm4.64 6.8c-.15 1.58-.8 5.42-1.13 7.19-.14.75-.42 1-.68 1.03-.58.05-1.02-.38-1.58-.75-.88-.58-1.38-.94-2.23-1.5-.99-.65-.35-1.01.22-1.59.15-.15 2.71-2.48 2.76-2.69a.2.2 0 00-.05-.18c-.06-.05-.14-.03-.21-.02-.09.02-1.49.95-4.22 2.79-.4.27-.76.41-1.08.4-.36-.01-1.04-.2-1.55-.37-.63-.2-1.12-.31-1.08-.66.02-.18.27-.36.74-.55 2.92-1.27 4.86-2.11 5.83-2.52 2.78-1.16 3.35-1.36 3.73-1.36.08 0 .27.02.39.12.1.08.13.19.14.27-.01.06.01.24 0 .37z"
            fill="#229ED9"
          />
        </svg>
        {loading ? 'Connecting…' : label}
      </button>

      {error && (
        <p className="mt-1.5 text-center font-label-xs text-label-xs text-error">{error}</p>
      )}
    </div>
  )
}
