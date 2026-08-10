import RoleDashboard from './RoleDashboard.tsx'

export default function TalentDashboardPage() {
  return (
    <RoleDashboard
      role="Talent"
      blurb="Welcome to your job-hunting hub. Here is what is coming to your workspace:"
      features={[
        'Browse and search job listings that match your skills',
        'Track every application in one place',
        'Get matched with roles that actually fit you',
      ]}
    />
  )
}
