import { request } from './client.ts'
import { API_ENDPOINTS } from './endpoints.ts'

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

export type TelegramMessageTarget = 'Channel' | 'BotDm' | 'Both'
export type TelegramMessageType =
  | 'GoodMorning'
  | 'GoodAfternoon'
  | 'GoodNight'
  | 'CustomAd'
  | 'CustomMessage'

export interface TelegramScheduledMessage {
  id: string
  title: string
  target: TelegramMessageTarget
  messageType: TelegramMessageType
  daysOfWeek: string
  specificDate: string | null
  scheduledTime: string
  customTemplate: string | null
  bannerUrl: string | null
  buttonText: string | null
  buttonUrl: string | null
  isActive: boolean
  lastRunUtc: string | null
  lastRunStatus: string | null
  createdAt: string
  updatedAt: string
}

export interface CreateScheduledMessageRequest {
  title: string
  target?: TelegramMessageTarget
  messageType?: TelegramMessageType
  daysOfWeek?: string
  specificDate?: string | null
  scheduledTime?: string
  customTemplate?: string | null
  bannerUrl?: string | null
  buttonText?: string | null
  buttonUrl?: string | null
  isActive?: boolean
}

export interface UpdateScheduledMessageRequest {
  title: string
  target: TelegramMessageTarget
  messageType: TelegramMessageType
  daysOfWeek: string
  specificDate: string | null
  scheduledTime: string
  customTemplate: string | null
  bannerUrl: string | null
  buttonText: string | null
  buttonUrl: string | null
  isActive: boolean
}

export interface TestSendRequest {
  targetOverride?: 'Channel' | 'BotDm'
  customChatId?: number | null
  sectorPersonaId?: string | null
}

export interface TestSendResponse {
  success: boolean
  message: string
  sentCount: number
  mode: string
  targetDestination: string
  renderedPreviewHtml?: string
}

export interface TestConfigResponse {
  testChannelId: string | null
  hasTestChannel: boolean
  testChatId: number | null
  hasTestChat: boolean
  messagingTestMode: boolean
  adminIsLinked: boolean
  adminChatId: number | null
  productionChannelId: string | null
}

/** GET /api/admin/telegram/messages — lists all scheduled messages */
export function fetchScheduledMessages(token: string): Promise<TelegramScheduledMessage[]> {
  return request<TelegramScheduledMessage[]>(API_ENDPOINTS.adminTelegram.messages, {
    headers: auth(token),
  })
}

/** GET /api/admin/telegram/messages/{id} */
export function fetchScheduledMessage(
  id: string,
  token: string,
): Promise<TelegramScheduledMessage> {
  return request<TelegramScheduledMessage>(API_ENDPOINTS.adminTelegram.messageDetail(id), {
    headers: auth(token),
  })
}

/** POST /api/admin/telegram/messages */
export function createScheduledMessage(
  req: CreateScheduledMessageRequest,
  token: string,
): Promise<TelegramScheduledMessage> {
  return request<TelegramScheduledMessage>(API_ENDPOINTS.adminTelegram.messages, {
    method: 'POST',
    headers: auth(token),
    body: req,
  })
}

/** PUT /api/admin/telegram/messages/{id} */
export function updateScheduledMessage(
  id: string,
  req: UpdateScheduledMessageRequest,
  token: string,
): Promise<TelegramScheduledMessage> {
  return request<TelegramScheduledMessage>(API_ENDPOINTS.adminTelegram.messageDetail(id), {
    method: 'PUT',
    headers: auth(token),
    body: req,
  })
}

/** PATCH /api/admin/telegram/messages/{id}/toggle */
export function toggleScheduledMessage(
  id: string,
  token: string,
): Promise<{ id: string; isActive: boolean }> {
  return request<{ id: string; isActive: boolean }>(
    API_ENDPOINTS.adminTelegram.messageToggle(id),
    {
      method: 'PATCH',
      headers: auth(token),
    },
  )
}

/** DELETE /api/admin/telegram/messages/{id} */
export function deleteScheduledMessage(id: string, token: string): Promise<void> {
  return request<void>(API_ENDPOINTS.adminTelegram.messageDetail(id), {
    method: 'DELETE',
    headers: auth(token),
  })
}

/** POST /api/admin/telegram/messages/{id}/test */
export function testSendScheduledMessage(
  id: string,
  req: TestSendRequest,
  token: string,
): Promise<TestSendResponse> {
  return request<TestSendResponse>(API_ENDPOINTS.adminTelegram.messageTest(id), {
    method: 'POST',
    headers: auth(token),
    body: req,
  })
}

/** GET /api/admin/telegram/messages/test-config */
export function fetchTelegramTestConfig(token: string): Promise<TestConfigResponse> {
  return request<TestConfigResponse>(API_ENDPOINTS.adminTelegram.testConfig, {
    headers: auth(token),
  })
}

/** POST /api/admin/telegram/messages/seed-defaults */
export function seedDefaultSchedules(
  token: string,
  force = false,
): Promise<TelegramScheduledMessage[]> {
  const url = force
    ? `${API_ENDPOINTS.adminTelegram.seedDefaults}?force=true`
    : API_ENDPOINTS.adminTelegram.seedDefaults
  return request<TelegramScheduledMessage[]>(url, {
    method: 'POST',
    headers: auth(token),
  })
}
