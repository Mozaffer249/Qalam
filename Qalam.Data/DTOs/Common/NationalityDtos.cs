namespace Qalam.Data.DTOs.Common;

public class NationalityPublicDto
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? FlagEmoji { get; set; }
    public int SortOrder { get; set; }
}

public class NationalityAdminDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? FlagEmoji { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class CreateNationalityDto
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? FlagEmoji { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class UpdateNationalityDto
{
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? FlagEmoji { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class SetNationalityActiveDto
{
    public bool IsActive { get; set; }
}
