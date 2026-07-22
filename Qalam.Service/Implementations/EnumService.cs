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

    public List<EnumItemDto> GetIdentityTypes(TeacherLocation? location = null, string? nationalityCode = null)
    {
        // nationalityCode is deprecated — identity options follow residence location only.
        _ = nationalityCode;

        var allTypes = Enum.GetValues<IdentityType>();

        IEnumerable<IdentityType> filteredTypes = location switch
        {
            TeacherLocation.InsideSaudiArabia => allTypes.Where(t =>
                t == IdentityType.NationalId || t == IdentityType.Iqama),
            TeacherLocation.OutsideSaudiArabia => allTypes.Where(t =>
                t == IdentityType.Passport
                || t == IdentityType.DrivingLicense
                || t == IdentityType.GovernmentId),
            _ => allTypes
        };

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
