using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Cross-cutting helper used by offer handlers to post system messages and keep
/// conversations in sync with the offer lifecycle (hybrid: targeted vs broadcast).
/// </summary>
public interface IOfferConversationService
{
    /// <summary>
    /// Find-or-create the appropriate conversation for the request mode, optionally update
    /// the SessionOfferId pointer (targeted only), and append a system message.
    /// </summary>
    /// <param name="isOfferScoped">
    /// True for broadcast (one conversation per offer). False for targeted (one per request+teacher).
    /// </param>
    /// <param name="clearOfferPointerOnNull">
    /// When <paramref name="sessionOfferId"/> is null and this is true (targeted withdraw),
    /// clears the request-scoped conversation's offer pointer. Broadcast withdraws leave the pointer.
    /// </param>
    Task<OfferConversation> RecordOfferLifecycleEventAsync(
        int sessionRequestId,
        int teacherId,
        int? sessionOfferId,
        bool isOfferScoped,
        OfferMessageType messageType,
        string content,
        bool clearOfferPointerOnNull = false,
        CancellationToken cancellationToken = default);
}
