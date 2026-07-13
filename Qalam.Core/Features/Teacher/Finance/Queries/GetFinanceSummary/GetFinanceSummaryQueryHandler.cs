using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceSummary;

public class GetFinanceSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetFinanceSummaryQuery, Response<TeacherFinanceSummaryDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;

    public GetFinanceSummaryQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Response<TeacherFinanceSummaryDto>> Handle(
        GetFinanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherFinanceSummaryDto>("Teacher not found");

        var summary = await _dashboardRepository.GetFinanceSummaryAsync(teacher.Id, cancellationToken);
        return Success(entity: summary);
    }
}
