import { Route, Routes } from 'react-router-dom'
import Layout from './components/layout/Layout.tsx'
import LandingPage from './pages/shared/landing/LandingPage.tsx'
import AboutPage from './pages/shared/generic/AboutPage.tsx'
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
import RecruiterJobsPage from './pages/recruiter/jobs/JobsPage.tsx'
import JobFormPage from './pages/recruiter/jobs/JobFormPage.tsx'
import TalentApplicationsPage from './pages/talent/applications/ApplicationsPage.tsx'
import TalentApplicationDetailPage from './pages/talent/applications/ApplicationDetailPage.tsx'
import TalentApplicationProfilePage from './pages/talent/applications/ApplicationProfilePreviewPage.tsx'
import RecruiterApplicationsPage from './pages/recruiter/applications/ApplicationsPage.tsx'
import RecruiterTalentPreviewPage from './pages/recruiter/applications/TalentPreviewPage.tsx'
import AdminDashboardPage from './pages/admin/dashboard/AdminDashboardPage.tsx'
import AdminSectorsPage from './pages/admin/sectors/SectorsPage.tsx'
import AdminUsersPage from './pages/admin/users/UsersPage.tsx'
import AdminJobsPage from './pages/admin/jobs/AdminJobsPage.tsx'
import RecruiterProfilePage from './pages/recruiter/profile/ProfilePage.tsx'
import RecruiterProfilePreviewPage from './pages/recruiter/profile/RecruiterProfilePreviewPage.tsx'
import TalentProfilePage from './pages/talent/profile/ProfilePage.tsx'
import TalentProfilePreviewPage from './pages/talent/profile/TalentProfilePreviewPage.tsx'
import AdminProfilePage from './pages/admin/profile/ProfilePage.tsx'
import TalentSettingsPage from './pages/talent/settings/SettingsPage.tsx'
import RecruiterSettingsPage from './pages/recruiter/settings/SettingsPage.tsx'
import AdminSettingsPage from './pages/admin/settings/SettingsPage.tsx'
import NotFoundPage from './pages/shared/generic/NotFoundPage.tsx'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<LandingPage />} />
        <Route path="about" element={<AboutPage />} />
        {/* Signed-in role dashboards — resolve the user's role from whoami */}
        <Route path="dashboard" element={<DashboardRedirect />} />
        <Route path="dashboard/talent" element={<TalentJobsPage />} />
        <Route
          path="dashboard/talent/jobs/:jobId"
          element={<JobDetailPage />}
        />
        <Route path="dashboard/talent/saved" element={<SavedJobsPage />} />
        <Route path="dashboard/talent/applications" element={<TalentApplicationsPage />} />
        <Route path="dashboard/talent/applications/:applicationId" element={<TalentApplicationDetailPage />} />
        <Route path="dashboard/talent/applications/:applicationId/profile" element={<TalentApplicationProfilePage />} />
        <Route path="dashboard/recruiter" element={<RecruiterJobsPage />} />
        <Route path="dashboard/recruiter/applications" element={<RecruiterApplicationsPage />} />
        <Route path="dashboard/recruiter/applications/:applicationId/talent" element={<RecruiterTalentPreviewPage />} />
        <Route path="dashboard/recruiter/jobs/new" element={<JobFormPage />} />
        <Route
          path="dashboard/recruiter/jobs/:jobId/edit"
          element={<JobFormPage />}
        />
        <Route path="dashboard/admin" element={<AdminDashboardPage />} />
        <Route path="dashboard/admin/jobs" element={<AdminJobsPage />} />
        <Route path="dashboard/admin/users" element={<AdminUsersPage />} />
        <Route path="dashboard/admin/sectors" element={<AdminSectorsPage />} />
        <Route
          path="dashboard/recruiter/profile"
          element={<RecruiterProfilePage />}
        />
        <Route
          path="dashboard/recruiter/profile/preview"
          element={<RecruiterProfilePreviewPage />}
        />
        <Route
          path="dashboard/talent/profile"
          element={<TalentProfilePage />}
        />
        <Route
          path="dashboard/talent/profile/preview"
          element={<TalentProfilePreviewPage />}
        />
        <Route path="dashboard/admin/profile" element={<AdminProfilePage />} />
        <Route path="dashboard/talent/settings" element={<TalentSettingsPage />} />
        <Route path="dashboard/recruiter/settings" element={<RecruiterSettingsPage />} />
        <Route path="dashboard/admin/settings" element={<AdminSettingsPage />} />
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
  )
}

export default App
