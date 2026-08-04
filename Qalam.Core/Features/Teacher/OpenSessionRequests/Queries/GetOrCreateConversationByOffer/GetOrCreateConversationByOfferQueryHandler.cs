using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetOrCreateConversationByOffer;

public class GetOrCreateConversationByOfferQueryHandler : ResponseHandler,
    IRequestHandler<GetOrCreateConversationByOfferQuery, Response<OfferConversationDto>>
{
    private readonly ApplicationDBContext _context;
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOfferConversationRepository _convRepo;

    public GetOrCreateConversationByOfferQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ApplicationDBContext context,
        ITeacherRepository teacherRepo,
        IOfferConversationRepository convRepo) : base(localizer)
    {
        _context = context;
        _teacherRepo = teacherRepo;
        _convRepo = convRepo;
    }

    public async Task<Response<OfferConversationDto>> Handle(
        GetOrCreateConversationByOfferQuery request,
        CancellationToken cancellationToken)
    {
        var offerRow = await _context.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.Id == request.OfferId)
            .Select(o => new
            {
                o.Id,
                o.SessionRequestId,
                o.TeacherId,
                TargetedTeacherId = o.OpenSessionRequest.TargetedTeacherId,
                RequestedByUserId = o.OpenSessionRequest.RequestedByUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (offerRow == null)
            return NotFound<OfferConversationDto>("Offer not found.");

        var teacher = await _teacherRepo.GetByIdAsync(offerRow.TeacherId);
        if (teacher == null)
            return NotFound<OfferConversationDto>("Teacher not found.");

        ConversationCaller caller;
        if (teacher.UserId == request.UserId)
            caller = ConversationCaller.Teacher;
        else if (offerRow.RequestedByUserId == request.UserId)
            caller = ConversationCaller.Student;
        else
            return Forbidden<OfferConversationDto>("NOT_A_PARTICIPANT");

        var isTargeted = offerRow.TargetedTeacherId != null;
        OfferConversationDto? dto;

        if (isTargeted)
        {
            // Targeted: same request-scoped thread; point at this offer if needed.
            var conv = await _convRepo.EnsureExistsAsync(offerRow.SessionRequestId, offerRow.TeacherId, cancellationToken);
            if (conv.SessionOfferId != offerRow.Id)
                await _convRepo.SetCurrentOfferAsync(conv.Id, offerRow.Id, cancellationToken);
            dto = await _convRepo.GetHeaderDtoAsync(conv.Id, caller, cancellationToken);
        }
        else
        {
            var conv = await _convRepo.EnsureExistsForOfferAsync(
                offerRow.SessionRequestId, offerRow.TeacherId, offerRow.Id, cancellationToken);
            dto = await _convRepo.GetHeaderDtoAsync(conv.Id, caller, cancellationToken);
        }

        if (dto == null)
            return NotFound<OfferConversationDto>("Conversation could not be loaded.");

        return Success(entity: dto);
    }
}
