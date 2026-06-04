using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.DismissAvailableRequest;

public class DismissAvailableRequestCommandHandler : ResponseHandler,
    IRequestHandler<DismissAvailableRequestCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;

    public DismissAvailableRequestCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestTargetRepository targetRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _targetRepo = targetRepo;
    }

    public async Task<Response<string>> Handle(DismissAvailableRequestCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<string>("Teacher account not active.");

        var existing = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (existing == null)
            return Forbidden<string>("NOT_MATCHED");

        // Can't dismiss after committing an offer — those need explicit withdraw instead.
        if (existing.Status == OpenSessionRequestTargetStatus.OfferSubmitted)
            return BadRequest<string>("OFFER_ALREADY_SUBMITTED");

        await _targetRepo.SetStatusAsync(request.RequestId, teacher.Id, OpenSessionRequestTargetStatus.Skipped, cancellationToken);
        return Success(entity: "تم تجاهل الطلب");
    }
}
