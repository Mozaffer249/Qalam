using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;

public class GetMyEnrollmentsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentsQuery, Response<List<EnrollmentListItemDto>>>
{
    private readonly IGuardianChildrenService _guardianChildrenService;
    private readonly IStudentEnrollmentQueryService _enrollmentQueryService;

    public GetMyEnrollmentsQueryHandler(
        IGuardianChildrenService guardianChildrenService,
        IStudentEnrollmentQueryService enrollmentQueryService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianChildrenService = guardianChildrenService;
        _enrollmentQueryService = enrollmentQueryService;
    }

    public async Task<Response<List<EnrollmentListItemDto>>> Handle(
        GetMyEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = await _guardianChildrenService.ResolveTargetStudentIdAsync(
            request.UserId,
            request.StudentId,
            cancellationToken);

        if (studentId == null)
            return NotFound<List<EnrollmentListItemDto>>("Student not found.");

        var (items, totalCount) = await _enrollmentQueryService.ListForStudentAsync(
            studentId.Value,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, totalCount));
    }
}
