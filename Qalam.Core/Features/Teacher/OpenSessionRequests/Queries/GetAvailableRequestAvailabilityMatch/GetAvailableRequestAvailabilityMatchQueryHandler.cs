using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestAvailabilityMatch;

public class GetAvailableRequestAvailabilityMatchQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestAvailabilityMatchQuery, Response<List<SessionAvailabilityMatchDto>>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly ISessionAvailabilityMatchService _matchService;

    public GetAvailableRequestAvailabilityMatchQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        ISessionAvailabilityMatchService matchService) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _targetRepo = targetRepo;
        _matchService = matchService;
    }

    public async Task<Response<List<SessionAvailabilityMatchDto>>> Handle(
        GetAvailableRequestAvailabilityMatchQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<List<SessionAvailabilityMatchDto>>("Teacher account not active.");

        var target = await _targetRepo.GetByRequestAndTeacherAsync(request.RequestId, teacher.Id, cancellationToken);
        if (target == null)
            return Forbidden<List<SessionAvailabilityMatchDto>>("NOT_MATCHED");

        var result = await _matchService.MatchAsync(teacher.Id, request.RequestId, cancellationToken);
        return Success(entity: result);
    }
}
