using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface IPolicyVersionRepository
{
    /// <summary>Published version in force at <paramref name="asOf"/> (EffectiveFrom ≤ asOf &lt; EffectiveTo).</summary>
    Task<PolicyVersion?> GetEffectiveAsync(DateTime asOf, CancellationToken cancellationToken = default);
    Task<PolicyVersion?> GetByIdAsync(int id, bool track = false, CancellationToken cancellationToken = default);
    Task<PolicyVersion?> GetDraftAsync(bool track = false, CancellationToken cancellationToken = default);
    /// <summary>Published versions whose EffectiveTo is null or later than <paramref name="from"/>, newest first.</summary>
    Task<List<PolicyVersion>> ListOpenPublishedAsync(CancellationToken cancellationToken = default);
    Task<List<PolicyVersion>> ListAsync(CancellationToken cancellationToken = default);
    Task<int> GetMaxVersionNumberAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PolicyVersion version, CancellationToken cancellationToken = default);
    void Remove(PolicyVersion version);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPolicyCaseRepository
{
    Task AddAsync(PolicyCase policyCase, CancellationToken cancellationToken = default);
    Task<PolicyCase?> GetByIdAsync(int id, bool track = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsForScheduleAsync(PolicyCaseKind kind, int courseScheduleId, CancellationToken cancellationToken = default);
    Task<List<PolicyCase>> ListByEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<List<PolicyCase>> ListByScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default);
    Task<(List<PolicyCase> Items, int Total)> ListAsync(
        PolicyCaseKind? kind,
        PolicyCaseStatus? status,
        DateTime? from,
        DateTime? to,
        int? studentId,
        int? teacherId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
