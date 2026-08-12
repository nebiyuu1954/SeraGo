namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Lifecycle of a job posted through the SeraGo API. Jobs are visible to
/// everyone only once Published (and not soft-deleted); drafts are visible
/// only to their owner.
/// </summary>
public enum JobStatus
{
    /// <summary>Saved but not submitted — the owner can keep editing it. Hidden from everyone else.</summary>
    Draft = 0,

    /// <summary>Submitted for review — hidden until an admin approves.</summary>
    PendingApproval = 1,

    /// <summary>Approved and live.</summary>
    Published = 2,

    /// <summary>Rejected by an admin — the owner can edit and re-submit.</summary>
    Rejected = 3,
}
