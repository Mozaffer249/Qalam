using Qalam.Data.Commons;

namespace Qalam.Data.DTOs;

public class EducationDomainDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string ArabicCode { get; set; } = default!;
    public string EnglishCode { get; set; } = default!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed property for backward compatibility and localization
    public string Code => LocalizableEntity.GetLocalizedValue(ArabicCode, EnglishCode);
}
