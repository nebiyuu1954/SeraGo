import { useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { SidebarContext } from './sidebar.ts'

/**
 * Shares the dashboard sidebar's expanded/collapsed state. Expanded shows
 * labels; collapsed shrinks it to an icon-only rail. The toggle lives in
 * the sidebar itself (under Settings), so the header no longer controls it.
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
