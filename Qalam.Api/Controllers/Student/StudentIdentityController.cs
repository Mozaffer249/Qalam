using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Commands.SetStudentIdentity;
using Qalam.Core.Features.Student.Queries.GetStudentAllowedIdentityTypes;
using Qalam.Core.Features.Student.Queries.GetStudentIdentity;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;
namespace Qalam.Api.Controllers.Student;

[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentIdentityController : AppControllerBase
{
    [HttpPost(Router.StudentIdentity)]
    [ProducesResponseType(typeof(StudentIdentityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetIdentity([FromBody] SetStudentIdentityCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpGet(Router.StudentIdentity)]
    [ProducesResponseType(typeof(StudentIdentityResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIdentity()
        => NewResult(await Mediator.Send(new GetStudentIdentityQuery()));

    [HttpGet(Router.StudentIdentityAllowedTypes)]
    [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllowedTypes([FromQuery] int nationalityId)
        => NewResult(await Mediator.Send(new GetStudentAllowedIdentityTypesQuery { NationalityId = nationalityId }));

    [HttpPut(Router.StudentChildIdentity)]
    [ProducesResponseType(typeof(StudentIdentityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetChildIdentity(
        [FromRoute] int studentId,
        [FromBody] SetStudentIdentityDto identity)
        => NewResult(await Mediator.Send(new SetStudentIdentityCommand
        {
            Identity = identity,
            TargetStudentId = studentId
        }));
}
