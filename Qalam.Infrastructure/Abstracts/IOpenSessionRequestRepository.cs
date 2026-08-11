using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IOpenSessionRequestRepository : IGenericRepositoryAsync<OpenSessionRequest>
{
    /// <summary>
    /// Lightweight projection — used by the matching service when it only needs the SubjectId.
    /// Returns null if the request doesn't exist.
    /// </summary>
    Task<int?> GetSubjectIdAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Distinct Quran content-type / level IDs required by the request's sessions (nulls ignored).
    /// Empty lists mean no Quran filter for matching.
    /// </summary>
    Task<(List<int> ContentTypeIds, List<int> LevelIds)> GetSessionQuranRequirementIdsAsync(
        int requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Open broadcast OSRs for the given subjects (Active / ReceivingOffers, not expired, no TargetedTeacherId).
    /// Used when a teacher gains a subject and needs rematch into existing requests.
    /// </summary>
    Task<List<int>> GetOpenBroadcastRequestIdsBySubjectIdsAsync(
        IReadOnlyCollection<int> subjectIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Teacher inbox detail — projected directly to the response DTO so EF generates a narrow SELECT.
    /// </summary>
    Task<TeacherAvailableRequestDetailDto?> GetTeacherDetailDtoAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sessions for an availability-match calculation — only the day/time/duration columns are needed.
    /// </summary>
    Task<List<RequestSessionScheduleSlot>> GetSessionScheduleSlotsAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count of non-Withdrawn offers on the request.
    /// </summary>
    Task<int> CountActiveOffersAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Light helper used by the offer handlers to verify a request is in a state that can accept offers
    /// without loading the whole aggregate.
    /// </summary>
    Task<RequestStatusSummary?> GetStatusSummaryAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically transitions the request to <paramref name="newStatus"/>. Used by the offer flow
    /// (Active → ReceivingOffers on first offer). Returns true if a row was updated.
    /// </summary>
    Task<bool> UpdateStatusAsync(int requestId, OpenSessionRequestStatus newStatus, CancellationToken cancellationToken = default);

    Task<DateTime?> GetExpiresAtAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked draft aggregate for publish: sessions (units + time slot) and invitations.
    /// </summary>
    Task<OpenSessionRequest?> GetForPublishAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Student-facing detail graph (AsNoTracking) for response mapping after publish/create.
    /// </summary>
    Task<OpenSessionRequest?> GetStudentDetailAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pending OSR invitations for the given students (S2 inbox), projected to list DTOs.
    /// Does not set <see cref="StudentInvitationListItemDto.RespondByUtc"/>.
    /// </summary>
    Task<List<StudentInvitationListItemDto>> GetPendingInvitationListItemsAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 1: expire Draft / PendingInvitations / Active / ReceivingOffers past ExpiresAt or past
    /// the session-derived offer cutoff. Pending offers → Withdrawn.
    /// </summary>
    Task<List<ExpiredRequestResult>> ExpirePastCutoffRequestsAsync(
        DateTime nowUtc,
        OpenSessionRequestSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pending invitations older than the invite-response deadline → Expired;
    /// when no Pending remain on a PendingInvitations request → Active (any Accepted) or Cancelled.
    /// </summary>
    Task<List<InviteExpiryFinalizeResult>> ExpireStalePendingInvitationsAsync(
        DateTime nowUtc,
        int inviteResponseDeadlineHours,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 3: ReceivingOffers with zero Pending offers → Active.
    /// </summary>
    Task<List<int>> DemoteReceivingOffersWithoutLiveOffersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 4: PaymentPending whose linked enrollment is Cancelled → Expired.
    /// </summary>
    Task<List<SettledPaymentPendingResult>> SettleAbandonedPaymentPendingAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 5: candidates for expiry-soon nudges that have not yet reached the given stage.
    /// </summary>
    Task<List<ExpiryNudgeCandidate>> GetExpiryNudgeCandidatesAsync(
        DateTime nowUtc,
        int stageIndex,
        int hoursBeforeExpiry,
        CancellationToken cancellationToken = default);

    Task MarkExpiryNudgeStageAsync(int requestId, byte stage, CancellationToken cancellationToken = default);
}

/// <summary>Used by availability-match to compute conflicts without loading whole session graphs.</summary>
public record RequestSessionScheduleSlot(
    int Id,
    int SequenceNumber,
    DateOnly? PreferredDate,
    int? TimeSlotId,
    int DurationMinutes,
    TimeSpan? TimeSlotStart,
    TimeSpan? TimeSlotEnd);

/// <summary>Used by offer-create to gate state without loading the aggregate.</summary>
public record RequestStatusSummary(
    int Id,
    int StudentId,
    int RequestedByUserId,
    int? CreatedByGuardianId,
    OpenSessionRequestStatus Status,
    int? TargetedTeacherId = null);

public record ExpiredRequestResult(
    int RequestId,
    int RequestedByUserId,
    DateTime EffectiveExpiryUtc,
    bool Notify);

public record InviteExpiryFinalizeResult(
    int RequestId,
    int RequestedByUserId,
    int? TargetedTeacherId,
    bool BecameActive);

public record SettledPaymentPendingResult(
    int RequestId,
    int RequestedByUserId,
    DateTime? EnrollmentCancelledAt,
    bool Notify);

public record ExpiryNudgeCandidate(
    int RequestId,
    int RequestedByUserId,
    int? TargetedTeacherId,
    DateTime ExpiresAt,
    byte CurrentStage);
