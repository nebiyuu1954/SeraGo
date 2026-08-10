import { Route, Routes } from 'react-router-dom'
import Layout from './components/layout/Layout.tsx'
import LandingPage from './pages/landing/LandingPage.tsx'
import AboutPage from './pages/about/AboutPage.tsx'
import SignupPage from './pages/auth/SignupPage.tsx'
import LoginPage from './pages/auth/LoginPage.tsx'
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage.tsx'
import ResetPasswordPage from './pages/auth/ResetPasswordPage.tsx'
import GoogleCallbackPage from './pages/auth/GoogleCallbackPage.tsx'
import DashboardRedirect from './pages/dashboard/DashboardRedirect.tsx'
import TalentDashboardPage from './pages/dashboard/TalentDashboardPage.tsx'
import RecruiterDashboardPage from './pages/dashboard/RecruiterDashboardPage.tsx'
import AdminDashboardPage from './pages/dashboard/AdminDashboardPage.tsx'
import NotFoundPage from './pages/not-found/NotFoundPage.tsx'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<LandingPage />} />
        <Route path="about" element={<AboutPage />} />
        {/* Signed-in role dashboards — resolve the user's role from whoami */}
        <Route path="dashboard" element={<DashboardRedirect />} />
        <Route path="dashboard/talent" element={<TalentDashboardPage />} />
        <Route
          path="dashboard/recruiter"
          element={<RecruiterDashboardPage />}
        />
        <Route path="dashboard/admin" element={<AdminDashboardPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
      {/* Standalone auth screens — full-viewport, outside the app chrome */}
      <Route path="signup" element={<SignupPage />} />
      <Route path="login" element={<LoginPage />} />
      <Route path="forgot-password" element={<ForgotPasswordPage />} />
      <Route path="reset-password" element={<ResetPasswordPage />} />
      <Route path="auth/google/callback" element={<GoogleCallbackPage />} />
    </Routes>
  )
}

export default App
