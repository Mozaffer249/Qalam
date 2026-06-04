using System.Globalization;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetConversationMessages;

public class GetConversationMessagesQueryHandler : ResponseHandler,
    IRequestHandler<GetConversationMessagesQuery, Response<ConversationMessagesPageDto>>
{
    private readonly IOfferConversationRepository _convRepo;

    public GetConversationMessagesQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IOfferConversationRepository convRepo) : base(localizer)
    {
        _convRepo = convRepo;
    }

    public async Task<Response<ConversationMessagesPageDto>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var participant = await _convRepo.ResolveParticipantAsync(request.ConversationId, request.UserId, cancellationToken);
        if (participant == null)
            return Forbidden<ConversationMessagesPageDto>("NOT_A_PARTICIPANT");

        DateTime? cursor = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor)
            && DateTime.TryParse(request.Cursor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            cursor = parsed;
        }

        var older = !string.Equals(request.Direction, "newer", StringComparison.OrdinalIgnoreCase);
        var page = await _convRepo.GetMessagesPageAsync(request.ConversationId, cursor, request.Take, older, cancellationToken);
        return Success(entity: page);
    }
}
