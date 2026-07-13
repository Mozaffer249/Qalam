using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestsSummary;

public class GetAvailableRequestsSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestsSummaryQuery, Response<TeacherInboxSummaryDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IOpenSessionRequestTargetRepository _targetRepository;

    public GetAvailableRequestsSummaryQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        IOpenSessionRequestTargetRepository targetRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _targetRepository = targetRepository;
    }

    public async Task<Response<TeacherInboxSummaryDto>> Handle(
        GetAvailableRequestsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<TeacherInboxSummaryDto>("Teacher account not active.");

        var counts = await _targetRepository.GetTeacherInboxCountsAsync(teacher.Id, cancellationToken);
        return Success(entity: new TeacherInboxSummaryDto { Counts = counts });
    }
}
