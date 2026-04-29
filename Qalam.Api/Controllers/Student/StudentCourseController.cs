using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Availability.Queries.GetTeacherAvailabilityByRange;
using Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;
using Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;
using Qalam.Core.Features.Student.CourseCatalog.Queries.GetRecommendedCourses;
using Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;
using Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequestById;
using Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequests;
using Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitations;
using Qalam.Core.Features.Student.EnrollmentRequests.Queries.SearchStudentsForGroup;
using Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollmentById;
using Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;
using Qalam.Core.Features.Student.Queries.SearchStudents;
using Qalam.Core.Features.Student.Queries.GetMyChildren;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Student;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Student course catalog, enrollment requests, and my enrollments.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentCourseController : AppControllerBase
{
    /// <summary>
    /// Get paginated list of published courses (catalog). 
    /// Students browse for themselves. Guardians can specify StudentId to browse for a specific child.
    /// When student profile (DomainId, CurriculumId, LevelId, GradeId) is set, results are filtered to compatible courses; query params override.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Courses?PageNumber=1&amp;PageSize=10&amp;StudentId=5&amp;DomainId=1&amp;SubjectId=3
    /// 
    /// - **StudentId** (optional): For guardians browsing on behalf of a child
    /// - **DomainId, CurriculumId, LevelId, GradeId** (optional): Override student's profile filters
    /// - **SubjectId, TeachingModeId** (optional): Additional filters
    /// </remarks>
    [HttpGet(Router.StudentCourses)]
    [ProducesResponseType(typeof(List<CourseCatalogIndexItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedCourses([FromQuery] GetPublishedCoursesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get 4 recommended published courses for a specific student (domain-based).
    /// Requester must own the student (self) or be the student's guardian.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Courses/Recommended?StudentId=5
    /// </remarks>
    [HttpGet(Router.StudentRecommendedCourses)]
    [ProducesResponseType(typeof(List<CourseCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendedCourses([FromQuery] GetRecommendedCoursesQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get a published course by ID.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Courses/{id}
    ///
    /// Returns the full course detail including `sessions[]` (ordered by `sessionNumber` ascending). For flexible courses `sessions` is empty and `sessionsCount` is null.
    /// </remarks>
    [HttpGet(Router.StudentCourseById)]
    [ProducesResponseType(typeof(CourseCatalogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedCourseById(int id)
    {
        var query = new GetPublishedCourseByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get list of my children (for guardians to select when browsing courses).
    /// </summary>
    /// <remarks>GET Api/V1/Student/MyChildren</remarks>
    [HttpGet(Router.StudentMyChildren)]
    [Authorize(Roles = Roles.Guardian)]
    [ProducesResponseType(typeof(List<ChildStudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyChildren()
    {
        return NewResult(await Mediator.Send(new GetMyChildrenQuery()));
    }

    /// <summary>
    /// Request enrollment in a course (self, children, and/or invited students).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/EnrollmentRequests
    ///
    /// Rules:
    /// - `data.courseId` is required.
    /// - `data.studentIds` — students owned by the caller (self and/or children) that are enrolled immediately on approval.
    /// - `data.invitedStudentIds` — students that will receive an invitation and must accept before being added to the group.
    /// - `data.selectedAvailabilityIds` — teacher availability slots chosen for this request.
    /// - `data.proposedSessions` — only required for flexible courses. For fixed courses the session outline is taken from the course.
    ///   `sessionNumber` here is 1-based and must be unique within the array.
    ///
    /// Sample request body (fixed course, single student):
    /// <code>
    /// {
    ///   "data": {
    ///     "courseId": 1,
    ///     "studentIds": [ 42 ],
    ///     "invitedStudentIds": [],
    ///     "selectedAvailabilityIds": [ 10, 11 ],
    ///     "notes": "Prefers evening sessions.",
    ///     "proposedSessions": []
    ///   }
    /// }
    /// </code>
    ///
    /// Sample request body (flexible course, group with invites):
    /// <code>
    /// {
    ///   "data": {
    ///     "courseId": 7,
    ///     "studentIds": [ 42 ],
    ///     "invitedStudentIds": [ 55, 61 ],
    ///     "selectedAvailabilityIds": [ 21 ],
    ///     "notes": null,
    ///     "proposedSessions": [
    ///       { "sessionNumber": 1, "durationMinutes": 60, "title": "Kickoff",    "notes": null },
    ///       { "sessionNumber": 2, "durationMinutes": 60, "title": "Practice",   "notes": null },
    ///       { "sessionNumber": 3, "durationMinutes": 90, "title": "Assessment", "notes": "Final quiz." }
    ///     ]
    ///   }
    /// }
    /// </code>
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "data": {
    ///     "id": 123,
    ///     "courseId": 1,
    ///     "courseTitle": "Mathematics - Grade 10",
    ///     "status": "Pending",
    ///     "totalMinutes": 540,
    ///     "estimatedTotalPrice": 450.00,
    ///     "selectedAvailabilityIds": [ 10, 11 ],
    ///     "groupMembers": [
    ///       { "studentId": 42, "memberType": "Owner",  "confirmationStatus": "Confirmed", "confirmedAt": "2026-04-18T10:00:00Z" },
    ///       { "studentId": 55, "memberType": "Invited", "confirmationStatus": "Pending",  "confirmedAt": null }
    ///     ],
    ///     "proposedSessions": []
    ///   },
    ///   "succeeded": true
    /// }
    /// </code>
    /// </remarks>
    [HttpPost(Router.StudentEnrollmentRequests)]
    [ProducesResponseType(typeof(EnrollmentRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestEnrollment([FromBody] RequestCourseEnrollmentCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get my enrollment requests (paginated).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/EnrollmentRequests?PageNumber=1&amp;PageSize=10
    ///
    /// `data` is a flat list of <see cref="EnrollmentRequestListItemDto"/>. Pagination metadata
    /// (`totalCount`, `pageNumber`, `pageSize`, `totalPages`, `hasPreviousPage`, `hasNextPage`) is returned in the top-level `meta` field.
    /// </remarks>
    [HttpGet(Router.StudentEnrollmentRequests)]
    [ProducesResponseType(typeof(List<EnrollmentRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollmentRequests([FromQuery] GetMyEnrollmentRequestsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get my enrollment request by ID.
    /// </summary>
    /// <remarks>GET Api/V1/Student/EnrollmentRequests/{id}</remarks>
    [HttpGet(Router.StudentEnrollmentRequestById)]
    [ProducesResponseType(typeof(EnrollmentRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEnrollmentRequestById(int id)
    {
        var query = new GetMyEnrollmentRequestByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get my enrollments (paginated).
    /// </summary>
    /// <remarks>GET Api/V1/Student/Enrollments</remarks>
    [HttpGet(Router.StudentEnrollments)]
    [ProducesResponseType(typeof(List<EnrollmentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollments([FromQuery] GetMyEnrollmentsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get my enrollment by ID.
    /// </summary>
    /// <remarks>GET Api/V1/Student/Enrollments/{id}</remarks>
    [HttpGet(Router.StudentEnrollmentById)]
    [ProducesResponseType(typeof(EnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEnrollmentById(int id)
    {
        var query = new GetMyEnrollmentByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get pending invitations for the user's students (self + children).
    /// </summary>
    /// <remarks>GET Api/V1/Student/Invitations</remarks>
    [HttpGet(Router.StudentInvitations)]
    [ProducesResponseType(typeof(List<StudentInvitationListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInvitations([FromQuery] GetMyInvitationsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Search students by name or email for group enrollment.
    /// </summary>
    /// <remarks>GET Api/V1/Student/Students/Search?SearchTerm=ahmed&amp;MaxResults=20</remarks>
    [HttpGet(Router.StudentSearchForGroup)]
    [ProducesResponseType(typeof(List<StudentSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStudentsForGroup([FromQuery] SearchStudentsForGroupQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Search students by name or email (partial match, paginated). Returns guardian info if linked.
    /// </summary>
    /// <remarks>GET Api/V1/Student/Search?searchTerm=ahmed&amp;pageNumber=1&amp;pageSize=10</remarks>
    [HttpGet(Router.StudentSearch)]
    [ProducesResponseType(typeof(List<StudentByEmailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStudents([FromQuery] SearchStudentsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Teacher availability for a date range — calendar-style view with per-date slot status.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Teachers/{teacherId}/Availability?fromDate=2026-04-28&amp;toDate=2026-05-28
    ///
    /// Defaults: if `fromDate` is omitted it falls back to today; if `toDate` is omitted it
    /// falls back to fromDate + 30 days. The server caps the range at fromDate + 90 days.
    ///
    /// Each returned day lists the teacher's weekly slots that fall on that DayOfWeek, with
    /// their per-date status:
    /// - **Free** — bookable
    /// - **Booked** — a CourseSchedule already exists on this (date, slot)
    /// - **Blocked** — the teacher has a Blocked AvailabilityException on this (date, timeslot)
    ///
    /// Days where the teacher has no recurring availability are omitted.
    /// </remarks>
    [HttpGet(Router.StudentTeacherAvailability)]
    [ProducesResponseType(typeof(TeacherAvailabilityByWeekdayRangeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherAvailabilityByRange(
        int teacherId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var query = new GetTeacherAvailabilityByRangeQuery
        {
            TeacherId = teacherId,
            FromDate = fromDate,
            ToDate = toDate
        };
        return NewResult(await Mediator.Send(query));
    }
}
