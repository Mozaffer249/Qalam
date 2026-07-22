using Qalam.Data.DTOs.Common;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface IEnumService
{
    /// <param name="location">
    /// InsideSaudiArabia → NationalId/Iqama; OutsideSaudiArabia → Passport/License/GovernmentId;
    /// null → all types.
    /// </param>
    /// <param name="nationalityCode">Deprecated; ignored. Kept for wire compatibility.</param>
    List<EnumItemDto> GetIdentityTypes(TeacherLocation? location = null, string? nationalityCode = null);
    List<EnumItemDto> GetTeacherDocumentTypes();
}
