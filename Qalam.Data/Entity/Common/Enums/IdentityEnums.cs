namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Identity document type
/// </summary>
public enum IdentityType
{
    NationalId = 1,
    Iqama = 2,
    Passport = 3,
    DrivingLicense = 4,
    /// <summary>Government-issued national/resident ID outside Saudi Arabia.</summary>
    GovernmentId = 5
}

/// <summary>
/// Teacher location
/// </summary>
public enum TeacherLocation
{
    InsideSaudiArabia = 1,
    OutsideSaudiArabia = 2
}
