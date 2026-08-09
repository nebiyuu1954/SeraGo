import { Route, Routes } from 'react-router-dom'
import Layout from './components/layout/Layout.tsx'
import LandingPage from './pages/landing/LandingPage.tsx'
import AboutPage from './pages/about/AboutPage.tsx'
import NotFoundPage from './pages/not-found/NotFoundPage.tsx'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<LandingPage />} />
        <Route path="about" element={<AboutPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}

export default App
