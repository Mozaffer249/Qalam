using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherStatusSummary;

public class GetTeacherStatusSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherStatusSummaryQuery, Response<AdminTeacherStatusSummaryDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherAccessSettingsProvider _teacherAccessSettingsProvider;

    public GetTeacherStatusSummaryQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherAccessSettingsProvider teacherAccessSettingsProvider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _teacherAccessSettingsProvider = teacherAccessSettingsProvider;
    }

    public async Task<Response<AdminTeacherStatusSummaryDto>> Handle(
        GetTeacherStatusSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var access = await _teacherAccessSettingsProvider.GetSettingsAsync(cancellationToken);
        var summary = await _teacherRepository.GetStatusSummaryAsync(
            includeAwaitingPlatformLaunch: !access.TeacherDashboardReady,
            cancellationToken);
        return Success(entity: summary);
    }
}
