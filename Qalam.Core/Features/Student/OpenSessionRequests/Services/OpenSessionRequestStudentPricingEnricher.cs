using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.context;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Services;

/// <summary>
/// Fills student-facing OSR price + free-trial hint fields after AutoMapper mapping.
/// </summary>
public interface IOpenSessionRequestStudentPricingEnricher
{
    Task EnrichDetailAsync(OpenSessionRequestDetailDto dto, OpenSessionRequest entity, CancellationToken cancellationToken = default);

    Task EnrichListAsync(IReadOnlyList<OpenSessionRequestListItemDto> items, IReadOnlyList<OpenSessionRequest> entities, CancellationToken cancellationToken = default);
}

public class OpenSessionRequestStudentPricingEnricher : IOpenSessionRequestStudentPricingEnricher
{
    private readonly ApplicationDBContext _db;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;

    public OpenSessionRequestStudentPricingEnricher(
        ApplicationDBContext db,
        IFreeSessionPolicyService freeSessionPolicy)
    {
        _db = db;
        _freeSessionPolicy = freeSessionPolicy;
    }

    public async Task EnrichDetailAsync(
        OpenSessionRequestDetailDto dto,
        OpenSessionRequest entity,
        CancellationToken cancellationToken = default)
    {
        var isGroup = entity.GroupType is OfferGroupType.OpenGroup or OfferGroupType.InviteOnly
            || entity.Invitations.Any(i => i.Status == OpenSessionRequestInvitationStatus.Accepted);
        var sessionCount = entity.TotalSessionsCount > 0
            ? entity.TotalSessionsCount
            : entity.Sessions?.Count ?? 0;

        var unused = await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(entity.StudentId, cancellationToken);
        dto.IsFreeTrialEligible = _freeSessionPolicy.IsEligiblePackage(isGroup, sessionCount) && unused;

        if (entity.PricingSnapshotId.HasValue)
        {
            var snap = entity.PricingSnapshot
                ?? await _db.PricingSnapshots.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == entity.PricingSnapshotId.Value, cancellationToken);
            if (snap != null)
            {
                dto.TotalPrice = snap.TotalPrice;
                dto.Currency = snap.Currency;
                dto.MarketCode = snap.MarketCode;
            }
        }
        // Broadcast: leave TotalPrice null — teacher-specific until offers.
    }

    public async Task EnrichListAsync(
        IReadOnlyList<OpenSessionRequestListItemDto> items,
        IReadOnlyList<OpenSessionRequest> entities,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return;

        var byId = entities.ToDictionary(e => e.Id);
        var studentIds = entities.Select(e => e.StudentId).Distinct().ToList();
        var students = await _db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.HasUsedFreeTrialSession })
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var snapshotIds = entities
            .Where(e => e.PricingSnapshotId.HasValue)
            .Select(e => e.PricingSnapshotId!.Value)
            .Distinct()
            .ToList();
        var snapshots = snapshotIds.Count == 0
            ? new Dictionary<int, (decimal TotalPrice, string Currency, string MarketCode)>()
            : await _db.PricingSnapshots.AsNoTracking()
                .Where(s => snapshotIds.Contains(s.Id))
                .ToDictionaryAsync(
                    s => s.Id,
                    s => (s.TotalPrice, s.Currency, s.MarketCode),
                    cancellationToken);

        foreach (var item in items)
        {
            if (!byId.TryGetValue(item.Id, out var entity))
                continue;

            var isGroup = entity.GroupType is OfferGroupType.OpenGroup or OfferGroupType.InviteOnly;
            var unused = students.TryGetValue(entity.StudentId, out var st) && !st.HasUsedFreeTrialSession;
            item.IsFreeTrialEligible = _freeSessionPolicy.IsEligiblePackage(isGroup, entity.TotalSessionsCount) && unused;

            if (entity.PricingSnapshotId.HasValue
                && snapshots.TryGetValue(entity.PricingSnapshotId.Value, out var snap))
            {
                item.TotalPrice = snap.TotalPrice;
                item.Currency = snap.Currency;
                item.MarketCode = snap.MarketCode;
            }
        }
    }
}
