import { useEffect, useRef, useState } from 'react'
import {
  fetchTelegramLinkUrl,
  fetchTelegramStatus,
  getApiErrorMessage,
  getStoredAuthTokens,
  unlinkTelegram,
} from '../../api/index.ts'

type LinkState = 'loading' | 'linked' | 'unlinked' | 'offline'

/**
 * Telegram account-linking card for Settings → Notifications.
 *
 * "Connect" fetches a signed t.me deep link and opens it; the user lands in the
 * bot and the /start handshake links their chat to this account. "Refresh"
 * re-checks the status after they finish in Telegram. The per-type Telegram
 * toggles below control *which* notifications use the linked chat.
 */
interface TelegramLinkCardProps {
  onStatusChange?: (linked: boolean) => void
}

export default function TelegramLinkCard({ onStatusChange }: TelegramLinkCardProps = {}) {
  // No stored tokens → "offline" from the start (no synchronous setState in
  // the effect below, mirroring the PasswordSetupCard pattern).
  const [state, setState] = useState<LinkState>(() =>
    getStoredAuthTokens() ? 'loading' : 'offline',
  )
  const [telegramUser, setTelegramUser] = useState<{
    username?: string | null
    firstName?: string | null
  } | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const onStatusChangeRef = useRef(onStatusChange)
  useEffect(() => {
    onStatusChangeRef.current = onStatusChange
  })

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    let cancelled = false
    fetchTelegramStatus(tokens.accessToken)
      .then((data) => {
        if (!cancelled) {
          setState(data.linked ? 'linked' : 'unlinked')
          setTelegramUser(data.linked ? { username: data.username, firstName: data.firstName } : null)
          onStatusChangeRef.current?.(data.linked)
        }
      })
      .catch(() => {
        if (!cancelled) {
          setState('offline')
          onStatusChangeRef.current?.(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (typeof window !== 'undefined' && window.location.hash.toLowerCase() === '#telegram') {
      setTimeout(() => {
        document.getElementById('telegram')?.scrollIntoView({ behavior: 'smooth', block: 'center' })
      }, 150)
    }
  }, [])

  const refresh = async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setState('offline')
      onStatusChange?.(false)
      return
    }
    try {
      const data = await fetchTelegramStatus(tokens.accessToken)
      setState(data.linked ? 'linked' : 'unlinked')
      setTelegramUser(data.linked ? { username: data.username, firstName: data.firstName } : null)
      onStatusChange?.(data.linked)
    } catch {
      setState('offline')
      onStatusChange?.(false)
    }
  }

  const connect = async () => {
    setError(null)
    setBusy(true)
    try {
      const tokens = getStoredAuthTokens()
      if (!tokens) throw new Error('Please sign in again to link Telegram.')
      const { url } = await fetchTelegramLinkUrl(tokens.accessToken)
      window.open(url, '_blank', 'noopener,noreferrer')
    } catch (e) {
      setError(getApiErrorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const disconnect = async () => {
    setError(null)
    setBusy(true)
    try {
      const tokens = getStoredAuthTokens()
      if (!tokens) throw new Error('Please sign in again to unlink Telegram.')
      await unlinkTelegram(tokens.accessToken)
      setState('unlinked')
      setTelegramUser(null)
      onStatusChange?.(false)
    } catch (e) {
      setError(getApiErrorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div id="telegram" className="rounded-lg border border-surface-variant bg-surface-variant/20 p-4 transition-all">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="font-label-md text-label-md font-semibold text-on-surface">
            Telegram bot
          </p>
          <p className="font-label-sm text-label-sm text-on-surface-variant/70">
            {state === 'linked'
              ? 'Connected — matched jobs can be sent to your Telegram chat.'
              : 'Connect Telegram to receive job alerts in chat.'}
          </p>

          {state === 'linked' && (telegramUser?.username || telegramUser?.firstName) && (
            <div className="mt-2 inline-flex items-center gap-1.5 rounded-md bg-primary/10 px-2 py-0.5 font-label-xs text-label-xs font-medium text-primary">
              <span className="material-symbols-outlined text-sm">person</span>
              <span>
                Connected to {telegramUser.username ?? telegramUser.firstName}
                {telegramUser.firstName && telegramUser.username ? ` (${telegramUser.firstName})` : ''}
              </span>
            </div>
          )}
        </div>

        <span
          className={`shrink-0 rounded-full px-2.5 py-1 text-label-xs ${
            state === 'linked'
              ? 'bg-primary/10 text-primary'
              : 'bg-surface-variant/60 text-on-surface-variant/70'
          }`}
        >
          {state === 'linked' ? 'Linked' : state === 'loading' ? 'Checking…' : 'Not linked'}
        </span>
      </div>

      {error && (
        <p className="mt-2 font-label-sm text-label-sm text-error">{error}</p>
      )}

      <div className="mt-3 flex flex-wrap items-center gap-2">
        {state === 'linked' ? (
          <button
            type="button"
            onClick={disconnect}
            disabled={busy}
            className="rounded-full border border-surface-variant px-3 py-1.5 font-label-sm text-label-sm text-on-surface transition-colors hover:bg-surface-variant/50 disabled:opacity-50"
          >
            {busy ? 'Disconnecting…' : 'Disconnect'}
          </button>
        ) : (
          <button
            type="button"
            onClick={connect}
            disabled={busy || state === 'loading'}
            className="rounded-full bg-primary px-3 py-1.5 font-label-sm text-label-sm text-on-primary transition-opacity hover:opacity-90 disabled:opacity-50"
          >
            {busy ? 'Opening…' : 'Connect Telegram'}
          </button>
        )}

        <button
          type="button"
          onClick={() => void refresh()}
          disabled={busy}
          className="font-label-sm text-label-sm text-primary underline-offset-2 hover:underline disabled:opacity-50"
        >
          {state === 'unlinked' ? "I've linked it — refresh" : 'Refresh'}
        </button>
      </div>
    </div>
  )
}
