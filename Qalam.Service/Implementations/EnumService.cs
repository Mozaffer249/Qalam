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

    public List<EnumItemDto> GetIdentityTypes(bool? isInSaudiArabia = null)
    {
        var allTypes = Enum.GetValues<IdentityType>();

        // Filter based on location if specified
        var filteredTypes = isInSaudiArabia.HasValue
            ? allTypes.Where(t => isInSaudiArabia.Value
                ? (t == IdentityType.NationalId || t == IdentityType.Iqama)
                : t == IdentityType.Passport || t == IdentityType.DrivingLicense)
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
