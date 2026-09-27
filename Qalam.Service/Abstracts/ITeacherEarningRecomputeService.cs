using Qalam.Data.DTOs.Admin;

namespace Qalam.Service.Abstracts;

public interface ITeacherEarningRecomputeService
{
    /// <summary>
    /// Re-prices unsettled enrollment snapshots at full teacher earnings (student free trial no longer
    /// cuts them; 0% interview-pending shares resolve to custom → domain level → starter) and
    /// back-fills / re-prices Pending and OnHold earning lines for completed sessions, except each
    /// teacher's single interview session. IncludedInPayout and Voided lines are never touched.
    /// </summary>
    Task<AdminTeacherEarningsRecomputeResultDto> RecomputeAsync(
        bool dryRun,
        int? adminUserId,
        CancellationToken cancellationToken = default);
}
