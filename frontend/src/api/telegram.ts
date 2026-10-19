import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

/** GET /api/telegram/link payload — the deep link plus the raw token. */
export interface TelegramLinkData {
  /** Ready-to-open t.me URL (`https://t.me/<bot>?start=<token>`). */
  url: string
  /** The signed start token embedded in `url` (for diagnostics). */
  token: string
}

/** GET /api/telegram/status payload. */
export interface TelegramStatusData {
  linked: boolean
}

/**
 * GET /api/telegram/link — a t.me deep link that starts the account-linking
 * flow. Opening it sends `/start <token>` to the bot, which links the chat.
 */
export function fetchTelegramLinkUrl(accessToken: string): Promise<TelegramLinkData> {
  return request<TelegramLinkData>(API_ENDPOINTS.telegram.link, {
    headers: auth(accessToken),
  })
}

/** GET /api/telegram/status — whether this account has Telegram linked. */
export function fetchTelegramStatus(accessToken: string): Promise<TelegramStatusData> {
  return request<TelegramStatusData>(API_ENDPOINTS.telegram.status, {
    headers: auth(accessToken),
  })
}

/** POST /api/telegram/unlink — disconnect Telegram from this account. */
export function unlinkTelegram(accessToken: string): Promise<unknown> {
  return request(API_ENDPOINTS.telegram.unlink, {
    method: 'POST',
    headers: auth(accessToken),
  })
}

/** POST /api/telegram/connect-chat — connect Telegram chat to current signed-in user. */
export function connectTelegramChat(
  accessToken: string,
  chatId: number,
  token: string,
): Promise<{ success: boolean }> {
  return request<{ success: boolean }>(API_ENDPOINTS.telegram.connectChat, {
    method: 'POST',
    headers: auth(accessToken),
    body: { chatId, token },
  })
}

