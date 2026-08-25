using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Students.Queries.GetAdminStudentById;
using Qalam.Core.Features.Admin.Students.Queries.GetAdminStudentsList;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// Admin read-only endpoints for browsing students and opening a student file.
/// </summary>
[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Students")]
public class StudentManagementController : AppControllerBase
{
    /// <summary>
    /// Paginated list of students with optional search / minor / active filters.
    /// </summary>
    [HttpGet(Router.AdminStudents)]
    [ProducesResponseType(typeof(List<AdminStudentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isMinor = null,
        [FromQuery] bool? isActive = null)
    {
        return NewResult(await Mediator.Send(new GetAdminStudentsListQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            IsMinor = isMinor,
            IsActive = isActive
        }));
    }

    /// <summary>
    /// Student file: profile, guardian (if minor), and children when this user owns a guardian profile.
    /// </summary>
    [HttpGet(Router.AdminStudentById)]
    [ProducesResponseType(typeof(AdminStudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById([FromRoute] int studentId)
    {
        return NewResult(await Mediator.Send(new GetAdminStudentByIdQuery
        {
            StudentId = studentId
        }));
    }
}
