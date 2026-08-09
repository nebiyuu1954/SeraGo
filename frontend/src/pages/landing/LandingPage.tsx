import Hero from './Hero.tsx'
import JobListings from './JobListings.tsx'
import ForCandidates from './ForCandidates.tsx'
import Stats from './Stats.tsx'
import ForEmployers from './ForEmployers.tsx'
import HowItWorks from './HowItWorks.tsx'
import FAQ from './FAQ.tsx'
import CTA from './CTA.tsx'

export default function LandingPage() {
  return (
    <>
      <Hero />
      <JobListings />
      <ForCandidates />
      <Stats />
      <ForEmployers />
      <HowItWorks />
      <FAQ />
      <CTA />
    </>
  )
}
