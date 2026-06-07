using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationRequirementProvider : ITeacherRegistrationRequirementProvider
{
    private readonly ITeacherRegistrationRequirementRepository _repository;

    public TeacherRegistrationRequirementProvider(ITeacherRegistrationRequirementRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TeacherRegistrationRequirement>> GetActiveRequirementsAsync(CancellationToken cancellationToken = default) =>
        await _repository.GetActiveOrderedAsync(cancellationToken);

    public async Task<List<TeacherRegistrationRequirement>> GetAllRequirementsAsync(CancellationToken cancellationToken = default) =>
        await _repository.GetAllOrderedAsync(cancellationToken);

    public async Task<List<TeacherRegistrationRequirementPublicDto>> GetActivePublicDtosAsync(CancellationToken cancellationToken = default)
    {
        var list = await GetActiveRequirementsAsync(cancellationToken);
        return list.Select(ToPublicDto).ToList();
    }

    public TeacherRegistrationRequirementPublicDto ToPublicDto(TeacherRegistrationRequirement entity) =>
        new()
        {
            Code = entity.Code,
            NameAr = entity.NameAr,
            NameEn = entity.NameEn,
            DescriptionAr = entity.DescriptionAr,
            DescriptionEn = entity.DescriptionEn,
            RequirementType = entity.RequirementType.ToString(),
            IsRequired = entity.IsRequired,
            SortOrder = entity.SortOrder,
            MinCount = entity.MinCount,
            MaxCount = entity.MaxCount,
            MaxFileSizeBytes = entity.MaxFileSizeBytes,
            AllowedExtensions = RegistrationRequirementExtensionsHelper.Parse(entity.AllowedExtensionsJson),
            MaxLength = entity.MaxLength,
            Options = ToOptionDtos(entity)
        };

    public TeacherRegistrationRequirementAdminDto ToAdminDto(TeacherRegistrationRequirement entity) =>
        new()
        {
            Id = entity.Id,
            Code = entity.Code,
            NameAr = entity.NameAr,
            NameEn = entity.NameEn,
            DescriptionAr = entity.DescriptionAr,
            DescriptionEn = entity.DescriptionEn,
            RequirementType = entity.RequirementType,
            IsActive = entity.IsActive,
            IsRequired = entity.IsRequired,
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

    private static List<RequirementOptionDto>? ToOptionDtos(TeacherRegistrationRequirement entity)
    {
        if (entity.RequirementType != Qalam.Data.Entity.Common.Enums.RegistrationRequirementType.Selection)
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
