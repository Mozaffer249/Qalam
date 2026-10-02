using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Abstracts;

/// <summary>Tracked enrollment plus the engine inputs and the policy version that governs it.</summary>
public class PolicyContextBundle
{
    public required Enrollment Enrollment { get; init; }
    public required PolicyContext Context { get; init; }
    public required ResolvedPolicy Policy { get; init; }
}

public interface IPolicyContextBuilder
{
    /// <returns>Null when the enrollment does not exist.</returns>
    Task<PolicyContextBundle?> BuildAsync(
        int enrollmentId,
        PolicyCaseKind kind,
        int? targetScheduleId = null,
        PolicyStudentChoice choice = PolicyStudentChoice.None,
        CancellationToken cancellationToken = default);

    Task<int?> GetEnrollmentIdForScheduleAsync(int scheduleId, CancellationToken cancellationToken = default);
}
