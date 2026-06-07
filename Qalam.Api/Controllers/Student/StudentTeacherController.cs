using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Teachers.Queries.GetRecommendedTeachers;
using Qalam.Core.Features.Student.Teachers.Queries.GetTeachersList;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Student-facing teacher discovery — recommended (profile-narrowed) + paginated list with filters.
/// Both card responses' <c>id</c> field plugs straight into
/// <c>CreateOpenSessionRequestDto.targetedTeacherId</c> for the Scenario 2 targeted-teacher flow.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentTeacherController : AppControllerBase
{
    /// <summary>
    /// Top-N teachers narrowed by the student's profile (Domain → Level → Grade where set on the student).
    /// Ordered by RatingAverage DESC, approved-reviews count DESC, CreatedAt DESC.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Teachers/Recommended[?studentId=...&amp;take=...]
    ///
    /// - Omit <c>studentId</c> (or send 0) → server resolves the caller's own student profile.
    /// - Guardians acting on behalf of a child must send the child's <c>studentId</c>.
    /// - <c>take</c> defaults to 8.
    /// </remarks>
    [HttpGet(Router.StudentRecommendedTeachers)]
    [ProducesResponseType(typeof(List<TeacherCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommended([FromQuery] GetRecommendedTeachersQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Paginated list with filters — use this as the "Find a Teacher" picker that feeds the
    /// scenario-2 targeted-teacher Open Session Request.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Teachers[?subjectId=&amp;domainId=&amp;levelId=&amp;gradeId=&amp;quranContentTypeId=&amp;quranLevelId=&amp;location=&amp;minRating=&amp;search=&amp;sortBy=Rating|Newest|NameAsc&amp;pageNumber=&amp;pageSize=]
    ///
    /// Every filter is optional and AND-combined when supplied. <c>pageSize</c> is clamped to 50.
    /// Server only returns teachers whose <c>Status</c> is <c>Active</c> and <c>IsActive == true</c>.
    /// </remarks>
    [HttpGet(Router.StudentTeachers)]
    [ProducesResponseType(typeof(List<TeacherCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList([FromQuery] GetTeachersListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }
}
