using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetMyOfferById;

public class GetMyOfferByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyOfferByIdQuery, Response<TeacherOfferDetailDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;

    public GetMyOfferByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionOfferRepository offerRepo,
        IOpenSessionRequestRepository requestRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _offerRepo = offerRepo;
        _requestRepo = requestRepo;
    }

    public async Task<Response<TeacherOfferDetailDto>> Handle(GetMyOfferByIdQuery request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<TeacherOfferDetailDto>("Teacher account not active.");

        var detail = await _offerRepo.GetTeacherDetailDtoAsync(request.OfferId, teacher.Id, cancellationToken);
        if (detail == null)
            return NotFound<TeacherOfferDetailDto>("Offer not found.");

        // Convenience: also include the parent request snapshot so the UI doesn't need a separate call.
        detail.Request = await _requestRepo.GetTeacherDetailDtoAsync(detail.SessionRequestId, cancellationToken);

        return Success(entity: detail);
    }
}
