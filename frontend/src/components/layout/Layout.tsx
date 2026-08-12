import { Outlet } from 'react-router-dom'
import Navbar from './Navbar.tsx'
import Footer from './Footer.tsx'
import { SidebarProvider } from '../../context'

export default function Layout() {
  return (
    <SidebarProvider>
      <div className="flex min-h-svh flex-col">
        <Navbar />
        <main className="flex-1">
          <Outlet />
        </main>
        <Footer />
      </div>
    </SidebarProvider>
  )
}
