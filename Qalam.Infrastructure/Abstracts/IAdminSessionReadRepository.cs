using Qalam.Data.DTOs.Admin;

namespace Qalam.Infrastructure.Abstracts;

public interface IAdminSessionReadRepository
{
    Task<List<AdminSessionListItemDto>> ListAsync(
        AdminSessionListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminSessionDetailDto?> GetDetailAsync(
        int scheduleId,
        CancellationToken cancellationToken = default);
}
