using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessionById;
using Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
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

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}
