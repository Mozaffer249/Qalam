using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentsList;

public class GetTeacherEnrollmentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherEnrollmentsListQuery, Response<List<TeacherEnrollmentListItemDto>>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public GetTeacherEnrollmentsListQueryHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<List<TeacherEnrollmentListItemDto>>> Handle(
        GetTeacherEnrollmentsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetEnrollmentsForTeacherAsync(
            request.UserId,
            request.Status,
            request.Source,
            request.Kind,
            request.SourceBadge,
            request.Search,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        if (result == null)
            return NotFound<List<TeacherEnrollmentListItemDto>>("Teacher profile not found.");

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
