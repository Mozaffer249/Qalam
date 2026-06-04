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
        OfferMessageType messageType,
        string content,
        CancellationToken cancellationToken = default)
    {
        var conv = await _convRepo.EnsureExistsAsync(sessionRequestId, teacherId, cancellationToken);
        if (conv.SessionOfferId != sessionOfferId)
        {
            await _convRepo.SetCurrentOfferAsync(conv.Id, sessionOfferId, cancellationToken);
        }
        // System message: senderUserId is null.
        await _convRepo.AppendMessageAsync(conv.Id, senderUserId: null, messageType, content, cancellationToken);
        return conv;
    }
}
