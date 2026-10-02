using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Policy;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherCancellationPolicyController : AppControllerBase
{
    [HttpGet(Router.TeacherCancellationPolicy)]
    [ProducesResponseType(typeof(PolicySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromServices] IPolicyResolver resolver,
        CancellationToken cancellationToken)
    {
        var policy = await resolver.GetCurrentAsync(cancellationToken);
        return Ok(new { data = PolicySummaryBuilder.ForTeacher(policy), succeeded = true });
    }
}
