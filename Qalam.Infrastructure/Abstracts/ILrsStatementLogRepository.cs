using Qalam.Data.DTOs.Admin;

namespace Qalam.Infrastructure.Abstracts;

public interface ILrsStatementLogRepository
{
    Task<(List<AdminLrsStatementListItemDto> Items, int Total)> GetPagedAsync(AdminLrsStatementFilter filter, CancellationToken ct = default);
    Task<AdminLrsStatementDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AdminLrsDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<Data.Entity.LRS.LrsStatementLog?> GetEntityByIdAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
