using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
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
    OpenSessionRequestStatus Status);
