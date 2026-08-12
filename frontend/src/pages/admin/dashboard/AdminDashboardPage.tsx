import RoleDashboard from '../../shared/dashboard/RoleDashboard.tsx'

export default function AdminDashboardPage() {
  return (
    <RoleDashboard
      role="Admin"
      blurb="Welcome to the platform operations console. Here is what is coming to your workspace:"
      features={[
        'Manage users and their roles',
        'Review listings and flagged content',
        'View system-wide reports and health',
      ]}
    />
  )
}
