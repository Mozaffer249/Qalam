namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Teacher account status
/// </summary>
public enum TeacherStatus
{
    Pending = 1,                    // Profile incomplete
    Active = 2,
    Blocked = 3,
    PendingVerification = 4         // Documents uploaded, awaiting admin
}

/// <summary>
/// Teacher document type
/// </summary>
public enum TeacherDocumentType
{
    Id = 1,
    Certificate = 2,
    Other = 3,
    NationalId = 4,      // Saudi National ID
    Iqama = 5,           // Saudi Iqama
    Passport = 6         // International Passport
}

/// <summary>
/// Availability exception type
/// </summary>
public enum AvailabilityExceptionType
{
    Blocked = 1,
    Extra = 2
}
