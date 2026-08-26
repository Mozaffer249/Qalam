using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;
using System.Net;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Enrollments")]
public class EnrollmentsController : AppControllerBase
{
    private readonly IAdminEnrollmentQueryService _enrollments;

    public EnrollmentsController(IAdminEnrollmentQueryService enrollments)
    {
        _enrollments = enrollments;
    }

    [HttpGet(Router.AdminEnrollments)]
    [ProducesResponseType(typeof(List<AdminEnrollmentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] EnrollmentStatus? status = null,
        [FromQuery] EnrollmentSource? source = null,
        [FromQuery] EnrollmentKind? kind = null,
        [FromQuery] bool? isFreeTrial = null,
        [FromQuery] int? teacherId = null,
        [FromQuery] int? studentId = null,
        [FromQuery] int? courseId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _enrollments.ListAsync(new AdminEnrollmentListFilter
        {
            Status = status,
            Source = source,
            Kind = kind,
            IsFreeTrial = isFreeTrial,
            TeacherId = teacherId,
            StudentId = studentId,
            CourseId = courseId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        }, cancellationToken);
        return NewResult(OkResponse(items));
    }

    [HttpGet(Router.AdminEnrollmentById)]
    [ProducesResponseType(typeof(AdminEnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _enrollments.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NewResult(FailResponse<AdminEnrollmentDetailDto>("Enrollment not found.", HttpStatusCode.NotFound));
        return NewResult(OkResponse(item));
    }

    private static Response<T> OkResponse<T>(T data) => new(data)
    {
        StatusCode = HttpStatusCode.OK,
        Succeeded = true,
        Message = "Success"
    };

    private static Response<T> FailResponse<T>(string message, HttpStatusCode code) => new(message)
    {
        StatusCode = code,
        Succeeded = false
    };
}
