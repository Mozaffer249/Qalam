using Microsoft.Extensions.Localization;
using Qalam.Data.DTOs.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;
using Qalam.Service.Resources.Enums;

namespace Qalam.Service.Implementations;

public class EnumService : IEnumService
{
    private readonly IStringLocalizer<EnumResources> _localizer;

    public EnumService(IStringLocalizer<EnumResources> localizer)
    {
        _localizer = localizer;
    }

    public List<EnumItemDto> GetIdentityTypes(string? nationalityCode = null)
    {
        var allTypes = Enum.GetValues<IdentityType>();

        var isSaudi = string.Equals(nationalityCode?.Trim(), "SA", StringComparison.OrdinalIgnoreCase);

        var filteredTypes = !string.IsNullOrWhiteSpace(nationalityCode)
            ? allTypes.Where(t => isSaudi
                ? (t == IdentityType.NationalId || t == IdentityType.Iqama)
                : t == IdentityType.Passport
                    || t == IdentityType.DrivingLicense
                    || t == IdentityType.GovernmentId)
            : allTypes;

        return filteredTypes.Select(type => new EnumItemDto
        {
            Value = (int)type,
            Name = type.ToString(),
            DisplayName = GetIdentityTypeDisplayName(type)
        }).ToList();
    }

    public List<EnumItemDto> GetTeacherDocumentTypes()
    {
        return Enum.GetValues<TeacherDocumentType>()
            .Select(type => new EnumItemDto
            {
                Value = (int)type,
                Name = type.ToString(),
                DisplayName = GetDocumentTypeDisplayName(type)
            }).ToList();
    }

    private string GetIdentityTypeDisplayName(IdentityType identityType)
    {
        var key = identityType switch
        {
            IdentityType.NationalId => EnumResourcesKeys.IdentityType_NationalId,
            IdentityType.Iqama => EnumResourcesKeys.IdentityType_Iqama,
            IdentityType.Passport => EnumResourcesKeys.IdentityType_Passport,
            IdentityType.DrivingLicense => EnumResourcesKeys.IdentityType_DrivingLicense,
            IdentityType.GovernmentId => EnumResourcesKeys.IdentityType_GovernmentId,
            _ => identityType.ToString()
        };
        return _localizer[key];
    }

    private string GetDocumentTypeDisplayName(TeacherDocumentType documentType)
    {
        var key = documentType switch
        {
            TeacherDocumentType.IdentityDocument => EnumResourcesKeys.TeacherDocumentType_IdentityDocument,
            TeacherDocumentType.Certificate => EnumResourcesKeys.TeacherDocumentType_Certificate,
            TeacherDocumentType.Other => EnumResourcesKeys.TeacherDocumentType_Other,
            _ => documentType.ToString()
        };
        return _localizer[key];
    }
}
