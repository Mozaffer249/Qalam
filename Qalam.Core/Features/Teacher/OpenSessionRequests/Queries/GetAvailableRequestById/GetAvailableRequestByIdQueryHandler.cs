using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestById;

public class GetAvailableRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestByIdQuery, Response<TeacherAvailableRequestDetailDto>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;

    public GetAvailableRequestByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        IOpenSessionOfferRepository offerRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _targetRepo = targetRepo;
        _offerRepo = offerRepo;
    }

    public async Task<Response<TeacherAvailableRequestDetailDto>> Handle(
        GetAvailableRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<TeacherAvailableRequestDetailDto>("Teacher account not active.");

        // Authorization: the teacher must be targeted on this request.
        var target = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (target == null)
            return Forbidden<TeacherAvailableRequestDetailDto>("NOT_MATCHED");

        var detail = await _requestRepo.GetTeacherDetailDtoAsync(request.RequestId, cancellationToken);
        if (detail == null)
            return NotFound<TeacherAvailableRequestDetailDto>("Request not found.");

        // Side effect: flip the target row to Viewed on first detail open.
        if (target.Status == OpenSessionRequestTargetStatus.Notified)
        {
            await _targetRepo.SetStatusAsync(request.RequestId, teacher.Id, OpenSessionRequestTargetStatus.Viewed, cancellationToken);
        }

        // Hydrate the caller's own offer state for the UI (button labels + sticky card).
        var existing = await _offerRepo.GetExistingActiveOfferAsync(request.RequestId, teacher.Id, cancellationToken);
        if (existing != null)
        {
            detail.MyOfferId = existing.Value.OfferId;
            detail.MyOfferStatus = existing.Value.Status;
        }

        return Success(entity: detail);
    }
}
