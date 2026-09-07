namespace SeraGo.Core.Domain.Enums;

/// <summary>
/// Types of notifications the system can create.
/// Each type maps to a specific event in the platform.
/// </summary>
public static class NotificationType
{
    // Application events
    public const string ApplicationReceived = "application_received";
    public const string ApplicationStatusChanged = "application_status_changed";

    // Job events
    public const string JobAlert = "job_alert";
    public const string SavedSearchMatch = "saved_search_match";
    public const string JobPendingReview = "job_pending_review";

    // Payment events
    public const string PaymentSuccess = "payment_success";

    // Admin events
    public const string AdminReviewResult = "admin_review_result";
    public const string SystemAnnouncement = "system_announcement";

    // Digest
    public const string DailyDigest = "daily_digest";
}
