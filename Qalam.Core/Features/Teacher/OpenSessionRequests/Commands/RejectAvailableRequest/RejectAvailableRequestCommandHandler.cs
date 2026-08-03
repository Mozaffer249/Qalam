using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.RejectAvailableRequest;

public class RejectAvailableRequestCommandHandler : ResponseHandler,
    IRequestHandler<RejectAvailableRequestCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;

    public RejectAvailableRequestCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionRequestTargetRepository targetRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _requestRepo = requestRepo;
        _targetRepo = targetRepo;
    }

    public async Task<Response<string>> Handle(RejectAvailableRequestCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<string>("Teacher account not active.");

        var existing = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (existing == null)
            return Forbidden<string>("NOT_MATCHED");

        if (existing.Status == OpenSessionRequestTargetStatus.OfferSubmitted)
            return BadRequest<string>("OFFER_ALREADY_SUBMITTED");

        var entity = await _requestRepo.GetByIdAsync(request.RequestId);
        if (entity == null)
            return NotFound<string>("Request not found.");

        if (entity.TargetedTeacherId == null || entity.TargetedTeacherId != teacher.Id)
            return BadRequest<string>("NOT_TARGETED");

        if (entity.Status is not (OpenSessionRequestStatus.Active or OpenSessionRequestStatus.ReceivingOffers))
            return Conflict<string>("REQUEST_NOT_ACTIVE");

        var now = DateTime.UtcNow;
        entity.Status = OpenSessionRequestStatus.Rejected;
        entity.CancelledAt = now;
        entity.UpdatedAt = now;
        await _requestRepo.UpdateAsync(entity);

        await _targetRepo.SetStatusAsync(
            request.RequestId, teacher.Id, OpenSessionRequestTargetStatus.Skipped, cancellationToken);

        return Success(entity: "تم رفض الطلب");
    }
}
