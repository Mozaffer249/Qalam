using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Pricing.Queries.GetCourseHourlyRatePreview;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route("Api/V1/Teacher/Pricing")]
public class TeacherPricingController : AppControllerBase
{
    /// <summary>
    /// Preview the platform hourly rate for a course based on subject domain and session type.
    /// </summary>
    [HttpGet("course-hourly-rate")]
    [ProducesResponseType(typeof(CourseHourlyRatePreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseHourlyRatePreview([FromQuery] GetCourseHourlyRatePreviewQuery query)
        => NewResult(await Mediator.Send(query));
}
