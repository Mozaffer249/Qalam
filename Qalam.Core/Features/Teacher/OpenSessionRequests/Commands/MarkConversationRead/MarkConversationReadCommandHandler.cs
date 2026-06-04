using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.MarkConversationRead;

public class MarkConversationReadCommandHandler : ResponseHandler,
    IRequestHandler<MarkConversationReadCommand, Response<string>>
{
    private readonly IOfferConversationRepository _convRepo;

    public MarkConversationReadCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IOfferConversationRepository convRepo) : base(localizer)
    {
        _convRepo = convRepo;
    }

    public async Task<Response<string>> Handle(
        MarkConversationReadCommand request,
        CancellationToken cancellationToken)
    {
        var participant = await _convRepo.ResolveParticipantAsync(request.ConversationId, request.UserId, cancellationToken);
        if (participant == null)
            return Forbidden<string>("NOT_A_PARTICIPANT");

        await _convRepo.MarkReadAsync(request.ConversationId, participant.CallerRole, cancellationToken);
        return Success(entity: "تم تحديث حالة القراءة");
    }
}
