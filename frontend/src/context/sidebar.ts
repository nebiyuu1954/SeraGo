import { createContext, useContext } from 'react'

export interface SidebarContextValue {
  /**
   * Desktop sidebar column. Expanded shows labels; collapsed becomes a
   * narrow icon-only rail. Toggled from the sidebar itself (under Settings).
   */
  sidebarOpen: boolean
  setSidebarOpen: (open: boolean) => void
  toggleSidebar: () => void
  /**
   * Mobile slide-in drawer. Kept separate from the desktop column so the
   * two never leak into each other when the viewport crosses the md
   * breakpoint (e.g. resizing a desktop with an open sidebar to mobile).
   */
  drawerOpen: boolean
  setDrawerOpen: (open: boolean) => void
}

export const SidebarContext = createContext<SidebarContextValue | null>(null)

export function useSidebar(): SidebarContextValue {
  const context = useContext(SidebarContext)
  if (!context) {
    throw new Error('useSidebar must be used within a SidebarProvider')
  }
  return context
}
