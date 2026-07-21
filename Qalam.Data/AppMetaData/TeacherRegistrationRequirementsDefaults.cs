using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;

namespace Qalam.Data.AppMetaData;

/// <summary>
/// Default catalog rows for teacher registration (matches legacy hardcoded upload rules).
/// </summary>
public static class TeacherRegistrationRequirementsDefaults
{
    public const string DefaultExtensionsJson = "[\".pdf\",\".jpg\",\".jpeg\",\".png\"]";
    public const int DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public static IReadOnlyList<TeacherRegistrationRequirement> Create(DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var extensions = RegistrationRequirementExtensionsHelper.ToJson(new[] { ".pdf", ".jpg", ".jpeg", ".png" });

        return new List<TeacherRegistrationRequirement>
        {
            new()
            {
                Code = TeacherRegistrationRequirementCodes.IdentityDocument,
                NameAr = "وثيقة الهوية",
                NameEn = "Identity document",
                DescriptionAr = "هوية وطنية أو إقامة أو جواز سفر حسب جنسيتك",
                DescriptionEn = "National ID, Iqama, or passport depending on nationality",
                RequirementType = RegistrationRequirementType.File,
                IsActive = true,
                IsRequired = true,
                SortOrder = 10,
                MinCount = 1,
                MaxCount = 1,
                MaxFileSizeBytes = DefaultMaxFileSizeBytes,
                AllowedExtensionsJson = extensions,
                MapsToDocumentType = TeacherDocumentType.IdentityDocument,
                IsSystem = true,
                CreatedAt = now
            },
            new()
            {
                Code = TeacherRegistrationRequirementCodes.Certificate,
                NameAr = "الشهادات",
                NameEn = "Certificates",
                DescriptionAr = "شهادة واحدة على الأقل (حتى 5)",
                DescriptionEn = "At least one certificate (up to 5)",
                RequirementType = RegistrationRequirementType.File,
                IsActive = true,
                IsRequired = true,
                SortOrder = 20,
                MinCount = 1,
                MaxCount = 5,
                MaxFileSizeBytes = DefaultMaxFileSizeBytes,
                AllowedExtensionsJson = extensions,
                MapsToDocumentType = TeacherDocumentType.Certificate,
                IsSystem = true,
                CreatedAt = now
            },
            new()
            {
                Code = TeacherRegistrationRequirementCodes.Bio,
                NameAr = "نبذة عنك",
                NameEn = "Bio",
                DescriptionAr = "نبذة قصيرة تظهر للطلاب",
                DescriptionEn = "Short profile shown to students",
                RequirementType = RegistrationRequirementType.Text,
                IsActive = true,
                IsRequired = false,
                SortOrder = 30,
                MinCount = 0,
                MaxCount = 1,
                MaxLength = 500,
                IsSystem = true,
                CreatedAt = now
            }
        };
    }
}
