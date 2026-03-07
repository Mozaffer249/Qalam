using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Messaging;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Queries.GetMessageHistory;

public class GetMessageHistoryQueryHandler : ResponseHandler, IRequestHandler<GetMessageHistoryQuery, Response<List<MessageLog>>>
{
    private readonly IMessageTrackingService _messageTrackingService;

    public GetMessageHistoryQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IMessageTrackingService messageTrackingService) : base(localizer)
    {
        _messageTrackingService = messageTrackingService;
    }

    public async Task<Response<List<MessageLog>>> Handle(GetMessageHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _messageTrackingService.GetMessageHistoryAsync(request.PageNumber, request.PageSize);
        return Success("Message history retrieved", history);
    }
}
