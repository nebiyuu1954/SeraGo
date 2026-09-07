import { Outlet, useLocation } from 'react-router-dom'
import Navbar from './Navbar.tsx'
import Footer from './Footer.tsx'
import { SidebarProvider } from '../../context'

export default function Layout() {
  const { pathname } = useLocation()
  // Signed-in dashboards get the experimental density knob (--ui-scale in
  // index.css). Marketing/public routes always render at the designed size.
  const isDashboard = pathname.startsWith('/dashboard')

  return (
    <SidebarProvider>
      <div
        className={
          isDashboard
            ? 'ui-scale ui-scale-fill flex flex-col'
            : 'flex min-h-svh flex-col'
        }
      >
        <Navbar />
        <main className="flex-1">
          <Outlet />
        </main>
        <Footer />
      </div>
    </SidebarProvider>
  )
}
