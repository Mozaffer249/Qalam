namespace Qalam.Data.DTOs;

public class UniversityDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Code { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CollegeDto
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DepartmentDto
{
    public int Id { get; set; }
    public int CollegeId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AcademicProgramDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Code { get; set; }
    public string? DegreeType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
