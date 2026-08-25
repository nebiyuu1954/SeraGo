namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Lifecycle of a talent's application to a Serago-posted job.
/// 
/// Pending → Reviewed → Interview → Hired
///                          ↘ Rejected
/// </summary>
public enum ApplicationStatus
{
    /// <summary>Submitted by the talent — waiting for recruiter review.</summary>
    Pending = 0,

    /// <summary>Recruiter has opened / is actively reviewing the application.</summary>
    Reviewed = 1,

    /// <summary>Shortlisted for interview.</summary>
    Interview = 2,

    /// <summary>Offer extended / hired.</summary>
    Hired = 3,

    /// <summary>Recruiter passed on the candidate at any stage.</summary>
    Rejected = 4,
}
