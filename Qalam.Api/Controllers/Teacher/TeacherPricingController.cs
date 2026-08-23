using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;
using Qalam.Core.Features.Teacher.Pricing.Queries.GetMyDomainPricings;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route("Api/V1/Teacher/Pricing")]
public class TeacherPricingController : AppControllerBase
{
    /// <summary>
    /// Preview student hourly rate for a course (engine applies custom rates and reflect flags).
    /// </summary>
    [HttpGet("course-hourly-rate")]
    [ProducesResponseType(typeof(CourseHourlyRatePreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseHourlyRatePreview([FromQuery] GetCourseHourlyRatePreviewQuery query)
        => NewResult(await Mediator.Send(query));

    /// <summary>
    /// Read-only list of this teacher's per-domain pricing.
    /// </summary>
    [HttpGet("my-domain-pricings")]
    [ProducesResponseType(typeof(List<TeacherMyDomainPricingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDomainPricings()
        => NewResult(await Mediator.Send(new GetMyDomainPricingsQuery()));
}
