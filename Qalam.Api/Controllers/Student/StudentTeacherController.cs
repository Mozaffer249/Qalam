using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Teachers.Queries.GetRecommendedTeachers;
using Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherCertificates;
using Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherProfile;
using Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherReviews;
using Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherSubjects;
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

    [HttpGet(Router.StudentTeachers)]
    [ProducesResponseType(typeof(List<TeacherCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList([FromQuery] GetTeachersListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    [HttpGet(Router.StudentTeacherById)]
    [ProducesResponseType(typeof(StudentTeacherProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int teacherId)
    {
        return NewResult(await Mediator.Send(new GetStudentTeacherProfileQuery { TeacherId = teacherId }));
    }

    [HttpGet(Router.StudentTeacherSubjects)]
    [ProducesResponseType(typeof(List<StudentTeacherSubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubjects(int teacherId)
    {
        return NewResult(await Mediator.Send(new GetStudentTeacherSubjectsQuery { TeacherId = teacherId }));
    }

    [HttpGet(Router.StudentTeacherReviews)]
    [ProducesResponseType(typeof(List<StudentTeacherReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviews(int teacherId, [FromQuery] GetStudentTeacherReviewsQuery query)
    {
        query.TeacherId = teacherId;
        return NewResult(await Mediator.Send(query));
    }

    [HttpGet(Router.StudentTeacherCertificates)]
    [ProducesResponseType(typeof(List<StudentTeacherCertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCertificates(int teacherId, [FromQuery] GetStudentTeacherCertificatesQuery query)
    {
        query.TeacherId = teacherId;
        return NewResult(await Mediator.Send(query));
    }
}
