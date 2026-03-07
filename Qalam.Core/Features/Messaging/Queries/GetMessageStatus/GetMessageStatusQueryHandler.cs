using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Messaging;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Queries.GetMessageStatus;

public class GetMessageStatusQueryHandler : ResponseHandler, IRequestHandler<GetMessageStatusQuery, Response<MessageLog>>
{
    private readonly IMessageTrackingService _messageTrackingService;

    public GetMessageStatusQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IMessageTrackingService messageTrackingService) : base(localizer)
    {
        _messageTrackingService = messageTrackingService;
    }

    public async Task<Response<MessageLog>> Handle(GetMessageStatusQuery request, CancellationToken cancellationToken)
    {
        var messageLog = await _messageTrackingService.GetMessageStatusAsync(request.MessageId);

        if (messageLog is null)
            return NotFound<MessageLog>("Message not found");

        return Success("Message status retrieved", messageLog);
    }
}
