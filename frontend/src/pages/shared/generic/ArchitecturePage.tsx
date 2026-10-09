import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import useSWR from 'swr'
import { request } from '../../../api/client'

export type ArchitectureStatsResponse = {
  scraper: {
    status: string
    runCount: number
    totalApiHits: number
    totalItemsFound: number
    totalItemsInserted: number
    totalItemsUpdated: number
    totalItemsSkipped: number
    recentRuns: Array<{
      api_hits: number
      time: string
      found: number
      stable_websites_status: string
      skipped: number
      updated: number
      inserted: number
      unstable_websites_status: string
    }>
  }
  database: {
    activeJobs: number
    totalJobViews: number
    totalUsers: number
    viewsToday: number
    viewsThisWeek: number
    viewsThisMonth: number
  }
  recentAiClassifications: Array<{
    jobTitle: string
    sectorBefore: string
    sectorAfter: string
    reasoning: string
  }>
}

const fetcher = (url: string) => request<ArchitectureStatsResponse>(url)

export default function ArchitecturePage() {
  const { data, mutate, isValidating } = useSWR('/architecture/stats', fetcher, {
    revalidateOnFocus: false
  })

  const systems = [
    {
      id: 'frontend',
      title: 'SeraGo Web Client',
      icon: 'devices',
      description: 'React + Vite Single Page Application served with Tailwind CSS.',
      statLabel: 'Views This Month',
      statValue: data?.database.viewsThisMonth?.toLocaleString() || '...',
      badge: 'Serago frontend'
    },
    {
      id: 'backend',
      title: 'Core .NET API',
      icon: 'terminal',
      description: 'C# .NET 8 Backend powering authentication, business logic, and job management.',
      statLabel: 'Active Jobs',
      statValue: data?.database.activeJobs?.toLocaleString() || '...',
      badge: 'Serago Backend #1'
    },
    {
      id: 'ai-service',
      title: 'AI Engine',
      icon: 'smart_toy',
      description: 'Django-based local intelligence orchestrating Ollama for resume parsing, job normalization, and AI cover letters.',
      statLabel: 'AI Engine',
      statValue: 'Active',
      badge: 'Ai Django Backend #2'
    },
    {
      id: 'ai-matching',
      title: 'AI Semantic Job Matching',
      icon: 'join',
      description: 'pgvector-based cosine similarity search for personalized job feeds.',
      statLabel: 'Search Type',
      statValue: 'Vector Math',
      badge: 'Ai Django Backend #2'
    },
    {
      id: 'scraper',
      title: 'Data Scraper',
      icon: 'public',
      description: 'Native Django pipeline pulling external jobs and synchronizing via .NET.',
      statLabel: 'Runs Today',
      statValue: data?.scraper.runCount?.toLocaleString() || '...',
      badge: 'Scraper Django backend #3'
    },
    {
      id: 'firewall-nginx',
      title: 'Firewall & Edge',
      icon: 'security',
      description: 'UFW and NGINX handling incoming traffic and SSL termination.',
      statLabel: 'Edge Status',
      statValue: 'Online'
    },
    {
      id: 'database',
      title: 'VPS PostgreSQL DB',
      icon: 'database',
      description: 'Self-hosted PostgreSQL on our VPS storing the primary state of SeraGo.',
      statLabel: 'Status',
      statValue: 'Active'
    },
    {
      id: 'ci-cd',
      title: 'CI/CD Pipeline',
      icon: 'rocket_launch',
      description: 'GitHub Actions powering automated testing and SSH deployments.',
      statLabel: 'Last Deploy',
      statValue: 'Success'
    },
    {
      id: 'cloud-storage',
      title: 'Cloud Storage',
      icon: 'cloud',
      description: 'Cloudflare R2 for secure, scalable resume and avatar storage.',
      statLabel: 'Storage Mode',
      statValue: 'R2 Buckets'
    },
    {
      id: 'email-service',
      title: 'Email Service',
      icon: 'mail',
      description: 'Reliable background worker for delivering critical account notifications.',
      statLabel: 'Delivery',
      statValue: 'SMTP'
    },
    {
      id: 'redis-signalr',
      title: 'Redis & WebSockets',
      icon: 'memory',
      description: 'Redis 7 handling the SignalR backplane for real-time events.',
      statLabel: 'Cache State',
      statValue: 'Hot'
    },
    {
      id: 'process-management',
      title: 'Process Manager',
      icon: 'memory_alt',
      description: 'Ubuntu native systemd ensuring high availability for all services.',
      statLabel: 'Daemon',
      statValue: 'systemd'
    },
    {
      id: 'security-hardening',
      title: 'Security Hardening',
      icon: 'shield',
      description: 'Aggressive edge-level filtering for malicious traffic.',
      statLabel: 'Bot Traffic',
      statValue: 'Blocked'
    },
    {
      id: 'ssl-encryption',
      title: 'SSL Encryption',
      icon: 'lock',
      description: 'Automated HTTPS encryption via Let\'s Encrypt and Certbot.',
      statLabel: 'Certificate',
      statValue: 'Secured'
    },
    {
      id: 'log-retention',
      title: 'Log Observability',
      icon: 'manage_search',
      description: 'Centralized logging preventing disk exhaustion.',
      statLabel: 'Log Rotate',
      statValue: 'Enabled'
    }
  ]

  return (
    <div className="min-h-screen bg-surface px-4 py-12 font-sans md:px-8 lg:px-12">
      <div className="mx-auto max-w-[1400px]">
        {/* Header */}
        <motion.div 
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-16 flex flex-col md:flex-row items-center justify-between gap-6"
        >
          <div className="text-left">
            <h1 className="font-display-lg text-primary tracking-tight">System Architecture</h1>
            <p className="font-body-lg text-secondary mt-2 max-w-2xl">
              Explore the complete lifecycle of SeraGo. From the edge firewall down to the AI 
              classification engine. Select a system below for a deep dive.
            </p>
          </div>
          
          <button
            onClick={() => mutate()}
            disabled={isValidating}
            className="flex items-center gap-2 bg-surface-container-lowest hover:bg-surface-container-low text-primary px-4 py-2.5 rounded-lg font-label-md border border-outline-variant transition-all shadow-sm active:scale-95 disabled:opacity-70 disabled:cursor-not-allowed"
          >
            <span className={`material-symbols-outlined text-[20px] ${isValidating ? 'animate-spin' : ''}`}>
              sync
            </span>
            {isValidating ? 'Refreshing...' : 'Refresh Live Stats'}
          </button>
        </motion.div>

        {/* Grid */}
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {systems.map((sys, idx) => (
            <motion.div
              key={sys.id}
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: idx * 0.1 }}
              className="bg-surface-container-lowest border-outline-variant relative flex flex-col justify-between rounded-2xl border p-6 shadow-sm hover:border-primary/50 hover:shadow-md transition-all group overflow-hidden"
            >
              {sys.badge && (
                <div className="absolute right-0 top-6 bg-accent text-on-accent px-4 py-1.5 rounded-l-full text-[10px] uppercase font-bold tracking-wider shadow-md">
                  {sys.badge}
                </div>
              )}
              
              <div className="mt-2">
                <div className="flex items-center gap-4 mb-4">
                  <div className="rounded-full bg-surface-container-low p-3 border border-outline-variant group-hover:border-primary/50 transition-colors">
                    <span className="material-symbols-outlined text-3xl text-primary">{sys.icon}</span>
                  </div>
                  <h2 className="font-headline-md text-on-surface">{sys.title}</h2>
                </div>
                <p className="font-body-md text-secondary mb-6 h-12">
                  {sys.description}
                </p>
              </div>

              <div className="flex items-end justify-between border-t border-surface-variant pt-4 mt-auto">
                <div>
                  <div className="text-xs text-secondary uppercase tracking-widest mb-1">{sys.statLabel}</div>
                  <div className="font-label-lg text-on-surface">{sys.statValue}</div>
                </div>
                <Link 
                  to={`/architecture/${sys.id}`}
                  className="bg-primary/10 text-primary hover:bg-primary hover:text-on-primary rounded px-4 py-2 font-label-md transition-colors flex items-center gap-2 z-10"
                >
                  Deep Dive
                  <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
                </Link>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  )
}
