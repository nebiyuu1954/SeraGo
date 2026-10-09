import { useParams, Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import useSWR from 'swr'
import { request } from '../../../api/client'
import type { ArchitectureStatsResponse } from './ArchitecturePage'

type SystemDetails = {
  title: string
  icon: string
  description: string
  technologies: string[]
  deepDive: React.ReactNode
}

const getSystems = (data: ArchitectureStatsResponse | undefined): Record<string, SystemDetails> => ({
  'firewall-nginx': {
    title: 'Firewall & Edge (NGINX)',
    icon: 'security',
    description: 'The first line of defense and traffic routing for the SeraGo ecosystem.',
    technologies: ['UFW', 'NGINX', 'Cloudflare', 'SSL/TLS'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          All incoming traffic hits our VPS directly, as our <code>.pro.et</code> domain bypasses traditional CDNs to ensure data sovereignty and direct routing.
        </p>
        <p>
          At the edge, <strong>UFW (Uncomplicated Firewall)</strong> drops all unexpected ports, strictly allowing only HTTP (80), HTTPS (443), and SSH (22).
        </p>
        <p>
          <strong>NGINX</strong> acts as our high-performance reverse proxy and terminates SSL (managed automatically via Certbot). It routes traffic using strict subdomain matching:
        </p>
        <ul className="list-disc pl-6 space-y-2">
          <li><code>serago.pro.et</code> ➔ React Frontend</li>
          <li><code>api.serago.pro.et</code> ➔ .NET Core API (Kestrel)</li>
          <li><code>ai.serago.pro.et</code> ➔ Django AI Service (Gunicorn)</li>
        </ul>
      </div>
    )
  },
  'frontend': {
    title: 'SeraGo Web Client',
    icon: 'devices',
    description: 'React + Vite Single Page Application served with Tailwind CSS v4.',
    technologies: ['React 19', 'Vite 8', 'Tailwind v4', 'SWR', 'Framer Motion', 'TipTap'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          <div className="bg-surface-container-low p-4 rounded-xl border border-outline-variant flex flex-col justify-center">
            <div className="text-[10px] text-secondary uppercase tracking-wider mb-1 font-bold">Views Today</div>
            <div className="font-display-lg text-primary leading-none">{data?.database.viewsToday?.toLocaleString() || '...'}</div>
          </div>
          <div className="bg-surface-container-low p-4 rounded-xl border border-outline-variant flex flex-col justify-center">
            <div className="text-[10px] text-secondary uppercase tracking-wider mb-1 font-bold">Views This Week</div>
            <div className="font-display-lg text-primary leading-none">{data?.database.viewsThisWeek?.toLocaleString() || '...'}</div>
          </div>
          <div className="bg-surface-container-low p-4 rounded-xl border border-outline-variant flex flex-col justify-center">
            <div className="text-[10px] text-secondary uppercase tracking-wider mb-1 font-bold">Views This Month</div>
            <div className="font-display-lg text-primary leading-none">{data?.database.viewsThisMonth?.toLocaleString() || '...'}</div>
          </div>
        </div>
        <p>
          The SeraGo web client is a cutting-edge Single Page Application built on <strong>React 19</strong> and bundled with <strong>Vite 8</strong> for lightning-fast HMR and optimized production builds.
        </p>
        <p>
          For styling and animation, we utilize the newly released <strong>TailwindCSS v4</strong> paired with <strong>Framer Motion</strong>. This combination allows us to execute a highly customized design system (with strict UI scaling and custom CSS variables) alongside fluid micro-animations—like the staggered entrance of the architecture grid.
        </p>
        <p>
          State management and data fetching are handled by <strong>SWR</strong>, which provides intelligent caching, focus revalidation, and background polling (powering the live, real-time stats you see on the hub). Form state is managed via <strong>Formik</strong> and <strong>Yup</strong>.
        </p>
        <p>
          We also implemented <strong>TipTap</strong>, a headless wrapper around ProseMirror, to provide a rich-text editing experience, and we maintain an active <strong>SignalR</strong> WebSocket connection to the backend for instant push notifications.
        </p>
      </div>
    )
  },
  'backend': {
    title: 'Core API Backend',
    icon: 'terminal',
    description: 'C# .NET 8 Backend powering authentication, business logic, and job management.',
    technologies: ['.NET 8', 'Aufy Auth', 'Hangfire', 'EF Core', 'SignalR', 'AWS SDK'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Our primary backend is a robust <strong>.NET 8 Web API</strong> utilizing <strong>Entity Framework Core</strong> for data access and migrations against our PostgreSQL database.
        </p>
        <p>
          For identity and security, we integrated the <strong>Aufy</strong> authentication library. Aufy handles our secure, HTTP-only cookie-based auth flow and Google OAuth integration, which required strict subdomain routing and CORS policies at the NGINX edge to function perfectly.
        </p>
        <p>
          Asynchronous workloads are delegated to <strong>Hangfire</strong>. Backed by PostgreSQL, Hangfire reliably queues and executes background jobs like dispatching tasks to the Django AI service or sending transactional emails via <strong>FluentEmail</strong>.
        </p>
        <p>
          The API also features a <strong>SignalR</strong> hub connected to our Redis backplane for real-time client communication, and utilizes the <strong>AWS S3 SDK</strong> to seamlessly interface with our Cloudflare R2 storage buckets.
        </p>
      </div>
    )
  },
  'database': {
    title: 'Self-Hosted PostgreSQL',
    icon: 'database',
    description: 'The primary source of truth for users, jobs, and system state, running directly on our VPS.',
    technologies: ['PostgreSQL 16', 'VPS Hosted', 'pgvector (planned)'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          In production, we self-host <strong>PostgreSQL</strong> directly on our VPS to ensure ultra-low latency between our .NET API, AI Service, and the database.
        </p>
        <p>
          We actually run two isolated logical databases within our Postgres cluster. The <strong>SeraGo Core DB</strong> stores all relational business data: Users, Jobs, Applications, Saved Jobs, and User Settings. It is strictly isolated and only accessible from internal VPS services.
        </p>
        <p>
          The second database is the <strong>Scraper DB</strong>. This completely isolates the raw, dirty data ingested from external Ethiopian job boards. By keeping this data in a separate database, we guarantee that our main system is never polluted by malformed web data until it passes the .NET normalization sync.
        </p>
        <p>
          In the future, we will utilize the <strong>pgvector</strong> extension directly within Postgres to store embeddings and perform cosine similarity searches for AI job matching.
        </p>
      </div>
    )
  },
  'scraper': {
    title: 'Data Scraper',
    icon: 'public',
    description: 'Django-based automated data ingestion pipeline pulling jobs from external sources.',
    technologies: ['Django Admin', 'Cloudflare Bypass', 'Linux Cron', 'SyncScheduler'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          The SeraGo Scraper is a fully independent <strong>Django</strong> application hosted natively on our VPS. By running it natively instead of on external CI runners, we can utilize local network bypasses to avoid aggressive rate limits from target portals.
        </p>
        <p>
          The pipeline is 100% automated via native <strong>Linux cron</strong> scheduling. The scraper executes every 15 minutes to crawl multiple Ethiopian job portals. For job portals heavily protected by <strong>Cloudflare</strong> anti-bot challenges, we dynamically route requests through specialized external scraping services to successfully bypass the captchas and extract the data.
        </p>
        <p>
          We expose a secure, dedicated Django Admin panel at <code>scraper.serago.pro.et</code> to monitor every single run in real-time. The <strong>ScrapeLog</strong> system records highly detailed telemetry: the exact JSON payloads fetched, the delta between jobs found vs. new jobs inserted, execution duration, and full exception stack traces if a target site's HTML structure changes.
        </p>
        <p>
          Data ingestion is perfectly choreographed: exactly 5 minutes after the scraper finishes its run, the main .NET API's <code>SyncScheduler</code> activates. It pulls the raw jobs, maps them into our 22 canonical industry sectors, and pushes them live to the users.
        </p>

        <div className="mt-8">
          <h3 className="font-headline-md text-on-surface mb-4">Live Pipeline Telemetry</h3>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-2 mb-4">
            {[
              { label: 'Run count', value: data?.scraper.runCount ?? '...' },
              { label: 'API hits', value: data?.scraper.totalApiHits ?? '...' },
              { label: 'Items found', value: data?.scraper.totalItemsFound ?? '...' },
              { label: 'Inserted', value: data?.scraper.totalItemsInserted ?? '...' },
              { label: 'Updated', value: data?.scraper.totalItemsUpdated ?? '...' },
              { label: 'Skipped', value: data?.scraper.totalItemsSkipped ?? '...' }
            ].map((s, i) => (
              <div key={i} className="bg-surface-container-low p-3 rounded-lg border border-outline-variant text-center">
                <div className="text-[10px] text-secondary uppercase tracking-wider mb-1 font-bold">{s.label}</div>
                <div className="font-label-lg text-primary">{s.value}</div>
              </div>
            ))}
          </div>

          <div className="border border-outline-variant rounded-xl overflow-hidden overflow-x-auto">
            <table className="w-full text-left text-sm whitespace-nowrap">
              <thead className="bg-surface-container-low text-secondary uppercase text-[10px] tracking-wider">
                <tr>
                  <th className="px-4 py-3">Time</th>
                  <th className="px-4 py-3">API Hits</th>
                  <th className="px-4 py-3">Found</th>
                  <th className="px-4 py-3">Inserted</th>
                  <th className="px-4 py-3">Updated</th>
                  <th className="px-4 py-3">Skipped</th>
                  <th className="px-4 py-3">Stable</th>
                  <th className="px-4 py-3">Unstable</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-variant bg-surface-container-lowest font-body-sm">
                {(data?.scraper.recentRuns || []).map((r, i) => (
                  <tr key={i} className="hover:bg-surface-container-low transition-colors">
                    <td className="px-4 py-3 font-label-md text-primary">{r.time}</td>
                    <td className="px-4 py-3 text-on-surface">{r.api_hits}</td>
                    <td className="px-4 py-3 text-on-surface">{r.found}</td>
                    <td className="px-4 py-3 text-success font-bold">+{r.inserted}</td>
                    <td className="px-4 py-3 text-accent">{r.updated}</td>
                    <td className="px-4 py-3 text-secondary">{r.skipped}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 rounded text-[10px] uppercase font-bold ${r.stable_websites_status === 'success' ? 'bg-success/10 text-success' : 'bg-error/10 text-error'}`}>{r.stable_websites_status}</span>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 rounded text-[10px] uppercase font-bold ${r.unstable_websites_status === 'success' ? 'bg-success/10 text-success' : 'bg-error/10 text-error'}`}>{r.unstable_websites_status}</span>
                    </td>
                  </tr>
                ))}
                {!data?.scraper.recentRuns?.length && (
                  <tr>
                    <td colSpan={8} className="px-4 py-6 text-center text-secondary">No runs recorded today yet.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    )
  },
  'ai-service': {
    title: 'AI Engine',
    icon: 'smart_toy',
    description: 'Django-based local intelligence orchestrating Ollama for resume parsing, job normalization, and AI cover letters.',
    technologies: ['Ollama', 'Llama 3 / Mistral', 'Django', 'Celery'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          The <strong>AI Engine</strong> is an isolated Django service that orchestrates all Machine Learning workloads. Instead of relying purely on expensive external APIs, we run <strong>Ollama</strong> as a persistent background daemon directly on our VPS.
        </p>
        <p>
          This engine is specifically responsible for three critical, heavy-duty NLP tasks:
        </p>
        <ul className="list-disc pl-6 space-y-2">
          <li><strong>Resume Parsing:</strong> Automatically extracting skills, experience, and education from user-uploaded PDFs to populate their structured talent profile.</li>
          <li><strong>Job Classification & Normalization:</strong> Analyzing unstructured job postings from the scraper, categorizing them into proper industry sectors, extracting required skills, and formatting the data uniformly.</li>
          <li><strong>AI Cover Letters:</strong> Generating highly personalized cover letters by cross-referencing a specific job description against the user's parsed resume profile.</li>
        </ul>
        <p>
          Ollama allows us to load highly efficient, quantized open-weights models entirely into our server's memory. When processing private user resumes, the data never leaves our server, guaranteeing 100% data privacy.
        </p>
        <p>
          Because LLM inference can be resource-intensive, all requests are queued through <strong>Celery</strong> (backed by Redis). This ensures the local Ollama instance processes requests asynchronously without ever crashing or blocking the main .NET API.
        </p>

        <div className="mt-8 border border-outline-variant rounded-xl overflow-hidden">
          <div className="bg-surface-container-low px-4 py-3 border-b border-outline-variant font-label-lg text-on-surface">Live Classifications Stream</div>
          <div className="divide-y divide-surface-variant">
            {(data?.recentAiClassifications || []).map((c, i) => (
              <div key={i} className="p-4 bg-surface-container-lowest flex flex-col gap-2">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                  <span className="font-label-lg text-primary">{c.jobTitle}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-secondary text-xs">{c.sectorBefore}</span>
                    <span className="material-symbols-outlined text-[14px] text-secondary">arrow_forward</span>
                    <span className="bg-accent/10 text-accent px-2 py-1 rounded text-[10px] uppercase font-bold tracking-wider w-fit">{c.sectorAfter}</span>
                  </div>
                </div>
                <div className="text-sm text-secondary font-body-sm flex items-center gap-2">
                  <span className="material-symbols-outlined text-[16px]">psychology</span>
                  {c.reasoning}
                </div>
              </div>
            ))}
            {!data?.recentAiClassifications?.length && (
              <div className="p-6 text-center text-secondary">No AI classifications recorded yet.</div>
            )}
          </div>
        </div>
      </div>
    )
  },
  'ci-cd': {
    title: 'CI/CD Pipeline',
    icon: 'rocket_launch',
    description: 'Automated testing, building, and deployment pipeline.',
    technologies: ['GitHub Actions', 'Docker', 'SSH Deploy'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Every push to the repository triggers our <strong>GitHub Actions</strong> workflows.
        </p>
        <p>
          The CI pipeline builds the .NET application and compiles the React frontend to ensure there are no compilation errors.
        </p>
        <p>
          Upon merging to the main branch, the CD pipeline securely connects to our VPS via SSH, pulls the latest code, builds the production bundles, restarts the systemd services, and clears the NGINX cache automatically.
        </p>
      </div>
    )
  },
  'cloud-storage': {
    title: 'Cloud Object Storage',
    icon: 'cloud',
    description: 'Secure, scalable asset storage for user resumes, avatars, and attachments.',
    technologies: ['Cloudflare R2', 'S3 API', 'Pre-signed URLs'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Instead of storing large blobs in PostgreSQL, we utilize <strong>Cloudflare R2</strong> (an S3-compatible object storage) for all user-uploaded files like resumes and avatars.
        </p>
        <p>
          The .NET backend orchestrates secure access by generating short-lived <strong>Pre-signed URLs</strong>. This allows the frontend or the AI Service to upload and download files directly from the bucket without routing the heavy file stream through the API servers.
        </p>
      </div>
    )
  },
  'email-service': {
    title: 'Transactional Email Service',
    icon: 'mail',
    description: 'Reliable background worker for delivering critical account notifications.',
    technologies: ['SMTP', 'Hangfire', 'MailKit'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          To ensure the core API responds instantly during user registration or password resets, all email sending is offloaded to a background worker.
        </p>
        <p>
          We use <strong>Hangfire</strong> to queue email jobs. The background worker uses <strong>MailKit</strong> to connect to our SMTP provider and dispatch the HTML templates. 
        </p>
        <p>
          If the SMTP provider is temporarily down, Hangfire automatically retries the job with exponential backoff, ensuring no critical emails are lost.
        </p>
      </div>
    )
  },
  'ai-matching': {
    title: 'AI Semantic Job Matching',
    icon: 'join',
    description: 'Vector-based cosine similarity search for the personalized "For You" job feed.',
    technologies: ['pgvector', 'all-MiniLM-L6-v2', 'HNSW Index'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Instead of basic keyword search, SeraGo uses semantic vector matching to power the "For You" job feed. We use the <strong>all-MiniLM-L6-v2</strong> sentence-transformer model to convert job descriptions and user resumes into 384-dimensional mathematical vectors.
        </p>
        <p>
          These vectors are stored directly in PostgreSQL using the <strong>pgvector</strong> extension. We use an <strong>HNSW (Hierarchical Navigable Small World)</strong> index to perform lightning-fast similarity searches across millions of vectors in under 10 milliseconds.
        </p>
        <p>
          To ensure instant page loads for users, we pre-compute the Top 50 job matches for each user in the background via Celery, filtering out irrelevant sectors automatically. When a user opens their dashboard, the database simply returns their pre-computed, ultra-relevant job feed instantly.
        </p>
      </div>
    )
  },
  'disaster-recovery': {
    title: 'Disaster Recovery & Backups',
    icon: 'backup',
    description: 'Automated 3-tier backup strategy ensuring zero data loss.',
    technologies: ['pg_dump', 'Cloudflare R2', 'Cron'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Our infrastructure follows a strict 3-tier backup strategy complying with the 3-2-1 rule (3 copies, 2 media, 1 offsite) to ensure absolute data safety.
        </p>
        <p>
          Every night, a background cron job executes <code>pg_dump</code> to create a complete snapshot of the PostgreSQL database, storing it locally on the VPS with a rolling 30-day retention policy.
        </p>
        <p>
          Simultaneously, the system encrypts and pushes a copy of this backup to <strong>Cloudflare R2</strong> object storage for off-site redundancy (90-day retention). This guarantees that even in the event of a total catastrophic server failure, our entire platform can be restored in minutes.
        </p>
      </div>
    )
  },
  'redis-signalr': {
    title: 'In-Memory Cache & WebSockets',
    icon: 'memory',
    description: 'Redis 7 handling the SignalR backplane for real-time events.',
    technologies: ['Redis 7', 'SignalR', 'In-Memory Caching'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          To support instant, real-time communication between the .NET API and the React frontend, we use <strong>SignalR</strong> WebSockets.
        </p>
        <p>
          Because our backend can scale to multiple processes, we use <strong>Redis 7</strong> as the SignalR backplane. This ensures that a WebSocket message generated by one Kestrel worker is instantly broadcast to all connected clients, regardless of which process they are connected to.
        </p>
        <p>
          Redis also acts as our primary in-memory cache, storing frequently accessed data (like sector lists and AI classifications) to drastically reduce PostgreSQL query load.
        </p>
      </div>
    )
  },
  'process-management': {
    title: 'Process Management (systemd)',
    icon: 'memory_alt',
    description: 'Ubuntu native systemd ensuring high availability for all services.',
    technologies: ['systemd', 'journald', 'Ubuntu 24.04'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Instead of relying on legacy process managers like Supervisor, our deployment leans completely into Ubuntu's native <strong>systemd</strong>.
        </p>
        <p>
          Every component of SeraGo (the .NET Kestrel server, the Django Gunicorn server, and Celery workers) is registered as a persistent systemd service.
        </p>
        <p>
          This ensures that if any process crashes due to an unhandled exception or an Out-Of-Memory (OOM) error, systemd instantly restarts it. It also manages graceful shutdowns and startup dependencies (e.g., ensuring Postgres and Redis are running before the .NET API starts).
        </p>
      </div>
    )
  },
  'security-hardening': {
    title: 'Security & Bot Blocking',
    icon: 'shield',
    description: 'Aggressive edge-level filtering for malicious traffic.',
    technologies: ['NGINX Rules', 'HSTS', 'X-Frame-Options'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          Since we bypass traditional CDNs, we implemented aggressive security hardening directly at the NGINX edge.
        </p>
        <p>
          Our NGINX configuration explicitly drops traffic from known malicious bot patterns. Any request attempting to access hidden files (like <code>.env</code> or <code>.git</code>) or execute scripts (<code>.php</code>, <code>.sh</code>, <code>wp-admin</code>) is instantly terminated with a 404 to obscure the server architecture.
        </p>
        <p>
          We also enforce strict security headers, including <strong>Strict-Transport-Security (HSTS)</strong>, <strong>X-Content-Type-Options</strong>, and <strong>X-Frame-Options</strong> to prevent clickjacking and MIME-sniffing attacks.
        </p>
      </div>
    )
  },
  'ssl-encryption': {
    title: 'SSL Automation (Certbot)',
    icon: 'lock',
    description: 'Automated HTTPS encryption for all subdomains via Let\'s Encrypt.',
    technologies: ['Certbot', 'Let\'s Encrypt', 'Diffie-Hellman'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          All traffic between our users and the VPS is encrypted using TLS 1.3. We use <strong>Certbot</strong> to automatically provision and renew certificates from <strong>Let's Encrypt</strong>.
        </p>
        <p>
          A single certificate natively secures all of our subdomains (<code>serago.pro.et</code>, <code>api.serago.pro.et</code>, <code>ai.serago.pro.et</code>).
        </p>
        <p>
          To achieve an A+ security rating, we generated a custom 2048-bit <strong>Diffie-Hellman parameter</strong> (<code>ssl-dhparams.pem</code>) to ensure perfect forward secrecy.
        </p>
      </div>
    )
  },
  'log-retention': {
    title: 'Log Retention & Observability',
    icon: 'manage_search',
    description: 'Centralized logging preventing disk exhaustion.',
    technologies: ['journalctl', 'logrotate', 'System Monitoring'],
    deepDive: (
      <div className="space-y-6 text-secondary font-body-md">
        <p>
          With multiple services running on a single VPS, unmanaged logs can quickly consume all available disk space, leading to a catastrophic crash.
        </p>
        <p>
          Because we use systemd, all application standard output is automatically routed to <strong>journald</strong>. We configured strict retention limits on <code>journalctl</code> (e.g., maximum size of 500MB and max retention of 14 days).
        </p>
        <p>
          For NGINX access and error logs, we utilize <strong>logrotate</strong> to compress and archive older logs weekly, ensuring our VPS SSD always has breathing room.
        </p>
      </div>
    )
  }
})

const fetcher = (url: string) => request<ArchitectureStatsResponse>(url)

export default function ArchitectureSystemPage() {
  const { systemId } = useParams<{ systemId: string }>()
  
  // No polling here either, just re-use the SWR cache if it exists, or fetch once.
  const { data } = useSWR('/architecture/stats', fetcher, { revalidateOnFocus: false })
  
  const systems = getSystems(data)
  const system = systemId ? systems[systemId] : null

  if (!system) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center bg-surface">
        <h1 className="font-display-md text-error">System Not Found</h1>
        <Link to="/architecture" className="mt-4 text-primary hover:underline">
          Return to Architecture Hub
        </Link>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-surface px-4 py-12 font-sans md:px-8 lg:px-12">
      <div className="mx-auto max-w-4xl">
        <Link 
          to="/architecture" 
          className="inline-flex items-center gap-2 text-secondary hover:text-primary transition-colors mb-8"
        >
          <span className="material-symbols-outlined text-sm">arrow_back</span>
          <span className="font-label-md">Back to Overview</span>
        </Link>

        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-surface-container-lowest border-outline-variant rounded-2xl border p-8 shadow-sm"
        >
          <div className="flex items-center gap-6 mb-8">
            <div className="rounded-full bg-surface-container-low p-4 border border-outline-variant">
              <span className="material-symbols-outlined text-5xl text-primary">{system.icon}</span>
            </div>
            <div>
              <h1 className="font-display-lg text-on-surface">{system.title}</h1>
              <p className="font-body-lg text-secondary mt-2">{system.description}</p>
            </div>
          </div>

          <div className="mb-10">
            <h3 className="font-label-lg text-on-surface mb-4 uppercase tracking-wider">Tech Stack</h3>
            <div className="flex flex-wrap gap-3">
              {system.technologies.map(tech => (
                <span key={tech} className="bg-primary/10 text-primary px-4 py-2 rounded-full font-label-md border border-primary/20">
                  {tech}
                </span>
              ))}
            </div>
          </div>

          <div>
            <h3 className="font-label-lg text-on-surface mb-4 uppercase tracking-wider">Deep Dive</h3>
            <div className="bg-surface-container-low p-6 rounded-xl border border-outline-variant/50">
              {system.deepDive}
            </div>
          </div>
        </motion.div>
      </div>
    </div>
  )
}
