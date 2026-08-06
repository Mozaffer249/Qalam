namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Teacher account status
/// </summary>
public enum TeacherStatus
{
    /// <summary>
    /// Step 3 completed - Personal info added, awaiting document upload
    /// User should proceed to Step 4 (Upload Documents)
    /// </summary>
    AwaitingDocuments = 1,

    /// <summary>
    /// Step 4 completed - Documents uploaded, awaiting admin verification
    /// User should wait for admin approval
    /// </summary>
    PendingVerification = 2,

    /// <summary>
    /// One or more documents rejected - Teacher needs to re-upload
    /// </summary>
    DocumentsRejected = 3,

    /// <summary>
    /// Admin approved - Teacher is fully verified and can teach
    /// </summary>
    Active = 4,

    /// <summary>
    /// Teacher account is blocked by admin
    /// </summary>
    Blocked = 5
}

/// <summary>
/// Document verification status by admin
/// </summary>
public enum DocumentVerificationStatus
{
    /// <summary>
    /// Document uploaded, awaiting admin review
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Document approved by admin
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Document rejected by admin - needs re-upload
    /// </summary>
    Rejected = 3
}

/// <summary>
/// Teacher document type
/// Note: Identity type (NationalId/Iqama/Passport) is stored separately in IdentityType field
/// </summary>
public enum TeacherDocumentType
{
    IdentityDocument = 1,  // Any identity document (actual type in IdentityType field)
    Certificate = 2,       // Educational or professional certificates
    Other = 3              // Other supporting documents
}

/// <summary>
/// Availability exception type
/// </summary>
public enum AvailabilityExceptionType
{
    Blocked = 1,
    Extra = 2
}
