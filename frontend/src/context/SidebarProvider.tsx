import { useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { SidebarContext } from './sidebar.ts'

/**
 * Shares the dashboard sidebar's open/closed state between the header and
 * the dashboard pages: when the desktop sidebar is open the header hides its
 * nav links; when it's collapsed they reappear.
 *
 * The desktop column opens by default on desktop (it's the primary dashboard
 * nav); the mobile drawer always starts closed.
 */
export function SidebarProvider({ children }: { children: ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState<boolean>(
    () => window.matchMedia('(min-width: 768px)').matches,
  )
  const [drawerOpen, setDrawerOpen] = useState(false)

  const toggleSidebar = useCallback(() => {
    setSidebarOpen((open) => !open)
  }, [])

  const value = useMemo(
    () => ({
      sidebarOpen,
      setSidebarOpen,
      toggleSidebar,
      drawerOpen,
      setDrawerOpen,
    }),
    [sidebarOpen, toggleSidebar, drawerOpen],
  )

  return <SidebarContext.Provider value={value}>{children}</SidebarContext.Provider>
}
