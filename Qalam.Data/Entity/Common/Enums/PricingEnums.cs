namespace Qalam.Data.Entity.Common.Enums;

public enum PricingSnapshotContext
{
    CourseEnrollmentRequest = 1,
    OpenSessionOffer = 2,
    Enrollment = 3,
    /// <summary>Frozen quote for a directed (targeted) open session request at create/publish time.</summary>
    OpenSessionRequest = 4
}

public enum TeacherLevelUpgradeSuggestionStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
