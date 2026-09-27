import { Route, Routes } from 'react-router-dom'
import AnalyticsTracker from './components/AnalyticsTracker.tsx'
import Layout from './components/layout/Layout.tsx'
import LandingPage from './pages/shared/landing/LandingPage.tsx'
import AboutPage from './pages/shared/generic/AboutPage.tsx'
import PrivacyPage from './pages/shared/generic/PrivacyPage.tsx'
import TermsPage from './pages/shared/generic/TermsPage.tsx'
import SignupPage from './pages/shared/auth/SignupPage.tsx'
import LoginPage from './pages/shared/auth/LoginPage.tsx'
import ForgotPasswordPage from './pages/shared/auth/ForgotPasswordPage.tsx'
import ResetPasswordPage from './pages/shared/auth/ResetPasswordPage.tsx'
import ConfirmEmailPage from './pages/shared/auth/ConfirmEmailPage.tsx'
import GoogleCallbackPage from './pages/shared/auth/GoogleCallbackPage.tsx'
import DashboardRedirect from './pages/shared/dashboard/DashboardRedirect.tsx'
import TalentJobsPage from './pages/talent/jobs/JobsPage.tsx'
import JobDetailPage from './pages/talent/jobs/JobDetailPage.tsx'
import SavedJobsPage from './pages/talent/saved/SavedJobsPage.tsx'
import JobFormPage from './pages/admin/jobs/JobFormPage.tsx'
import AdminDashboardPage from './pages/admin/dashboard/AdminDashboardPage.tsx'
import AdminSectorsPage from './pages/admin/sectors/SectorsPage.tsx'
import AdminUsersPage from './pages/admin/users/UsersPage.tsx'
import AdminUserDetailPage from './pages/admin/users/AdminUserDetailPage.tsx'
import AdminJobsPage from './pages/admin/jobs/AdminJobsPage.tsx'
import AdminJobDetailPage from './pages/admin/jobs/AdminJobDetailPage.tsx'
import AdminJobsViewsPage from './pages/admin/jobs/AdminJobsViewsPage.tsx'
import AdminScraperPage from './pages/admin/scraper/AdminScraperPage.tsx'
import AdminAiPage from './pages/admin/ai/AdminAiPage.tsx'
import AdminScraperWeekDetailPage from './pages/admin/scraper/AdminScraperWeekDetailPage.tsx'
import TalentProfilePage from './pages/talent/profile/ProfilePage.tsx'
import TalentProfilePreviewPage from './pages/talent/profile/TalentProfilePreviewPage.tsx'
import AdminProfilePage from './pages/admin/profile/ProfilePage.tsx'
import TalentSettingsPage from './pages/talent/settings/SettingsPage.tsx'
import AdminSettingsPage from './pages/admin/settings/SettingsPage.tsx'
import NotFoundPage from './pages/shared/generic/NotFoundPage.tsx'

function App() {
  return (
    <>
      {/* Sends a GA4 page_view on every client-side route change */}
      <AnalyticsTracker />
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<LandingPage />} />
          <Route path="about" element={<AboutPage />} />
          <Route path="privacy" element={<PrivacyPage />} />
          <Route path="terms" element={<TermsPage />} />
          {/* Signed-in role dashboards — resolve the user's role from whoami */}
          <Route path="dashboard" element={<DashboardRedirect />} />
          <Route path="dashboard/talent" element={<TalentJobsPage />} />
          <Route
            path="dashboard/talent/jobs/:jobId"
            element={<JobDetailPage />}
          />
          <Route path="dashboard/talent/saved" element={<SavedJobsPage />} />
          <Route path="dashboard/admin" element={<AdminDashboardPage />} />
          <Route path="dashboard/admin/jobs" element={<AdminJobsPage />} />
          <Route
            path="dashboard/admin/jobs/:jobId"
            element={<AdminJobDetailPage />}
          />
          <Route
            path="dashboard/admin/jobs/:jobId/preview"
            element={<JobDetailPage />}
          />
          <Route
            path="dashboard/admin/jobs/:jobId/edit"
            element={<JobFormPage />}
          />
          <Route
            path="dashboard/admin/jobs/views"
            element={<AdminJobsViewsPage />}
          />
          <Route
            path="dashboard/admin/scraper"
            element={<AdminScraperPage />}
          />
          <Route path="dashboard/admin/ai" element={<AdminAiPage />} />
          <Route
            path="dashboard/admin/scraper/weeks/:periodStart"
            element={<AdminScraperWeekDetailPage />}
          />
          <Route path="dashboard/admin/users" element={<AdminUsersPage />} />
          <Route
            path="dashboard/admin/users/:userId"
            element={<AdminUserDetailPage />}
          />
          <Route
            path="dashboard/admin/sectors"
            element={<AdminSectorsPage />}
          />
          <Route
            path="dashboard/talent/profile"
            element={<TalentProfilePage />}
          />
          <Route
            path="dashboard/talent/profile/preview"
            element={<TalentProfilePreviewPage />}
          />
          <Route
            path="dashboard/admin/profile"
            element={<AdminProfilePage />}
          />
          <Route
            path="dashboard/talent/settings"
            element={<TalentSettingsPage />}
          />
          <Route
            path="dashboard/admin/settings"
            element={<AdminSettingsPage />}
          />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
        {/* Standalone auth screens — full-viewport, outside the app chrome */}
        <Route path="signup" element={<SignupPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="forgot-password" element={<ForgotPasswordPage />} />
        <Route path="reset-password" element={<ResetPasswordPage />} />
        <Route path="confirm-email" element={<ConfirmEmailPage />} />
        <Route path="auth/google/callback" element={<GoogleCallbackPage />} />
      </Routes>
    </>
  )
}

export default App
