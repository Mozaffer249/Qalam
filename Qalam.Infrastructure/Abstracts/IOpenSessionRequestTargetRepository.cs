using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IOpenSessionRequestTargetRepository : IGenericRepositoryAsync<OpenSessionRequestTarget>
{
    /// <summary>
    /// IDs of teachers already targeted on this request. Used by the matching service to stay idempotent.
    /// </summary>
    Task<List<int>> GetTargetedTeacherIdsAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>Bulk-insert new targets. Caller computes the rows.</summary>
    Task BulkInsertAsync(IEnumerable<OpenSessionRequestTarget> targets, CancellationToken cancellationToken = default);

    /// <summary>Lookup a single target row by (requestId, teacherId).</summary>
    Task<OpenSessionRequestTarget?> GetByRequestAndTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets Status = newStatus (and ViewedAt when transitioning to Viewed) atomically.
    /// No-op if the row doesn't exist. Returns true if a row was updated.
    /// </summary>
    Task<bool> SetStatusAsync(int requestId, int teacherId, OpenSessionRequestTargetStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated list for the teacher inbox. Projects directly to TeacherAvailableRequestListItemDto via .Select.
    /// </summary>
    Task<PaginatedResult<TeacherAvailableRequestListItemDto>> GetTeacherInboxAsync(
        int teacherId,
        TeacherInboxFilters filters,
        CancellationToken cancellationToken = default);
}

public record TeacherInboxFilters(
    OpenSessionRequestTargetStatus? Status,
    int? SubjectId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int PageNumber,
    int PageSize,
    TeacherInboxSort SortBy);

public enum TeacherInboxSort
{
    Newest = 1,
    ExpiringSoon = 2,
    MostOffers = 3
}
