using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentById;

public class GetTeacherEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherEnrollmentByIdQuery, Response<TeacherEnrollmentDetailDto>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public GetTeacherEnrollmentByIdQueryHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<TeacherEnrollmentDetailDto>> Handle(
        GetTeacherEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _enrollmentService.GetEnrollmentByIdAsync(
            request.UserId, request.Id, cancellationToken);

        if (dto == null)
            return NotFound<TeacherEnrollmentDetailDto>("Enrollment not found.");

        return Success(entity: dto);
    }
}
