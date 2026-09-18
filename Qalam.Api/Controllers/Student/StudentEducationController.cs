using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Queries.GetStudentActiveDomains;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Student education catalog helpers (domains for Home / Domains / filter wizard).
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentEducationController : AppControllerBase
{
    /// <summary>
    /// List active education domains for students (enhanced slim payload with descriptions).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Student/Domains
    ///
    /// Returns only <c>isActive == true</c> domains, ordered for student display.
    /// Use <c>code</c> (never hardcoded ids) when applying Discover filters.
    /// </remarks>
    [HttpGet(Router.StudentDomains)]
    [ProducesResponseType(typeof(List<StudentEducationDomainDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveDomains()
    {
        return NewResult(await Mediator.Send(new GetStudentActiveDomainsQuery()));
    }
}
