import { useCallback, useEffect, useRef, useState } from 'react'
import { API_ENDPOINTS } from '../api/endpoints.ts'
import { request } from '../api/client.ts'
import { getStoredAuthTokens } from '../api/auth.ts'
import type {
  Notification,
  PagedNotifications,
  UnreadCountResponse,
  MarkReadResult,
} from '../types/notifications.ts'
import { config } from '../config/index.ts'

/** Connection state for SignalR. */
type SignalRStatus = 'connecting' | 'connected' | 'disconnected'

/**
 * Core notification hook. Manages:
 * 1. Unread count (from Redis via API)
 * 2. SignalR connection (real-time updates)
 * 3. 30-second polling fallback
 * 4. Mark as read functionality
 *
 * Usage:
 * ```tsx
 * const { unreadCount, isConnected, markAsRead, markAllAsRead } = useNotifications()
 * ```
 */
export function useNotifications() {
  const [unreadCount, setUnreadCount] = useState(0)
  const [signalRStatus, setSignalRStatus] = useState<SignalRStatus>('disconnected')
  const signalRConnectionRef = useRef<any>(null)
  const pollIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const mountedRef = useRef(true)

  // ── Fetch unread count from API ──
  const fetchUnreadCount = useCallback(async () => {
    try {
      const tokens = getStoredAuthTokens()
      if (!tokens) return

      const data = await request<UnreadCountResponse>(
        API_ENDPOINTS.notifications.unreadCount,
        { headers: { Authorization: `Bearer ${tokens.accessToken}` } },
      )
      if (mountedRef.current) {
        setUnreadCount(data.count)
      }
    } catch {
      // Silently ignore — polling will retry
    }
  }, [])

  // ── Mark specific notifications as read ──
  const markAsRead = useCallback(async (notificationIds: string[]) => {
    try {
      const tokens = getStoredAuthTokens()
      if (!tokens) return

      const data = await request<MarkReadResult>(
        API_ENDPOINTS.notifications.read,
        {
          method: 'PATCH',
          headers: { Authorization: `Bearer ${tokens.accessToken}` },
          body: { notificationIds },
        },
      )

      if (mountedRef.current) {
        setUnreadCount((prev) => Math.max(0, prev - data.markedCount))
      }

      return data
    } catch {
      // Handle error
    }
  }, [])

  // ── Mark all notifications as read ──
  const markAllAsRead = useCallback(async () => {
    try {
      const tokens = getStoredAuthTokens()
      if (!tokens) return

      const data = await request<MarkReadResult>(
        API_ENDPOINTS.notifications.readAll,
        {
          method: 'PATCH',
          headers: { Authorization: `Bearer ${tokens.accessToken}` },
        },
      )

      if (mountedRef.current) {
        setUnreadCount(0)
      }

      return data
    } catch {
      // Handle error
    }
  }, [])

  // ── SignalR connection setup ──
  useEffect(() => {
    mountedRef.current = true

    const tokens = getStoredAuthTokens()
    if (!tokens) return

    // Dynamically import SignalR to avoid adding it to the main bundle
    // if the user isn't logged in
    let connection: any = null
    let cancelled = false

    async function connectSignalR() {
      try {
        const signalR = await import('@microsoft/signalr')

        if (cancelled) return

        const hubUrl = config.apiBaseUrl.replace('/api', '') + '/hubs/notifications'

        connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl, {
            accessTokenFactory: () => getStoredAuthTokens()?.accessToken ?? '',
            transport: signalR.HttpTransportType.LongPolling,
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
          .configureLogging(signalR.LogLevel.Warning)
          .build()

        // Handle incoming notifications
        connection.on('new_notification', (event: any) => {
          if (mountedRef.current) {
            setUnreadCount(event.unreadCount)
          }
        })

        // Handle unread count updates (mark read, batch, etc.)
        connection.on('unread_count_updated', (event: any) => {
          if (mountedRef.current) {
            setUnreadCount(event.count)
          }
        })

        // Connection state tracking
        connection.onreconnecting(() => {
          if (mountedRef.current) setSignalRStatus('connecting')
        })
        connection.onreconnected(() => {
          if (mountedRef.current) setSignalRStatus('connected')
        })
        connection.onclose(() => {
          if (mountedRef.current) setSignalRStatus('disconnected')
        })

        // Start connection
        if (mountedRef.current) setSignalRStatus('connecting')
        await connection.start()
        if (mountedRef.current) {
          signalRConnectionRef.current = connection
          setSignalRStatus('connected')
        }
      } catch {
        // SignalR not available or connection failed — polling will handle it
        if (mountedRef.current) setSignalRStatus('disconnected')
      }
    }

    connectSignalR()

    return () => {
      cancelled = true
      if (connection) {
        connection.stop().catch(() => {})
      }
    }
  }, [])

  // ── 30-second polling fallback ──
  useEffect(() => {
    // Always poll — SignalR might be connected but we still want
    // to catch notifications from other tabs/devices
    fetchUnreadCount()

    pollIntervalRef.current = setInterval(fetchUnreadCount, 30_000)

    return () => {
      if (pollIntervalRef.current) {
        clearInterval(pollIntervalRef.current)
      }
    }
  }, [fetchUnreadCount])

  // ── Cleanup ──
  useEffect(() => {
    return () => {
      mountedRef.current = false
    }
  }, [])

  return {
    unreadCount,
    isConnected: signalRStatus === 'connected',
    signalRStatus,
    markAsRead,
    markAllAsRead,
    refreshCount: fetchUnreadCount,
  }
}

/**
 * Hook for fetching paginated notification list.
 * Used by the notification panel when the user clicks the bell.
 */
export function useNotificationList() {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)
  const pageSize = 20

  const fetchNotifications = useCallback(async (pageNum: number) => {
    setLoading(true)
    try {
      const tokens = getStoredAuthTokens()
      if (!tokens) return

      const params = new URLSearchParams({
        page: String(pageNum),
        pageSize: String(pageSize),
      })

      const data = await request<PagedNotifications>(
        `${API_ENDPOINTS.notifications.list}?${params}`,
        { headers: { Authorization: `Bearer ${tokens.accessToken}` } },
      )

      setNotifications(data.notifications)
      setTotalCount(data.totalCount)
      setTotalPages(data.totalPages)
      setPage(data.page)
    } catch {
      // Handle error
    } finally {
      setLoading(false)
    }
  }, [])

  const goToPage = useCallback(
    (pageNum: number) => {
      fetchNotifications(pageNum)
    },
    [fetchNotifications],
  )

  return {
    notifications,
    totalCount,
    totalPages,
    page,
    loading,
    pageSize,
    fetchNotifications,
    goToPage,
  }
}
