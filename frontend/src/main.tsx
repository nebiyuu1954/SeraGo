import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { ThemeProvider } from './context'
import { Toaster } from 'sonner'
import { initAnalytics } from './lib/analytics'
import './index.css'
import App from './App.tsx'

// Initialise GA4 before the first render (no-op unless a production build has
// a measurement ID configured). Page views are sent per route by AnalyticsTracker.
initAnalytics()

const router = createBrowserRouter([{ path: '*', element: <App /> }])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <Toaster
        position="top-right"
        richColors
        duration={3000}
        toastOptions={{
          style: {
            '--normal-bg': 'var(--color-surface-container-lowest)',
            '--normal-text': 'var(--color-on-surface)',
            '--normal-border': 'var(--color-outline-variant)',
            '--success-bg': '#dcfce7',
            '--success-text': '#166534',
            '--success-border': '#86efac',
            '--error-bg': '#fee2e2',
            '--error-text': '#991b1b',
            '--error-border': '#fca5a5',
            '--warning-bg': '#fff7ed',
            '--warning-text': '#9a3412',
            '--warning-border': '#fdba74',
            '--info-bg': '#eff6ff',
            '--info-text': '#1e40af',
            '--info-border': '#93c5fd',
          } as React.CSSProperties,
        }}
      />
      <RouterProvider router={router} />
    </ThemeProvider>
  </StrictMode>,
)
