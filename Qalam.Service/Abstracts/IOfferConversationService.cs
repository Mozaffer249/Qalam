using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Cross-cutting helper used by the P4 offer handlers (and P5 conversation handlers) to
/// post system messages and keep the (request, teacher) conversation in sync with the offer
/// lifecycle. Keeps "POST offer → 'تم تقديم العرض' system message" in one place.
/// </summary>
public interface IOfferConversationService
{
    /// <summary>
    /// Find-or-create the (request, teacher) conversation, sets its SessionOfferId pointer (or null
    /// to clear), and appends a system message. Returns the conversation row.
    /// </summary>
    Task<OfferConversation> RecordOfferLifecycleEventAsync(
        int sessionRequestId,
        int teacherId,
        int? sessionOfferId,
        OfferMessageType messageType,
        string content,
        CancellationToken cancellationToken = default);
}
