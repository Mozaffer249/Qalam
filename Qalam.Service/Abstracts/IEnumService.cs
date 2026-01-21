using Qalam.Data.DTOs.Common;

namespace Qalam.Service.Abstracts;

public interface IEnumService
{
    List<EnumItemDto> GetIdentityTypes(bool? isInSaudiArabia = null);
    List<EnumItemDto> GetTeacherDocumentTypes();
}
