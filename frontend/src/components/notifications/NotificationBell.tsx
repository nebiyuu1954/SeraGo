import { useState } from 'react'
import NotificationPanel from './NotificationPanel.tsx'

interface NotificationBellProps {
  unreadCount: number
  isConnected: boolean
  onMarkAsRead: (ids: string[]) => Promise<unknown>
  onMarkAllAsRead: () => Promise<unknown>
}

/**
 * Bell icon with unread count badge for the dashboard header.
 * Clicking opens the notification panel dropdown.
 *
 * Placement: DashboardShell header, next to the avatar menu button.
 */
export default function NotificationBell({
  unreadCount,
  isConnected,
  onMarkAsRead,
  onMarkAllAsRead,
}: NotificationBellProps) {
  const [isOpen, setIsOpen] = useState(false)

  return (
    <div className="relative">
      {/* Bell button */}
      <button
        type="button"
        onClick={() => setIsOpen((o) => !o)}
        className="relative flex h-10 w-10 items-center justify-center rounded-full border border-surface-variant bg-surface-container-low transition-colors hover:bg-surface-container"
        aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <span className="material-symbols-outlined text-on-surface-variant">
          notifications
        </span>

        {/* Unread badge */}
        {unreadCount > 0 && (
          <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-error px-1 font-label-sm text-label-sm font-bold text-on-error">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}

        {/* Connection indicator (small dot) */}
        {!isConnected && (
          <span
            className="absolute bottom-0 right-0 h-2.5 w-2.5 rounded-full border-2 border-surface-container-low bg-surface-variant"
            title="Reconnecting..."
          />
        )}
      </button>

      {/* Dropdown panel */}
      {isOpen && (
        <>
          {/* Backdrop — click to close */}
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
          />

          {/* Panel */}
          <div className="absolute right-0 z-20 mt-2 w-[380px] max-h-[500px] overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-lg">
            <NotificationPanel
              onMarkAsRead={onMarkAsRead}
              onMarkAllAsRead={onMarkAllAsRead}
              onClose={() => setIsOpen(false)}
            />
          </div>
        </>
      )}
    </div>
  )
}
