using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class OfferConversationService : IOfferConversationService
{
    private readonly IOfferConversationRepository _convRepo;

    public OfferConversationService(IOfferConversationRepository convRepo)
    {
        _convRepo = convRepo;
    }

    public async Task<OfferConversation> RecordOfferLifecycleEventAsync(
        int sessionRequestId,
        int teacherId,
        int? sessionOfferId,
        bool isOfferScoped,
        OfferMessageType messageType,
        string content,
        bool clearOfferPointerOnNull = false,
        CancellationToken cancellationToken = default)
    {
        OfferConversation conv;
        if (isOfferScoped)
        {
            if (!sessionOfferId.HasValue)
                throw new ArgumentException("sessionOfferId is required for offer-scoped conversations.", nameof(sessionOfferId));

            conv = await _convRepo.EnsureExistsForOfferAsync(
                sessionRequestId, teacherId, sessionOfferId.Value, cancellationToken);
        }
        else
        {
            conv = await _convRepo.EnsureExistsAsync(sessionRequestId, teacherId, cancellationToken);
            if (sessionOfferId.HasValue)
            {
                if (conv.SessionOfferId != sessionOfferId)
                    await _convRepo.SetCurrentOfferAsync(conv.Id, sessionOfferId, cancellationToken);
            }
            else if (clearOfferPointerOnNull && conv.SessionOfferId != null)
            {
                await _convRepo.SetCurrentOfferAsync(conv.Id, null, cancellationToken);
            }
        }

        await _convRepo.AppendMessageAsync(conv.Id, senderUserId: null, messageType, content, cancellationToken);
        return conv;
    }
}
