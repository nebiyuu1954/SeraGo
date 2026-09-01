import { useEffect } from 'react'
import { useNotificationList } from '../../hooks/useNotifications.ts'
import NotificationItem from './NotificationItem.tsx'

interface NotificationPanelProps {
  onMarkAsRead: (ids: string[]) => Promise<unknown>
  onMarkAllAsRead: () => Promise<unknown>
  onClose: () => void
}

/**
 * Dropdown panel showing paginated notification list.
 * Opens when the bell icon is clicked.
 */
export default function NotificationPanel({
  onMarkAsRead,
  onMarkAllAsRead,
  onClose,
}: NotificationPanelProps) {
  const {
    notifications,
    totalCount,
    page,
    totalPages,
    loading,
    fetchNotifications,
    goToPage,
  } = useNotificationList()

  // Fetch first page on mount
  useEffect(() => {
    fetchNotifications(1)
  }, [fetchNotifications])

  const handleMarkAllRead = async () => {
    await onMarkAllAsRead()
    // Re-fetch to update the read state visually
    fetchNotifications(page)
  }

  const handleNotificationClick = async (notificationId: string, isRead: boolean) => {
    if (!isRead) {
      await onMarkAsRead([notificationId])
    }
    // Navigation would happen here based on notification.data
    onClose()
  }

  return (
    <div className="flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-surface-variant px-4 py-3">
        <h3 className="font-label-lg text-label-lg font-semibold text-on-surface">
          Notifications
        </h3>
        {totalCount > 0 && (
          <button
            type="button"
            onClick={handleMarkAllRead}
            className="font-label-sm text-label-sm text-primary transition-colors hover:text-primary/80"
          >
            Mark all read
          </button>
        )}
      </div>

      {/* Notification list */}
      <div className="max-h-[400px] overflow-y-auto">
        {loading && notifications.length === 0 ? (
          <div className="flex items-center justify-center py-8">
            <span className="h-5 w-5 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
          </div>
        ) : notifications.length === 0 ? (
          <div className="py-8 text-center">
            <span className="material-symbols-outlined mb-2 text-4xl text-on-surface-variant/50">
              notifications_none
            </span>
            <p className="font-body-sm text-body-sm text-on-surface-variant/70">
              No notifications yet
            </p>
          </div>
        ) : (
          notifications.map((notification) => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              onClick={() => handleNotificationClick(notification.id, notification.isRead)}
            />
          ))
        )}
      </div>

      {/* Footer — pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 border-t border-surface-variant px-4 py-2">
          <button
            type="button"
            onClick={() => goToPage(page - 1)}
            disabled={page <= 1}
            className="font-label-sm text-label-sm text-primary disabled:text-on-surface-variant/50 disabled:cursor-not-allowed"
          >
            ← Prev
          </button>
          <span className="font-label-sm text-label-sm text-on-surface-variant">
            {page} / {totalPages}
          </span>
          <button
            type="button"
            onClick={() => goToPage(page + 1)}
            disabled={page >= totalPages}
            className="font-label-sm text-label-sm text-primary disabled:text-on-surface-variant/50 disabled:cursor-not-allowed"
          >
            Next →
          </button>
        </div>
      )}
    </div>
  )
}
