import type { Notification, NotificationType } from '../../types/notifications.ts'

interface NotificationItemProps {
  notification: Notification
  onClick: () => void
}

/** Icon and color mapping for notification types. */
const NOTIFICATION_STYLE: Record<
  NotificationType,
  { icon: string; color: string }
> = {
  application_received: { icon: 'person_add', color: 'text-blue-500' },
  application_status_changed: { icon: 'compare_arrows', color: 'text-purple-500' },
  job_alert: { icon: 'work', color: 'text-green-500' },
  saved_search_match: { icon: 'bookmark', color: 'text-amber-500' },
  payment_success: { icon: 'payments', color: 'text-emerald-500' },
  admin_review_result: { icon: 'admin_panel_settings', color: 'text-indigo-500' },
  system_announcement: { icon: 'campaign', color: 'text-orange-500' },
  daily_digest: { icon: 'digest', color: 'text-gray-500' },
}

/** Format relative time (e.g., "2 minutes ago", "1 hour ago"). */
function timeAgo(dateString: string): string {
  const now = new Date()
  const date = new Date(dateString)
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000)

  if (seconds < 60) return 'just now'
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`
  if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`

  return date.toLocaleDateString()
}

/**
 * Single notification row in the panel.
 * Unread notifications have a blue dot indicator.
 */
export default function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const style = NOTIFICATION_STYLE[notification.type] ?? {
    icon: 'info',
    color: 'text-gray-500',
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-surface-container-low ${
        !notification.isRead ? 'bg-primary-container/10' : ''
      }`}
    >
      {/* Icon */}
      <div className={`mt-0.5 shrink-0 ${style.color}`}>
        <span className="material-symbols-outlined text-xl">{style.icon}</span>
      </div>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <div className="flex items-start gap-2">
          <p
            className={`font-label-md text-label-md leading-tight ${
              !notification.isRead
                ? 'font-semibold text-on-surface'
                : 'font-medium text-on-surface/80'
            }`}
          >
            {notification.title}
          </p>
          {/* Unread indicator dot */}
          {!notification.isRead && (
            <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-primary" />
          )}
        </div>
        <p className="mt-0.5 font-body-sm text-body-sm text-on-surface-variant/70 line-clamp-2">
          {notification.body}
        </p>
        <p className="mt-1 font-label-xs text-label-xs text-on-surface-variant/50">
          {timeAgo(notification.createdAt)}
        </p>
      </div>
    </button>
  )
}
