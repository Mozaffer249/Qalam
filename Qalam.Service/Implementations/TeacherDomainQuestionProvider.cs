using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherDomainQuestionProvider : ITeacherDomainQuestionProvider
{
    public TeacherDomainQuestionPublicDto ToPublicDto(
        TeacherDomainQuestion entity,
        TeacherDomainQuestionSubmission? submission = null) =>
        new()
        {
            Code = entity.Code,
            NameAr = entity.NameAr,
            NameEn = entity.NameEn,
            DescriptionAr = entity.DescriptionAr,
            DescriptionEn = entity.DescriptionEn,
            RequirementType = entity.RequirementType.ToString(),
            IsRequired = entity.IsRequired,
            RequiresAdminReview = entity.RequiresAdminReview,
            SortOrder = entity.SortOrder,
            MinCount = entity.MinCount,
            MaxCount = entity.MaxCount,
            MaxFileSizeBytes = entity.MaxFileSizeBytes,
            AllowedExtensions = RegistrationRequirementExtensionsHelper.Parse(entity.AllowedExtensionsJson),
            MaxLength = entity.MaxLength,
            Options = ToOptionDtos(entity),
            IsSubmitted = submission != null,
            VerificationStatus = submission?.VerificationStatus
        };

    public TeacherDomainQuestionAdminDto ToAdminDto(TeacherDomainQuestion entity) =>
        new()
        {
            Id = entity.Id,
            DomainId = entity.DomainId,
            DomainCode = entity.Domain?.Code ?? string.Empty,
            DomainNameEn = entity.Domain?.NameEn ?? string.Empty,
            Code = entity.Code,
            NameAr = entity.NameAr,
            NameEn = entity.NameEn,
            DescriptionAr = entity.DescriptionAr,
            DescriptionEn = entity.DescriptionEn,
            RequirementType = entity.RequirementType,
            IsActive = entity.IsActive,
            IsRequired = entity.IsRequired,
            RequiresAdminReview = entity.RequiresAdminReview,
            SortOrder = entity.SortOrder,
            MinCount = entity.MinCount,
            MaxCount = entity.MaxCount,
            MaxFileSizeBytes = entity.MaxFileSizeBytes,
            AllowedExtensions = RegistrationRequirementExtensionsHelper.Parse(entity.AllowedExtensionsJson),
            MaxLength = entity.MaxLength,
            Options = ToOptionDtos(entity),
            MapsToDocumentType = entity.MapsToDocumentType,
            IsSystem = entity.IsSystem
        };

    private static List<RequirementOptionDto>? ToOptionDtos(TeacherDomainQuestion entity)
    {
        if (entity.RequirementType != RegistrationRequirementType.Selection)
            return null;

        var parsed = RegistrationRequirementOptionsHelper.Parse(entity.OptionsJson);
        return parsed.Count == 0
            ? new List<RequirementOptionDto>()
            : parsed.Select(o => new RequirementOptionDto
            {
                Value = o.Value,
                LabelAr = o.LabelAr,
                LabelEn = o.LabelEn
            }).ToList();
    }
}
