using Qalam.Data.DTOs.Admin;

namespace Qalam.Service.Abstracts;

public interface IAdminEnrollmentQueryService
{
    Task<List<AdminEnrollmentListItemDto>> ListAsync(
        AdminEnrollmentListFilter filter,
        CancellationToken cancellationToken = default);

    Task<AdminEnrollmentDetailDto?> GetByIdAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);
}
