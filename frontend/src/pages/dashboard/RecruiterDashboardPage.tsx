import RoleDashboard from './RoleDashboard.tsx'

export default function RecruiterDashboardPage() {
  return (
    <RoleDashboard
      role="Recruiter"
      blurb="Welcome to your hiring command center. Here is what is coming to your workspace:"
      features={[
        'Post job openings and reach qualified talent',
        'Manage your live listings from one place',
        'Review applicants and move them through your pipeline',
      ]}
    />
  )
}
