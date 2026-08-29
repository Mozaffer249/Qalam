using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Sessions.Commands.CancelMySession;
using Qalam.Core.Features.Teacher.Sessions.Commands.CompleteMySession;
using Qalam.Core.Features.Teacher.Sessions.Commands.GetMySessionLiveToken;
using Qalam.Core.Features.Teacher.Sessions.Commands.JoinMySession;
using Qalam.Core.Features.Teacher.Sessions.Commands.LeaveMySession;
using Qalam.Core.Features.Teacher.Sessions.Commands.RescheduleMySession;
using Qalam.Core.Features.Teacher.Sessions.Commands.RespondToSessionComplaint;
using Qalam.Core.Features.Teacher.Sessions.Commands.SetSessionAttendance;
using Qalam.Core.Features.Teacher.Sessions.Commands.SetSessionTeacherNote;
using Qalam.Core.Features.Teacher.Sessions.Commands.StartMySession;
using Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessionById;
using Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessions;
using Qalam.Core.Features.Teacher.Sessions.Queries.GetSessionReviews;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Live;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherMySessions)]
public class TeacherMySessionsController : AppControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<TeacherMySessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] GetMySessionsQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeacherMySessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
        => NewResult(await Mediator.Send(new GetMySessionByIdQuery { Id = id }));

    [HttpPost("{id:int}/Join")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join(int id)
        => NewResult(await Mediator.Send(new JoinMySessionCommand { Id = id }));

    [HttpPost("{id:int}/Leave")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Leave(int id)
        => NewResult(await Mediator.Send(new LeaveMySessionCommand { Id = id }));

    [HttpPost("{id:int}/LiveToken")]
    [ProducesResponseType(typeof(LiveSessionAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> LiveToken(int id)
        => NewResult(await Mediator.Send(new GetMySessionLiveTokenCommand { Id = id }));

    [HttpPost("{id:int}/Start")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(int id)
        => NewResult(await Mediator.Send(new StartMySessionCommand { Id = id }));

    [HttpPost("{id:int}/Complete")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(int id)
        => NewResult(await Mediator.Send(new CompleteMySessionCommand { Id = id }));

    [HttpPost("{id:int}/Cancel")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id)
        => NewResult(await Mediator.Send(new CancelMySessionCommand { Id = id }));

    [HttpPost("{id:int}/Reschedule")]
    [ProducesResponseType(typeof(RescheduleMySessionResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleMySessionRequestDto body)
        => NewResult(await Mediator.Send(new RescheduleMySessionCommand
        {
            Id = id,
            NewDate = body.NewDate,
            TeacherAvailabilityId = body.TeacherAvailabilityId,
        }));

    [HttpPost("{id:int}/Attendance")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAttendance(int id, [FromBody] SetSessionAttendanceRequestDto body)
        => NewResult(await Mediator.Send(new SetSessionAttendanceCommand
        {
            Id = id,
            Items = body.Items ?? new(),
        }));

    [HttpPut("{id:int}/Notes")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetNotes(int id, [FromBody] SetSessionTeacherNoteRequestDto body)
        => NewResult(await Mediator.Send(new SetSessionTeacherNoteCommand
        {
            Id = id,
            Note = body.Note ?? string.Empty,
        }));

    [HttpGet("{id:int}/Reviews")]
    [ProducesResponseType(typeof(List<SessionReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews(int id)
        => NewResult(await Mediator.Send(new GetSessionReviewsQuery { Id = id }));

    [HttpGet("{id:int}/Homework")]
    [ProducesResponseType(typeof(List<TeacherSessionHomeworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListHomework(int id, [FromServices] ITeacherContentService contentService, [FromServices] ITeacherRepository teacherRepository, CancellationToken ct)
    {
        var teacher = await teacherRepository.GetByUserIdAsync(GetUserId());
        if (teacher == null) return NotFound();
        var homework = await contentService.ListSessionHomeworkAsync(teacher.Id, id, ct);
        return Ok(homework);
    }

    [HttpPost("{id:int}/Homework")]
    [ProducesResponseType(typeof(TeacherSessionHomeworkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateHomework(int id, [FromBody] CreateSessionHomeworkDto dto, [FromServices] ITeacherContentService contentService, [FromServices] ITeacherRepository teacherRepository, CancellationToken ct)
    {
        var teacher = await teacherRepository.GetByUserIdAsync(GetUserId());
        if (teacher == null) return NotFound();
        var homework = await contentService.CreateSessionHomeworkAsync(teacher.Id, id, dto, ct);
        if (homework == null) return BadRequest("Cannot create homework assignment.");
        return Ok(homework);
    }

    [HttpPut("{id:int}/Homework/{assignmentId:int}")]
    [ProducesResponseType(typeof(TeacherSessionHomeworkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateHomework(int id, int assignmentId, [FromBody] UpdateSessionHomeworkDto dto, [FromServices] ITeacherContentService contentService, [FromServices] ITeacherRepository teacherRepository, CancellationToken ct)
    {
        var teacher = await teacherRepository.GetByUserIdAsync(GetUserId());
        if (teacher == null) return NotFound();
        var homework = await contentService.UpdateSessionHomeworkAsync(teacher.Id, id, assignmentId, dto, ct);
        if (homework == null) return NotFound();
        return Ok(homework);
    }

    [HttpDelete("{id:int}/Homework/{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteHomework(int id, int assignmentId, [FromServices] ITeacherContentService contentService, [FromServices] ITeacherRepository teacherRepository, CancellationToken ct)
    {
        var teacher = await teacherRepository.GetByUserIdAsync(GetUserId());
        if (teacher == null) return NotFound();
        var ok = await contentService.DeleteSessionHomeworkAsync(teacher.Id, id, assignmentId, ct);
        if (!ok) return NotFound();
        return Ok();
    }

    [HttpPost("{id:int}/Complaints/{complaintId:int}/Respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RespondToComplaint(
        int id,
        int complaintId,
        [FromBody] TeacherRespondComplaintRequest body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new RespondToSessionComplaintCommand
        {
            ScheduleId = id,
            ComplaintId = complaintId,
            Response = body.Response,
        }, cancellationToken));

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}
