using Qalam.Data.DTOs.Common;

namespace Qalam.Service.Abstracts;

public interface IEnumService
{
    /// <param name="nationalityCode">ISO2 code. SA → NationalId/Iqama; other → Passport/License/GovernmentId; null → all.</param>
    List<EnumItemDto> GetIdentityTypes(string? nationalityCode = null);
    List<EnumItemDto> GetTeacherDocumentTypes();
}
