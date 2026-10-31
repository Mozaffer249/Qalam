using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Student;

public class SetStudentIdentityDto
{
    public int NationalityId { get; set; }
    public IdentityType IdentityType { get; set; }
    public string IdentityNumber { get; set; } = string.Empty;
    public string? IssuingCountryCode { get; set; }
}

public class StudentIdentityResponseDto
{
    public IdentityType? IdentityType { get; set; }
    public string? IdentityNumber { get; set; }
    public int? NationalityId { get; set; }
    public string? NationalityCode { get; set; }
    public string? NationalityNameAr { get; set; }
    public string? NationalityNameEn { get; set; }
    public string? IssuingCountryCode { get; set; }
}
