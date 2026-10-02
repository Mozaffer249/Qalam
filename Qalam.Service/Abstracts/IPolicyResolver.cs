using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public record ResolvedPolicy(int? VersionId, int VersionNumber, CancellationPolicyRules Rules);

public interface IPolicyResolver
{
    /// <summary>Policy in force now (used for new enrollments and admin previews).</summary>
    Task<ResolvedPolicy> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The enrollment's locked policy: <see cref="Enrollment.PolicyVersionId"/>, otherwise the version that was
    /// effective when the enrollment was activated (or created).
    /// </summary>
    Task<ResolvedPolicy> ForEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default);

    /// <summary>Locks the current version on the enrollment when it has none yet (caller saves).</summary>
    Task SnapshotAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
}
