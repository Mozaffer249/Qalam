using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.MarkAvailableRequestViewed;

public class MarkAvailableRequestViewedCommandHandler : ResponseHandler,
    IRequestHandler<MarkAvailableRequestViewedCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;

    public MarkAvailableRequestViewedCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestTargetRepository targetRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _targetRepo = targetRepo;
    }

    public async Task<Response<string>> Handle(MarkAvailableRequestViewedCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<string>("Teacher account not active.");

        var existing = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (existing == null)
            return Forbidden<string>("NOT_MATCHED");

        // Idempotent — only bump if currently Notified.
        if (existing.Status == OpenSessionRequestTargetStatus.Notified)
        {
            await _targetRepo.SetStatusAsync(request.RequestId, teacher.Id, OpenSessionRequestTargetStatus.Viewed, cancellationToken);
        }

        return Success(entity: "Marked as viewed.");
    }
}
