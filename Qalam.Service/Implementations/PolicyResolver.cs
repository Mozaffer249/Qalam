using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PolicyResolver : IPolicyResolver
{
    private readonly IPolicyVersionRepository _versions;

    public PolicyResolver(IPolicyVersionRepository versions) => _versions = versions;

    public async Task<ResolvedPolicy> GetCurrentAsync(CancellationToken cancellationToken = default)
        => ToResolved(await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken));

    public async Task<ResolvedPolicy> ForEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        if (enrollment.PolicyVersionId is int id)
        {
            var locked = enrollment.PolicyVersion ?? await _versions.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (locked != null)
                return ToResolved(locked);
        }

        var asOf = enrollment.ActivatedAt ?? enrollment.CreatedAt;
        return ToResolved(await _versions.GetEffectiveAsync(asOf, cancellationToken)
                          ?? await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken));
    }

    public async Task SnapshotAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        if (enrollment.PolicyVersionId != null)
            return;
        var current = await _versions.GetEffectiveAsync(DateTime.UtcNow, cancellationToken);
        enrollment.PolicyVersionId = current?.Id;
    }

    private static ResolvedPolicy ToResolved(PolicyVersion? version)
        => version == null
            ? new ResolvedPolicy(null, 0, CancellationPolicyDefaults.Create())
            : new ResolvedPolicy(version.Id, version.VersionNumber, CancellationPolicyDefaults.FromJson(version.RulesJson));
}
