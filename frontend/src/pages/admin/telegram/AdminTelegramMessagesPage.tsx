import { useCallback, useEffect, useState } from 'react'
import {
  fetchScheduledMessages,
  createScheduledMessage,
  updateScheduledMessage,
  toggleScheduledMessage,
  deleteScheduledMessage,
  testSendScheduledMessage,
  fetchTelegramTestConfig,
  seedDefaultSchedules,
  fetchSectors,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import type {
  TelegramScheduledMessage,
  TelegramMessageTarget,
  TelegramMessageType,
  CreateScheduledMessageRequest,
  TestSendRequest,
  TestSendResponse,
  TestConfigResponse,
} from '../../../api'
import type { SectorResponse } from '../../../types'
import { useRequireRole } from '../../../hooks'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { cn } from '../../../lib/cn.ts'

const TARGET_LABELS: Record<TelegramMessageTarget, { label: string; icon: string; color: string }> = {
  Channel: { label: 'Channel', icon: 'campaign', color: 'bg-blue-50 text-blue-700 border-blue-200' },
  BotDm: { label: 'Bot DMs', icon: 'smart_toy', color: 'bg-purple-50 text-purple-700 border-purple-200' },
  Both: { label: 'Channel & Bot', icon: 'hub', color: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
}

const TYPE_CONFIG: Record<
  TelegramMessageType,
  { label: string; icon: string; color: string; defaultTime: string; desc: string }
> = {
  GoodMorning: {
    label: 'Good Morning',
    icon: 'wb_sunny',
    color: 'bg-amber-50 text-amber-700 border-amber-200',
    defaultTime: '08:00',
    desc: 'Yesterday job totals, top sectors, motivational quote, search link',
  },
  GoodAfternoon: {
    label: 'Good Afternoon',
    icon: 'light_mode',
    color: 'bg-orange-50 text-orange-700 border-orange-200',
    defaultTime: '13:00',
    desc: 'Morning pulse, fresh jobs count, midday application encouragement',
  },
  GoodNight: {
    label: 'Good Night',
    icon: 'nightlight',
    color: 'bg-indigo-50 text-indigo-700 border-indigo-200',
    defaultTime: '20:30',
    desc: 'Full day job recap, top 3-4 hiring sectors, evening reflection',
  },
  CustomAd: {
    label: 'Feature Spotlight',
    icon: 'auto_awesome',
    color: 'bg-pink-50 text-pink-700 border-pink-200',
    defaultTime: '11:30',
    desc: 'Promotes SeraGo bot, instant alerts, resume matching, and AI features',
  },
  CustomMessage: {
    label: 'Custom Message',
    icon: 'edit_note',
    color: 'bg-slate-50 text-slate-700 border-slate-200',
    defaultTime: '12:00',
    desc: 'Custom HTML template with dynamic placeholders',
  },
}

const DAYS_OF_WEEK = [
  { key: 'Mon', label: 'Mon' },
  { key: 'Tue', label: 'Tue' },
  { key: 'Wed', label: 'Wed' },
  { key: 'Thu', label: 'Thu' },
  { key: 'Fri', label: 'Fri' },
  { key: 'Sat', label: 'Sat' },
  { key: 'Sun', label: 'Sun' },
]

const PLACEHOLDERS = [
  { tag: '{totalJobs}', desc: 'Total jobs count' },
  { tag: '{yesterdayJobs}', desc: 'Jobs posted yesterday' },
  { tag: '{morningJobs}', desc: 'Jobs posted this morning' },
  { tag: '{topSectors}', desc: 'Formatted top sectors list' },
  { tag: '{date}', desc: 'Today date (e.g. Oct 23)' },
  { tag: '{quote}', desc: 'Daily inspirational quote' },
  { tag: '{userFirstName}', desc: 'Recipient first name' },
  { tag: '{sectorName}', desc: 'User sector name' },
  { tag: '{sectorCount}', desc: 'User sector job count' },
]

export default function AdminTelegramMessagesPage() {
  const auth = useRequireRole('Admin')
  const [messages, setMessages] = useState<TelegramScheduledMessage[]>([])
  const [testConfig, setTestConfig] = useState<TestConfigResponse | null>(null)
  const [sectors, setSectors] = useState<SectorResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [bannerNotice, setBannerNotice] = useState<string | null>(null)

  // Modals state
  const [testModalTarget, setTestModalTarget] = useState<TelegramScheduledMessage | null>(null)
  const [editModalMessage, setEditModalMessage] = useState<TelegramScheduledMessage | null>(null)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)
  const [seeding, setSeeding] = useState(false)

  // Test Modal specific state
  const [testDestination, setTestDestination] = useState<'channel' | 'myChat' | 'testChat' | 'custom'>(
    'channel',
  )
  const [customChatId, setCustomChatId] = useState('')
  const [selectedPersonaId, setSelectedPersonaId] = useState('')
  const [testSending, setTestSending] = useState(false)
  const [testResult, setTestResult] = useState<TestSendResponse | null>(null)

  // Edit / Create Form state
  const [formTitle, setFormTitle] = useState('')
  const [formTarget, setFormTarget] = useState<TelegramMessageTarget>('Both')
  const [formType, setFormType] = useState<TelegramMessageType>('GoodMorning')
  const [formDays, setFormDays] = useState<string[]>(['*'])
  const [formSpecificDate, setFormSpecificDate] = useState('')
  const [formScheduledTime, setFormScheduledTime] = useState('08:00')
  const [formCustomTemplate, setFormCustomTemplate] = useState('')
  const [formBannerUrl, setFormBannerUrl] = useState('')
  const [formButtonText, setFormButtonText] = useState('')
  const [formButtonUrl, setFormButtonUrl] = useState('')
  const [formIsActive, setFormIsActive] = useState(true)
  const [savingForm, setSavingForm] = useState(false)

  const loadData = useCallback(async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)

    try {
      const [msgsRes, cfgRes, secRes] = await Promise.allSettled([
        fetchScheduledMessages(tokens.accessToken),
        fetchTelegramTestConfig(tokens.accessToken),
        fetchSectors(tokens.accessToken),
      ])

      if (msgsRes.status === 'fulfilled') setMessages(msgsRes.value)
      if (cfgRes.status === 'fulfilled') setTestConfig(cfgRes.value)
      if (secRes.status === 'fulfilled') setSectors(secRes.value)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleToggle = async (message: TelegramScheduledMessage) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      const res = await toggleScheduledMessage(message.id, tokens.accessToken)
      setMessages((prev) =>
        prev.map((m) => (m.id === message.id ? { ...m, isActive: res.isActive } : m)),
      )
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
  }

  const handleDelete = async (id: string) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await deleteScheduledMessage(id, tokens.accessToken)
      setMessages((prev) => prev.filter((m) => m.id !== id))
      setDeleteConfirmId(null)
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
  }

  const handleSeedDefaults = async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setSeeding(true)
    try {
      const seeded = await seedDefaultSchedules(tokens.accessToken)
      setMessages(seeded)
      setBannerNotice('Standard Telegram schedules seeded successfully!')
      setTimeout(() => setBannerNotice(null), 5000)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setSeeding(false)
    }
  }

  // Open Test Modal
  const openTestModal = (msg: TelegramScheduledMessage) => {
    setTestModalTarget(msg)
    setTestResult(null)
    if (msg.target === 'BotDm') {
      setTestDestination(testConfig?.adminIsLinked ? 'myChat' : 'testChat')
    } else {
      setTestDestination('channel')
    }
    if (sectors.length > 0) {
      setSelectedPersonaId(sectors[0].id)
    }
  }

  const handleRunTest = async () => {
    if (!testModalTarget) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    setTestSending(true)
    setTestResult(null)

    const req: TestSendRequest = {}

    if (testDestination === 'channel') {
      req.targetOverride = 'Channel'
    } else {
      req.targetOverride = 'BotDm'
      if (testDestination === 'custom') {
        const parsed = parseInt(customChatId.trim(), 10)
        if (isNaN(parsed)) {
          setError('Please provide a valid numeric Chat ID.')
          setTestSending(false)
          return
        }
        req.customChatId = parsed
      } else if (testDestination === 'myChat' && testConfig?.adminChatId) {
        req.customChatId = testConfig.adminChatId
      } else if (testDestination === 'testChat' && testConfig?.testChatId) {
        req.customChatId = testConfig.testChatId
      }
    }

    if (selectedPersonaId) {
      req.sectorPersonaId = selectedPersonaId
    }

    try {
      const res = await testSendScheduledMessage(testModalTarget.id, req, tokens.accessToken)
      setTestResult(res)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setTestSending(false)
    }
  }

  // Open Create / Edit Form
  const openCreateModal = () => {
    setFormTitle('')
    setFormTarget('Both')
    setFormType('GoodMorning')
    setFormDays(['*'])
    setFormSpecificDate('')
    setFormScheduledTime('08:00')
    setFormCustomTemplate('')
    setFormBannerUrl('')
    setFormButtonText('')
    setFormButtonUrl('')
    setFormIsActive(true)
    setIsCreateOpen(true)
  }

  const openEditModal = (msg: TelegramScheduledMessage) => {
    setEditModalMessage(msg)
    setFormTitle(msg.title)
    setFormTarget(msg.target)
    setFormType(msg.messageType)
    try {
      const parsedDays = JSON.parse(msg.daysOfWeek)
      setFormDays(Array.isArray(parsedDays) ? parsedDays : ['*'])
    } catch {
      setFormDays(['*'])
    }
    setFormSpecificDate(msg.specificDate || '')
    setFormScheduledTime(msg.scheduledTime)
    setFormCustomTemplate(msg.customTemplate || '')
    setFormBannerUrl(msg.bannerUrl || '')
    setFormButtonText(msg.buttonText || '')
    setFormButtonUrl(msg.buttonUrl || '')
    setFormIsActive(msg.isActive)
    setIsCreateOpen(false)
  }

  const handleSaveForm = async (e: React.FormEvent) => {
    e.preventDefault()
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    setSavingForm(true)
    setError(null)

    const payload: CreateScheduledMessageRequest = {
      title: formTitle,
      target: formTarget,
      messageType: formType,
      daysOfWeek: JSON.stringify(formDays),
      specificDate: formSpecificDate.trim() || null,
      scheduledTime: formScheduledTime.trim() || '08:00',
      customTemplate: formCustomTemplate.trim() || null,
      bannerUrl: formBannerUrl.trim() || null,
      buttonText: formButtonText.trim() || null,
      buttonUrl: formButtonUrl.trim() || null,
      isActive: formIsActive,
    }

    try {
      if (editModalMessage) {
        const updated = await updateScheduledMessage(
          editModalMessage.id,
          payload as any,
          tokens.accessToken,
        )
        setMessages((prev) => prev.map((m) => (m.id === updated.id ? updated : m)))
        setEditModalMessage(null)
      } else {
        const created = await createScheduledMessage(payload, tokens.accessToken)
        setMessages((prev) => [...prev, created])
        setIsCreateOpen(false)
      }
      setBannerNotice('Schedule saved successfully.')
      setTimeout(() => setBannerNotice(null), 4000)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setSavingForm(false)
    }
  }

  const toggleDay = (day: string) => {
    if (day === '*') {
      setFormDays(['*'])
      return
    }
    let current = formDays.includes('*') ? [] : [...formDays]
    if (current.includes(day)) {
      current = current.filter((d) => d !== day)
      if (current.length === 0) current = ['*']
    } else {
      current.push(day)
    }
    setFormDays(current)
  }

  const insertPlaceholder = (tag: string) => {
    setFormCustomTemplate((prev) => `${prev}${tag}`)
  }

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span
          aria-hidden="true"
          className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
        />
      </div>
    )
  }

  // Stats calculation
  const totalSchedules = messages.length
  const activeSchedules = messages.filter((m) => m.isActive).length
  const channelSchedules = messages.filter((m) => m.target === 'Channel' || m.target === 'Both').length
  const botSchedules = messages.filter((m) => m.target === 'BotDm' || m.target === 'Both').length

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-3xl text-primary">send</span>
            <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
              Telegram Scheduled Messaging
            </h1>
          </div>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            Automate and broadcast intelligent morning, afternoon, night, and ad cards to your Telegram
            channel and personalized bot subscribers in East Africa Time (UTC+3).
          </p>
        </div>
        <div className="flex items-center gap-2">
          {messages.length === 0 && (
            <button
              onClick={handleSeedDefaults}
              disabled={seeding}
              className="inline-flex items-center gap-1.5 rounded-lg border border-primary/30 bg-primary/5 px-3.5 py-2 font-label-md text-label-md font-semibold text-primary transition-all hover:bg-primary/10 active:scale-95"
            >
              <span className="material-symbols-outlined text-lg">auto_fix_high</span>
              {seeding ? 'Seeding...' : 'Seed Defaults'}
            </button>
          )}
          <button
            onClick={openCreateModal}
            className="inline-flex items-center gap-1.5 rounded-lg bg-primary px-4 py-2 font-label-md text-label-md font-semibold text-on-primary shadow-sm transition-all hover:bg-primary/90 active:scale-95"
          >
            <span className="material-symbols-outlined text-lg">add</span>
            New Schedule
          </button>
        </div>
      </div>

      {/* Notifications */}
      {error && (
        <div
          role="alert"
          className="mt-4 flex items-center justify-between rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          <span>{error}</span>
          <button onClick={() => setError(null)} className="text-on-error-container hover:opacity-75">
            <span className="material-symbols-outlined text-sm">close</span>
          </button>
        </div>
      )}

      {bannerNotice && (
        <div
          role="status"
          className="mt-4 flex items-center justify-between rounded-xl border border-emerald-500/30 bg-emerald-50 px-4 py-3 font-label-md text-label-md text-emerald-800"
        >
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-emerald-600">check_circle</span>
            <span>{bannerNotice}</span>
          </div>
          <button onClick={() => setBannerNotice(null)} className="text-emerald-800 hover:opacity-75">
            <span className="material-symbols-outlined text-sm">close</span>
          </button>
        </div>
      )}

      {/* Testing & Isolation Banner */}
      <div className="mt-5 rounded-2xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:p-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex items-start gap-3">
            <div
              className={cn(
                'flex h-10 w-10 shrink-0 items-center justify-center rounded-xl',
                testConfig?.messagingTestMode
                  ? 'bg-emerald-100 text-emerald-700'
                  : 'bg-amber-100 text-amber-800',
              )}
            >
              <span className="material-symbols-outlined text-2xl">
                {testConfig?.messagingTestMode ? 'verified_user' : 'sensors'}
              </span>
            </div>
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-title-md text-title-md font-bold text-on-surface">
                  {testConfig?.messagingTestMode
                    ? 'Isolated Test Mode Active'
                    : 'Live Production Dispatch Mode'}
                </span>
                <span
                  className={cn(
                    'rounded-full px-2.5 py-0.5 text-xs font-semibold uppercase tracking-wider',
                    testConfig?.messagingTestMode
                      ? 'bg-emerald-100 text-emerald-800'
                      : 'bg-amber-100 text-amber-900',
                  )}
                >
                  {testConfig?.messagingTestMode ? 'Test Mode' : 'Production'}
                </span>
              </div>
              <p className="mt-0.5 font-body-sm text-body-sm text-on-surface-variant">
                {testConfig?.messagingTestMode
                  ? 'Broadcasts are safely trapped and redirected exclusively to your designated test channel or testing bot ID.'
                  : 'Scheduled jobs run against live subscribers and the production channel. Use the interactive "Test Send" button to preview safely.'}
              </p>
            </div>
          </div>

          <div className="flex flex-wrap gap-2 text-xs">
            <div className="rounded-lg border border-surface-variant bg-surface-container-low px-3 py-1.5">
              <span className="font-medium text-on-surface-variant">Test Channel: </span>
              <span className="font-mono font-semibold text-on-surface">
                {testConfig?.testChannelId || 'Not configured'}
              </span>
            </div>
            <div className="rounded-lg border border-surface-variant bg-surface-container-low px-3 py-1.5">
              <span className="font-medium text-on-surface-variant">Test Chat: </span>
              <span className="font-mono font-semibold text-on-surface">
                {testConfig?.testChatId || 'Not configured'}
              </span>
            </div>
            {testConfig?.adminIsLinked && (
              <div className="rounded-lg border border-purple-200 bg-purple-50 px-3 py-1.5 text-purple-700">
                <span className="font-medium">My Admin Bot: </span>
                <span className="font-mono font-semibold">Linked ({testConfig.adminChatId})</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Stat Cards */}
      <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="font-label-md text-label-md text-on-surface-variant">Total Schedules</span>
            <span className="material-symbols-outlined text-primary">schedule</span>
          </div>
          <div className="mt-2 font-headline-md text-headline-md font-bold text-on-surface">
            {totalSchedules}
          </div>
        </div>

        <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="font-label-md text-label-md text-on-surface-variant">Active Broadcasts</span>
            <span className="material-symbols-outlined text-emerald-600">check_circle</span>
          </div>
          <div className="mt-2 font-headline-md text-headline-md font-bold text-emerald-600">
            {activeSchedules}
          </div>
        </div>

        <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="font-label-md text-label-md text-on-surface-variant">Channel Schedules</span>
            <span className="material-symbols-outlined text-blue-600">campaign</span>
          </div>
          <div className="mt-2 font-headline-md text-headline-md font-bold text-blue-600">
            {channelSchedules}
          </div>
        </div>

        <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="font-label-md text-label-md text-on-surface-variant">Personalized Bot DMs</span>
            <span className="material-symbols-outlined text-purple-600">smart_toy</span>
          </div>
          <div className="mt-2 font-headline-md text-headline-md font-bold text-purple-600">
            {botSchedules}
          </div>
        </div>
      </div>

      {/* Schedules List */}
      <div className="mt-6">
        <div className="flex items-center justify-between">
          <h2 className="font-title-lg text-title-lg font-bold text-on-surface">Configured Broadcasts</h2>
          <span className="font-label-sm text-label-sm text-on-surface-variant">
            Scheduled in East Africa Time (EAT / UTC+3)
          </span>
        </div>

        {loading ? (
          <div className="mt-8 flex items-center justify-center">
            <span
              aria-hidden="true"
              className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
            />
          </div>
        ) : messages.length === 0 ? (
          <div className="mt-6 flex flex-col items-center justify-center rounded-2xl border border-dashed border-surface-variant bg-surface-container-lowest py-12 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary">
              <span className="material-symbols-outlined text-3xl">mark_chat_unread</span>
            </div>
            <h3 className="mt-3 font-title-md text-title-md font-semibold text-on-surface">
              No Scheduled Messages Yet
            </h3>
            <p className="mt-1 max-w-md font-body-sm text-body-sm text-on-surface-variant">
              Get started instantly by seeding standard Good Morning, Good Afternoon, Good Night, and
              Feature Spotlight schedules.
            </p>
            <button
              onClick={handleSeedDefaults}
              disabled={seeding}
              className="mt-4 inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 font-label-md text-label-md font-semibold text-on-primary shadow-sm hover:bg-primary/90"
            >
              <span className="material-symbols-outlined text-lg">auto_fix_high</span>
              {seeding ? 'Seeding Default Schedules...' : 'Seed Standard Schedules'}
            </button>
          </div>
        ) : (
          <div className="mt-4 space-y-3">
            {messages.map((msg) => {
              const targetMeta = TARGET_LABELS[msg.target] || TARGET_LABELS.Both
              const typeMeta = TYPE_CONFIG[msg.messageType] || TYPE_CONFIG.CustomMessage
              let parsedDays: string[] = []
              try {
                parsedDays = JSON.parse(msg.daysOfWeek)
              } catch {
                parsedDays = ['*']
              }
              const isEveryDay = parsedDays.includes('*')

              return (
                <div
                  key={msg.id}
                  className={cn(
                    'group relative flex flex-col gap-4 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm transition-all hover:border-primary/40 sm:flex-row sm:items-center sm:justify-between sm:p-5',
                    !msg.isActive && 'opacity-65 bg-surface-container-low/40',
                  )}
                >
                  {/* Left: Info */}
                  <div className="flex flex-1 items-start gap-3.5">
                    <div
                      className={cn(
                        'flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border',
                        typeMeta.color,
                      )}
                    >
                      <span className="material-symbols-outlined text-2xl">{typeMeta.icon}</span>
                    </div>

                    <div className="space-y-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="font-title-md text-title-md font-bold text-on-surface">
                          {msg.title}
                        </span>

                        {/* Target badge */}
                        <span
                          className={cn(
                            'inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-semibold',
                            targetMeta.color,
                          )}
                        >
                          <span className="material-symbols-outlined text-xs">{targetMeta.icon}</span>
                          {targetMeta.label}
                        </span>

                        {/* Type badge */}
                        <span
                          className={cn(
                            'inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-semibold',
                            typeMeta.color,
                          )}
                        >
                          {typeMeta.label}
                        </span>

                        {/* Active status pill */}
                        <span
                          className={cn(
                            'rounded-full px-2 py-0.5 text-xs font-medium',
                            msg.isActive
                              ? 'bg-emerald-100 text-emerald-800'
                              : 'bg-surface-container text-on-surface-variant',
                          )}
                        >
                          {msg.isActive ? 'Active' : 'Paused'}
                        </span>
                      </div>

                      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-on-surface-variant">
                        {/* Time */}
                        <span className="inline-flex items-center gap-1 font-semibold text-on-surface">
                          <span className="material-symbols-outlined text-sm text-primary">schedule</span>
                          {msg.scheduledTime} EAT
                        </span>

                        {/* Days */}
                        <span className="inline-flex items-center gap-1">
                          <span className="material-symbols-outlined text-sm">calendar_today</span>
                          {msg.specificDate ? (
                            <span className="font-medium text-accent">Specific: {msg.specificDate}</span>
                          ) : isEveryDay ? (
                            'Every day'
                          ) : (
                            parsedDays.join(', ')
                          )}
                        </span>

                        {/* Last Run */}
                        {msg.lastRunUtc && (
                          <span className="inline-flex items-center gap-1">
                            <span className="material-symbols-outlined text-sm">history</span>
                            Last run: {new Date(msg.lastRunUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} ({msg.lastRunStatus || 'OK'})
                          </span>
                        )}
                      </div>

                      {msg.customTemplate && (
                        <p className="line-clamp-1 font-mono text-xs text-on-surface-variant/80">
                          Template: {msg.customTemplate}
                        </p>
                      )}
                    </div>
                  </div>

                  {/* Right: Actions */}
                  <div className="flex items-center gap-2 self-end sm:self-center">
                    {/* Test Send Button */}
                    <button
                      onClick={() => openTestModal(msg)}
                      className="inline-flex items-center gap-1 rounded-lg border border-primary/30 bg-primary/10 px-3 py-1.5 font-label-md text-label-md font-semibold text-primary transition-all hover:bg-primary/20 active:scale-95"
                      title="Test send this message immediately"
                    >
                      <span className="material-symbols-outlined text-base">rocket_launch</span>
                      Test
                    </button>

                    {/* Toggle Active */}
                    <button
                      onClick={() => handleToggle(msg)}
                      className={cn(
                        'inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 font-label-md text-label-md font-medium transition-all',
                        msg.isActive
                          ? 'border-surface-variant bg-surface-container-low text-on-surface hover:bg-surface-container'
                          : 'border-emerald-300 bg-emerald-50 text-emerald-800 hover:bg-emerald-100',
                      )}
                    >
                      <span className="material-symbols-outlined text-base">
                        {msg.isActive ? 'pause' : 'play_arrow'}
                      </span>
                      {msg.isActive ? 'Pause' : 'Activate'}
                    </button>

                    {/* Edit */}
                    <button
                      onClick={() => openEditModal(msg)}
                      className="rounded-lg border border-surface-variant p-1.5 text-on-surface-variant transition-all hover:border-primary hover:text-primary active:scale-95"
                      title="Edit schedule"
                    >
                      <span className="material-symbols-outlined text-lg">edit</span>
                    </button>

                    {/* Delete */}
                    {deleteConfirmId === msg.id ? (
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => handleDelete(msg.id)}
                          className="rounded-lg bg-error px-2 py-1 font-label-sm text-label-sm font-semibold text-on-error"
                        >
                          Confirm
                        </button>
                        <button
                          onClick={() => setDeleteConfirmId(null)}
                          className="rounded-lg border border-surface-variant px-2 py-1 font-label-sm text-label-sm text-on-surface-variant"
                        >
                          Cancel
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => setDeleteConfirmId(msg.id)}
                        className="rounded-lg border border-surface-variant p-1.5 text-on-surface-variant transition-all hover:border-error hover:text-error active:scale-95"
                        title="Delete schedule"
                      >
                        <span className="material-symbols-outlined text-lg">delete</span>
                      </button>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>

      {/* ─────────────────────────────────────────────────────────────────────────
          MODAL: Interactive Test Send
          ───────────────────────────────────────────────────────────────────────── */}
      {testModalTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
          <div className="w-full max-w-2xl rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-surface-variant pb-4">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-2xl text-primary">rocket_launch</span>
                <h3 className="font-title-lg text-title-lg font-bold text-on-surface">
                  Test Send: {testModalTarget.title}
                </h3>
              </div>
              <button
                onClick={() => setTestModalTarget(null)}
                className="text-on-surface-variant hover:text-on-surface"
              >
                <span className="material-symbols-outlined">close</span>
              </button>
            </div>

            <div className="mt-4 space-y-4">
              <div>
                <label className="font-label-md text-label-md font-semibold text-on-surface">
                  Choose Destination
                </label>
                <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4">
                  <button
                    type="button"
                    onClick={() => setTestDestination('channel')}
                    className={cn(
                      'flex flex-col items-center gap-1 rounded-xl border p-3 text-center transition-all',
                      testDestination === 'channel'
                        ? 'border-primary bg-primary/10 text-primary font-bold'
                        : 'border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container',
                    )}
                  >
                    <span className="material-symbols-outlined text-2xl">campaign</span>
                    <span className="text-xs">Test Channel</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setTestDestination('myChat')}
                    disabled={!testConfig?.adminIsLinked}
                    className={cn(
                      'flex flex-col items-center gap-1 rounded-xl border p-3 text-center transition-all',
                      testDestination === 'myChat'
                        ? 'border-purple-600 bg-purple-50 text-purple-700 font-bold'
                        : 'border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container',
                      !testConfig?.adminIsLinked && 'opacity-40 cursor-not-allowed',
                    )}
                  >
                    <span className="material-symbols-outlined text-2xl">person</span>
                    <span className="text-xs">
                      {testConfig?.adminIsLinked ? 'My Telegram DM' : 'My Bot (Unlinked)'}
                    </span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setTestDestination('testChat')}
                    disabled={!testConfig?.hasTestChat}
                    className={cn(
                      'flex flex-col items-center gap-1 rounded-xl border p-3 text-center transition-all',
                      testDestination === 'testChat'
                        ? 'border-blue-600 bg-blue-50 text-blue-700 font-bold'
                        : 'border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container',
                      !testConfig?.hasTestChat && 'opacity-40 cursor-not-allowed',
                    )}
                  >
                    <span className="material-symbols-outlined text-2xl">smart_toy</span>
                    <span className="text-xs">Env Test Chat</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setTestDestination('custom')}
                    className={cn(
                      'flex flex-col items-center gap-1 rounded-xl border p-3 text-center transition-all',
                      testDestination === 'custom'
                        ? 'border-accent bg-accent/10 text-accent font-bold'
                        : 'border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container',
                    )}
                  >
                    <span className="material-symbols-outlined text-2xl">pin</span>
                    <span className="text-xs">Custom Chat ID</span>
                  </button>
                </div>
              </div>

              {testDestination === 'custom' && (
                <div>
                  <label className="font-label-md text-label-md font-medium text-on-surface">
                    Target Telegram Chat ID
                  </label>
                  <input
                    type="text"
                    value={customChatId}
                    onChange={(e) => setCustomChatId(e.target.value)}
                    placeholder="e.g. 123456789"
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-mono text-sm text-on-surface focus:border-primary focus:outline-none"
                  />
                  <span className="mt-1 block text-xs text-on-surface-variant">
                    Send a message to your bot or @userinfobot to find your Telegram Chat ID.
                  </span>
                </div>
              )}

              {/* Persona Selector for personalized stats */}
              {(testDestination !== 'channel' || testModalTarget.target === 'BotDm') && (
                <div>
                  <label className="font-label-md text-label-md font-medium text-on-surface">
                    Simulate User Sector Persona (For Tailored Job Stats)
                  </label>
                  <select
                    value={selectedPersonaId}
                    onChange={(e) => setSelectedPersonaId(e.target.value)}
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  >
                    <option value="">Default (All Sectors / General)</option>
                    {sectors.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.name}
                      </option>
                    ))}
                  </select>
                  <span className="mt-1 block text-xs text-on-surface-variant">
                    Tests how user DMs dynamically show sector-specific job counts and greetings for talent in
                    that field.
                  </span>
                </div>
              )}

              {/* Action Button */}
              <div className="pt-2">
                <button
                  type="button"
                  onClick={handleRunTest}
                  disabled={testSending}
                  className="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-primary py-2.5 font-label-lg text-label-lg font-bold text-on-primary shadow-md transition-all hover:bg-primary/95 disabled:opacity-50"
                >
                  <span className="material-symbols-outlined">
                    {testSending ? 'hourglass_top' : 'send'}
                  </span>
                  {testSending ? 'Rendering & Dispatching to Telegram...' : 'Send Test Broadcast Now'}
                </button>
              </div>

              {/* Live Result & Preview */}
              {testResult && (
                <div className="mt-4 rounded-xl border border-emerald-300 bg-emerald-50/70 p-4">
                  <div className="flex items-center gap-2 font-title-sm text-title-sm font-bold text-emerald-800">
                    <span className="material-symbols-outlined text-emerald-600">check_circle</span>
                    {testResult.message}
                  </div>
                  <div className="mt-1 flex flex-wrap gap-2 text-xs text-emerald-900">
                    <span>Target: <b>{testResult.targetDestination}</b></span>
                    <span>•</span>
                    <span>Mode: <b>{testResult.mode}</b></span>
                    <span>•</span>
                    <span>Sent: <b>{testResult.sentCount} message(s)</b></span>
                  </div>

                  {testResult.renderedPreviewHtml && (
                    <div className="mt-3">
                      <span className="font-label-sm text-label-sm font-semibold uppercase tracking-wider text-emerald-900">
                        Rendered Telegram Card Preview
                      </span>
                      <div className="mt-1.5 max-w-md rounded-2xl bg-[#1e293b] p-4 text-white shadow-lg">
                        <div
                          className="space-y-2 text-sm leading-relaxed text-slate-100"
                          dangerouslySetInnerHTML={{ __html: testResult.renderedPreviewHtml }}
                        />
                        {testModalTarget.buttonText && (
                          <div className="mt-3 pt-2 border-t border-slate-700">
                            <div className="rounded-lg bg-sky-500/20 py-1.5 text-center font-semibold text-xs text-sky-300">
                              🔗 {testModalTarget.buttonText}
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ─────────────────────────────────────────────────────────────────────────
          MODAL: Create / Edit Schedule
          ───────────────────────────────────────────────────────────────────────── */}
      {(isCreateOpen || editModalMessage) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm overflow-y-auto">
          <div className="w-full max-w-2xl rounded-2xl border border-surface-variant bg-surface-container-lowest p-6 shadow-2xl my-8">
            <div className="flex items-center justify-between border-b border-surface-variant pb-4">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-2xl text-primary">
                  {editModalMessage ? 'edit_calendar' : 'add_circle'}
                </span>
                <h3 className="font-title-lg text-title-lg font-bold text-on-surface">
                  {editModalMessage ? 'Edit Scheduled Broadcast' : 'Create Scheduled Broadcast'}
                </h3>
              </div>
              <button
                onClick={() => {
                  setIsCreateOpen(false)
                  setEditModalMessage(null)
                }}
                className="text-on-surface-variant hover:text-on-surface"
              >
                <span className="material-symbols-outlined">close</span>
              </button>
            </div>

            <form onSubmit={handleSaveForm} className="mt-4 space-y-4">
              {/* Title */}
              <div>
                <label className="font-label-md text-label-md font-semibold text-on-surface">
                  Schedule Title *
                </label>
                <input
                  type="text"
                  required
                  value={formTitle}
                  onChange={(e) => setFormTitle(e.target.value)}
                  placeholder="e.g. Daily Morning Pulse"
                  className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                />
              </div>

              {/* Target & Type */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="font-label-md text-label-md font-semibold text-on-surface">
                    Target Recipient
                  </label>
                  <select
                    value={formTarget}
                    onChange={(e) => setFormTarget(e.target.value as TelegramMessageTarget)}
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  >
                    <option value="Both">Both Channel & Bot Subscribers</option>
                    <option value="Channel">Telegram Channel Only</option>
                    <option value="BotDm">Bot Subscribers (Direct Messages)</option>
                  </select>
                </div>

                <div>
                  <label className="font-label-md text-label-md font-semibold text-on-surface">
                    Message Type
                  </label>
                  <select
                    value={formType}
                    onChange={(e) => {
                      const t = e.target.value as TelegramMessageType
                      setFormType(t)
                      if (TYPE_CONFIG[t]?.defaultTime) {
                        setFormScheduledTime(TYPE_CONFIG[t].defaultTime)
                      }
                    }}
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  >
                    <option value="GoodMorning">Good Morning (Yesterday stats & tip)</option>
                    <option value="GoodAfternoon">Good Afternoon (Morning stats & pulse)</option>
                    <option value="GoodNight">Good Night (Full day recap & top sectors)</option>
                    <option value="CustomAd">Feature Spotlight / Promotional Ad</option>
                    <option value="CustomMessage">Custom Template</option>
                  </select>
                </div>
              </div>

              {/* Timing */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="font-label-md text-label-md font-semibold text-on-surface">
                    Execution Time (East Africa Time / UTC+3) *
                  </label>
                  <input
                    type="time"
                    required
                    value={formScheduledTime}
                    onChange={(e) => setFormScheduledTime(e.target.value)}
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  />
                  <span className="mt-1 block text-xs text-on-surface-variant">
                    Ethiopia Standard Time (EAT / UTC+3)
                  </span>
                </div>

                <div>
                  <label className="font-label-md text-label-md font-semibold text-on-surface">
                    One-off Specific Date (Optional)
                  </label>
                  <input
                    type="date"
                    value={formSpecificDate}
                    onChange={(e) => setFormSpecificDate(e.target.value)}
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  />
                  <span className="mt-1 block text-xs text-on-surface-variant">
                    Leave blank to run on recurring days of the week.
                  </span>
                </div>
              </div>

              {/* Days of Week */}
              <div>
                <div className="flex items-center justify-between">
                  <label className="font-label-md text-label-md font-semibold text-on-surface">
                    Recurring Days of the Week
                  </label>
                  <button
                    type="button"
                    onClick={() => toggleDay('*')}
                    className="text-xs text-primary font-medium hover:underline"
                  >
                    {formDays.includes('*') ? 'Select Specific Days' : 'Set to Every Day'}
                  </button>
                </div>
                <div className="mt-2 flex flex-wrap gap-2">
                  <button
                    type="button"
                    onClick={() => toggleDay('*')}
                    className={cn(
                      'rounded-lg px-3 py-1.5 text-xs font-semibold transition-all',
                      formDays.includes('*')
                        ? 'bg-primary text-on-primary'
                        : 'border border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container',
                    )}
                  >
                    Every Day (*)
                  </button>
                  {DAYS_OF_WEEK.map((d) => {
                    const active = !formDays.includes('*') && formDays.includes(d.key)
                    return (
                      <button
                        key={d.key}
                        type="button"
                        onClick={() => toggleDay(d.key)}
                        className={cn(
                          'rounded-lg px-3 py-1.5 text-xs font-semibold transition-all',
                          active
                            ? 'bg-primary text-on-primary'
                            : 'border border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container',
                        )}
                      >
                        {d.label}
                      </button>
                    )
                  })}
                </div>
              </div>

              {/* Action Button & Banner URL */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="font-label-md text-label-md font-medium text-on-surface">
                    Inline Button Text (Optional)
                  </label>
                  <input
                    type="text"
                    value={formButtonText}
                    onChange={(e) => setFormButtonText(e.target.value)}
                    placeholder="e.g. 🔍 Explore Jobs Now"
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  />
                </div>

                <div>
                  <label className="font-label-md text-label-md font-medium text-on-surface">
                    Inline Button URL (Optional)
                  </label>
                  <input
                    type="url"
                    value={formButtonUrl}
                    onChange={(e) => setFormButtonUrl(e.target.value)}
                    placeholder="https://t.me/serago_bot or https://serago.et"
                    className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                  />
                </div>
              </div>

              <div>
                <label className="font-label-md text-label-md font-medium text-on-surface">
                  Banner Image URL (Optional)
                </label>
                <input
                  type="text"
                  value={formBannerUrl}
                  onChange={(e) => setFormBannerUrl(e.target.value)}
                  placeholder="https://example.com/banner.jpg or local relative path"
                  className="mt-1 w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 text-sm text-on-surface focus:border-primary focus:outline-none"
                />
              </div>

              {/* Custom Template Override */}
              <div>
                <label className="font-label-md text-label-md font-semibold text-on-surface">
                  Custom Template (HTML Format, Optional)
                </label>
                <p className="text-xs text-on-surface-variant">
                  Leave empty to use SeraGo's built-in intelligent dynamic card builder with fresh stats
                  and quotes.
                </p>

                {/* Placeholders chips */}
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {PLACEHOLDERS.map((p) => (
                    <button
                      key={p.tag}
                      type="button"
                      onClick={() => insertPlaceholder(p.tag)}
                      className="rounded-md border border-surface-variant bg-surface-container-low px-2 py-0.5 font-mono text-xs text-primary transition-all hover:bg-primary/10"
                      title={p.desc}
                    >
                      +{p.tag}
                    </button>
                  ))}
                </div>

                <textarea
                  rows={4}
                  value={formCustomTemplate}
                  onChange={(e) => setFormCustomTemplate(e.target.value)}
                  placeholder="<b>Good Morning, Ethiopia!</b> ☀️&#10;&#10;Yesterday we recorded {yesterdayJobs} fresh vacancies across {topSectors}..."
                  className="mt-2 w-full rounded-lg border border-outline-variant bg-surface-container-lowest p-3 font-mono text-xs text-on-surface focus:border-primary focus:outline-none"
                />
              </div>

              {/* Active Toggle */}
              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="formActiveCheck"
                  checked={formIsActive}
                  onChange={(e) => setFormIsActive(e.target.checked)}
                  className="h-4 w-4 rounded border-outline-variant text-primary focus:ring-primary"
                />
                <label
                  htmlFor="formActiveCheck"
                  className="font-label-md text-label-md font-medium text-on-surface cursor-pointer"
                >
                  Enable schedule immediately upon saving
                </label>
              </div>

              {/* Submit Buttons */}
              <div className="flex items-center justify-end gap-2 border-t border-surface-variant pt-4">
                <button
                  type="button"
                  onClick={() => {
                    setIsCreateOpen(false)
                    setEditModalMessage(null)
                  }}
                  className="rounded-lg border border-surface-variant px-4 py-2 font-label-md text-label-md text-on-surface-variant hover:bg-surface-container"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={savingForm}
                  className="inline-flex items-center gap-1.5 rounded-lg bg-primary px-5 py-2 font-label-md text-label-md font-bold text-on-primary shadow-sm hover:bg-primary/95 disabled:opacity-50"
                >
                  <span className="material-symbols-outlined text-base">save</span>
                  {savingForm ? 'Saving...' : editModalMessage ? 'Update Schedule' : 'Create Schedule'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </DashboardShell>
  )
}
