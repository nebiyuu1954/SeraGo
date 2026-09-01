/**
 * Notification types for the SeraGo notification system.
 * These mirror the backend DTOs from NotificationService and NotificationEndpoints.
 */

/** A single notification record. */
export interface Notification {
  id: string
  type: NotificationType
  title: string
  body: string
  /** Structured JSON payload — job IDs, application IDs, actor info, etc. */
  data: Record<string, unknown> | null
  isRead: boolean
  createdAt: string // ISO-8601
}

/** Notification event types — matches SeraGo.Core.Domain.Enums.NotificationType. */
export type NotificationType =
  | 'application_received'
  | 'application_status_changed'
  | 'job_alert'
  | 'saved_search_match'
  | 'payment_success'
  | 'admin_review_result'
  | 'system_announcement'
  | 'daily_digest'

/** Paginated notification list response. */
export interface PagedNotifications {
  notifications: Notification[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/** Unread count response. */
export interface UnreadCountResponse {
  count: number
}

/** Mark-as-read request. */
export interface MarkReadRequest {
  notificationIds: string[]
}

/** Mark-as-read response. */
export interface MarkReadResult {
  markedCount: number
}

/** SignalR event: new notification pushed in real-time. */
export interface NotificationPushEvent {
  unreadCount: number
  notification: Notification
}

/** SignalR event: unread count updated (mark read, batch update, etc.). */
export interface UnreadCountUpdateEvent {
  count: number
}
