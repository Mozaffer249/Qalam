namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Teacher account status
/// </summary>
public enum TeacherStatus
{
    Pending = 1,
    Active = 2,
    Blocked = 3
}

/// <summary>
/// Teacher document type
/// </summary>
public enum TeacherDocumentType
{
    Id = 1,
    Certificate = 2,
    Other = 3
}

/// <summary>
/// Availability exception type
/// </summary>
public enum AvailabilityExceptionType
{
    Blocked = 1,
    Extra = 2
}
