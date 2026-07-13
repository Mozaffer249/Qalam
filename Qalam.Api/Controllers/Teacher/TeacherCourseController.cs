using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.CreateCourse;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.DeleteCourse;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourse;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSessionUnits;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.UploadCourseImage;
using Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCourseById;
using Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCoursesList;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Course;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher course management: teachers create, update, delete and list their own courses.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherCourse)]
public class TeacherCourseController : AppControllerBase
{
    /// <summary>
    /// Get paginated list of the current teacher's courses.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/TeacherCourse
    /// Sample query: ?PageNumber=1&amp;PageSize=10&amp;DomainId=1&amp;Status=Draft&amp;SubjectId=2
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "data": {
    ///     "items": [
    ///       {
    ///         "id": 1,
    ///         "title": "Mathematics - Grade 10",
    ///         "descriptionShort": "Full curriculum algebra and geometry.",
    ///         "teacherId": 5,
    ///         "domainId": 1,
    ///         "domainNameEn": "Academic",
    ///         "subjectId": 2,
    ///         "subjectNameEn": "Mathematics",
    ///         "teachingModeId": 1,
    ///         "teachingModeNameEn": "Online",
    ///         "sessionTypeId": 1,
    ///         "sessionTypeNameEn": "Group",
    ///         "status": "Draft",
    ///         "isActive": true,
    ///         "price": 500.00
    ///       }
    ///     ],
    ///     "pageNumber": 1,
    ///     "pageSize": 10,
    ///     "totalCount": 5
    ///   },
    ///   "succeeded": true
    /// }
    /// </code>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] GetCoursesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get course by ID (own course only).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/TeacherCourse/{id}
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "data": {
    ///     "id": 1,
    ///     "title": "Mathematics - Grade 10",
    ///     "description": "Full curriculum algebra and geometry.",
    ///     "isActive": true,
    ///     "teacherId": 5,
    ///     "teacherDisplayName": "Ahmed Ali",
    ///     "domainId": 1,
    ///     "domainNameEn": "Academic",
    ///     "teacherSubjectId": 1,
    ///     "subjectNameEn": "Mathematics",
    ///     "curriculumId": 1,
    ///     "curriculumNameEn": "Saudi National",
    ///     "levelId": 2,
    ///     "levelNameEn": "Secondary",
    ///     "gradeId": 4,
    ///     "gradeNameEn": "Grade 10",
    ///     "teachingModeId": 1,
    ///     "teachingModeNameEn": "Online",
    ///     "sessionTypeId": 1,
    ///     "sessionTypeNameEn": "Group",
    ///     "isFlexible": false,
    ///     "sessionsCount": 3,
    ///     "sessionDurationMinutes": 45,
    ///     "price": 500.00,
    ///     "maxStudents": 15,
    ///     "canIncludeInPackages": true,
    ///     "status": "Published",
    ///     "units": null,
    ///     "sessions": [
    ///       { "id": 101, "sessionNumber": 1, "durationMinutes": 45, "title": "Intro",      "notes": null },
    ///       { "id": 102, "sessionNumber": 2, "durationMinutes": 45, "title": "Equations",  "notes": null },
    ///       { "id": 103, "sessionNumber": 3, "durationMinutes": 60, "title": "Quadratics", "notes": "Bring calculator." }
    ///     ]
    ///   },
    ///   "succeeded": true
    /// }
    /// </code>
    /// `sessionsCount` is derived from `sessions.length` on read (null for flexible courses).
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var query = new GetCourseByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Create a new course (Published).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Teacher/TeacherCourse
    ///
    /// Two modes:
    /// - Non-flexible (`isFlexible = false`): `sessionDurationMinutes` is required, and `sessions` must be a non-empty list. The array order is the session order — the server assigns `sessionNumber = 1..N`. Do NOT send `sessionNumber`.
    /// - Flexible (`isFlexible = true`): `sessionDurationMinutes` must be null and `sessions` must be null/empty.
    ///
    /// Per-session unit coverage (non-flexible only):
    /// - Each `sessions[]` item may include an optional `units[]` array (max 20 per session) that tags which `ContentUnit` or `Lesson` is covered.
    /// - Each `units[]` item must set EXACTLY ONE of `contentUnitId` or `lessonId` (not both, not neither).
    /// - Every referenced `ContentUnit` / `Lesson` must belong to the course's subject (the one resolved from `teacherSubjectId`). Cross-subject selections are rejected with 400.
    /// - Sending `units: []` or omitting it is valid — the session simply has no curriculum tagging.
    ///
    /// Sample request body (non-flexible, with per-session units):
    /// <code>
    /// {
    ///   "title": "Mathematics - Grade 10",
    ///   "description": "Full curriculum algebra and geometry.",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 1,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": false,
    ///   "sessionDurationMinutes": 45,
    ///   "price": 500,
    ///   "maxStudents": 15,
    ///   "canIncludeInPackages": true,
    ///   "sessions": [
    ///     { "durationMinutes": 45, "title": "Intro",      "notes": null, "units": [ { "contentUnitId": 3 }, { "contentUnitId": 4 } ] },
    ///     { "durationMinutes": 45, "title": "Equations",  "notes": null, "units": [ { "lessonId": 21 }, { "lessonId": 22 } ] },
    ///     { "durationMinutes": 60, "title": "Quadratics", "notes": "Bring calculator.", "units": [ { "contentUnitId": 5 }, { "lessonId": 31 } ] }
    ///   ]
    /// }
    /// </code>
    ///
    /// Sample request body (flexible):
    /// <code>
    /// {
    ///   "title": "On-demand Tutoring",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 2,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": true,
    ///   "sessionDurationMinutes": null,
    ///   "price": 40,
    ///   "maxStudents": null,
    ///   "canIncludeInPackages": false,
    ///   "sessions": null
    /// }
    /// </code>
    ///
    /// On success returns the full <see cref="CourseDetailDto"/> with `status = Published`, `sessions[]` ordered by `sessionNumber` ascending, and each `sessions[].units[]` hydrated with `contentUnitNameEn/Ar` or `lessonNameEn/Ar` so the UI can render without an extra fetch.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var command = new CreateCourseCommand { Data = dto };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Upload a course cover image (jpg, jpeg, png, webp; max 5 MB).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Teacher/TeacherCourse/upload-image (multipart/form-data, field name: file)
    ///
    /// Returns `{ "imageUrl": "uploads/courses/{teacherId}/{guid}.jpg" }` to include in create/update payload.
    /// </remarks>
    [HttpPost("upload-image")]
    [ProducesResponseType(typeof(CourseImageUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCourseImage(IFormFile file)
    {
        var command = new UploadCourseImageCommand { File = file };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing course.
    /// </summary>
    /// <remarks>
    /// PUT Api/V1/Teacher/TeacherCourse/{id}
    ///
    /// Sample request body:
    /// <code>
    /// {
    ///   "title": "Mathematics - Grade 10 (Updated)",
    ///   "description": "Updated curriculum with new topics.",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 1,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": false,
    ///   "sessionDurationMinutes": 60,
    ///   "price": 600,
    ///   "maxStudents": 20,
    ///   "canIncludeInPackages": true
    /// }
    /// </code>
    ///
    /// Note: this endpoint does not edit `sessions[]`. Use the dedicated session management endpoints to change the session outline.
    /// </remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
    {
        var command = new UpdateCourseCommand { Id = id, Data = dto };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Replace the unit/lesson coverage for a single course session.
    /// </summary>
    /// <remarks>
    /// PUT Api/V1/Teacher/TeacherCourse/{courseId}/Sessions/{sessionId}/Units
    ///
    /// Sample request body:
    /// <code>
    /// {
    ///   "units": [
    ///     { "contentUnitId": 3 },
    ///     { "lessonId": 21 }
    ///   ]
    /// }
    /// </code>
    ///
    /// Behavior: full-replace — all existing CourseSessionUnit rows on the session are deleted and
    /// the new set is inserted in a single round trip. Each unit must set exactly one of
    /// `contentUnitId` or `lessonId`, and the referenced unit/lesson must belong to the course's
    /// subject. Max 20 units per session.
    /// </remarks>
    [HttpPut("{courseId:int}/Sessions/{sessionId:int}/Units")]
    [ProducesResponseType(typeof(List<CourseSessionUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSessionUnits(
        int courseId,
        int sessionId,
        [FromBody] UpdateCourseSessionUnitsDto dto)
    {
        var command = new UpdateCourseSessionUnitsCommand
        {
            CourseId = courseId,
            SessionId = sessionId,
            Data = dto
        };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a course (soft if has enrollments, hard otherwise).
    /// </summary>
    /// <remarks>
    /// DELETE Api/V1/Teacher/TeacherCourse/{id}
    ///
    /// Soft-deletes if course has enrollments (sets IsActive = false), hard-deletes otherwise.
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "succeeded": true,
    ///   "message": "Deleted Successfully"
    /// }
    /// </code>
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var cmd = new DeleteCourseCommand { Id = id };
        return NewResult(await Mediator.Send(cmd));
    }
}
