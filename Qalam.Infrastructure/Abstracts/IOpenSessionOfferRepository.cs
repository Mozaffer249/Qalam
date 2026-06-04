using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IOpenSessionOfferRepository : IGenericRepositoryAsync<OpenSessionOffer>
{
    /// <summary>
    /// Returns the existing non-Withdrawn offer's (id, status) for the (request, teacher) pair, or null.
    /// Used by POST /Offers to either reject as 409 or allow re-creation after a previous withdraw.
    /// </summary>
    Task<(int OfferId, OpenSessionOfferStatus Status)?> GetExistingActiveOfferAsync(int requestId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the offer with its parent request shallow data so the offer handlers can authorize +
    /// gate on Status without separate queries. Returns null if not found.
    /// </summary>
    Task<OpenSessionOffer?> GetByIdForOwnerActionAsync(int offerId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>Single-offer detail projection for the teacher.</summary>
    Task<TeacherOfferDetailDto?> GetTeacherDetailDtoAsync(int offerId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>Paginated list for the teacher's "my offers" view.</summary>
    Task<PaginatedResult<TeacherOfferListItemDto>> GetTeacherMyOffersAsync(
        int teacherId,
        TeacherMyOffersFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Background-service expiry sweep — flips pending offers past their ExpiresAt to Expired.
    /// Returns the affected offer ids (so the caller can dispatch notifications).
    /// </summary>
    Task<List<int>> ExpirePendingOffersAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}

public record TeacherMyOffersFilters(
    OpenSessionOfferStatus? Status,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int PageNumber,
    int PageSize);
