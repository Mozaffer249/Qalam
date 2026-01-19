namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Identity document type
/// </summary>
public enum IdentityType
{
    NationalId = 1,    // الهوية الوطنية السعودية
    Iqama = 2,         // إقامة سعودية
    Passport = 3       // جواز سفر (خارج السعودية)
}

/// <summary>
/// Teacher location
/// </summary>
public enum TeacherLocation
{
    InsideSaudiArabia = 1,
    OutsideSaudiArabia = 2
}
