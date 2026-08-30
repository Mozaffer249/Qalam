using Qalam.Data.DTOs.Admin;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherLedgerReadRepository
{
    Task<List<TeacherLedgerEntryDto>> BuildLedgerAsync(
        int? teacherId,
        int? enrollmentId,
        string? typeFilter,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<(decimal Deductions, decimal Penalties, decimal Settlements, int WarningsCount)> GetImpactBucketsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);
}
