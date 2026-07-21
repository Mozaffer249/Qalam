using Qalam.Data.DTOs.Common;
using Qalam.Data.Entity.Common;

namespace Qalam.Service.Abstracts;

public interface INationalityProvider
{
    Task<List<Nationality>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Nationality>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<NationalityPublicDto>> GetActivePublicDtosAsync(CancellationToken cancellationToken = default);
    NationalityPublicDto ToPublicDto(Nationality entity);
    NationalityAdminDto ToAdminDto(Nationality entity);
}
