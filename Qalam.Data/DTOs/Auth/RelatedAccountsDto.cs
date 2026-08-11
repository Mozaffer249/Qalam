namespace Qalam.Data.DTOs.Auth;

public class RelatedAccountsDto
{
    public bool HasGuardian { get; set; }
    public RelatedSelfStudentDto? SelfStudent { get; set; }
    public List<RelatedChildAccountDto> Children { get; set; } = [];
}

public class RelatedSelfStudentDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsMinor { get; set; }
}

public class RelatedChildAccountDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
