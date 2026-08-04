using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetOrCreateConversationByRequest;

public class GetOrCreateConversationByRequestQueryHandler : ResponseHandler,
    IRequestHandler<GetOrCreateConversationByRequestQuery, Response<OfferConversationDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOfferConversationRepository _convRepo;

    public GetOrCreateConversationByRequestQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOfferConversationRepository convRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _convRepo = convRepo;
    }

    public async Task<Response<OfferConversationDto>> Handle(
        GetOrCreateConversationByRequestQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _requestRepo.GetStatusSummaryAsync(request.RequestId, cancellationToken);
        if (summary == null)
            return NotFound<OfferConversationDto>("Request not found.");

        // Broadcast requests use offer-scoped chat — clients must call by-offer.
        if (summary.TargetedTeacherId == null)
            return BadRequest<OfferConversationDto>("BROADCAST_USE_BY_OFFER");

        var teacher = await _teacherRepo.GetByIdAsync(request.TeacherId);
        if (teacher == null)
            return NotFound<OfferConversationDto>("Teacher not found.");

        ConversationCaller caller;
        if (teacher.UserId == request.UserId)
            caller = ConversationCaller.Teacher;
        else if (summary.RequestedByUserId == request.UserId)
            caller = ConversationCaller.Student;
        else
            return Forbidden<OfferConversationDto>("NOT_A_PARTICIPANT");

        var conv = await _convRepo.EnsureExistsAsync(request.RequestId, request.TeacherId, cancellationToken);
        var dto = await _convRepo.GetHeaderDtoAsync(conv.Id, caller, cancellationToken);
        if (dto == null)
            return NotFound<OfferConversationDto>("Conversation could not be loaded.");

        return Success(entity: dto);
    }
}
