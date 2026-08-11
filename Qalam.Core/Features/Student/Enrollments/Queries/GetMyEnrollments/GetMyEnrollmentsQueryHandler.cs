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
        var owned = await _guardianChildrenService.GetOwnedStudentIdsAsync(
            request.UserId,
            cancellationToken);

        if (owned.Count == 0)
        {
            return Success(
                entity: new List<EnrollmentListItemDto>(),
                Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, 0));
        }

        IReadOnlyCollection<int> queryStudentIds;
        if (request.StudentId is int requestedId)
        {
            if (!owned.Contains(requestedId))
                return NotFound<List<EnrollmentListItemDto>>("Student not found.");

            queryStudentIds = [requestedId];
        }
        else
        {
            queryStudentIds = owned;
        }

        var (items, totalCount) = await _enrollmentQueryService.ListForStudentsAsync(
            queryStudentIds,
            owned,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, totalCount));
    }
}
