using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Profile.Commands.DeleteSampleLessonMedia;
using Qalam.Core.Features.Teacher.Profile.Commands.UpdateTeacherBio;
using Qalam.Core.Features.Teacher.Profile.Commands.UploadSampleLessonMedia;
using Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route("Api/V1/Teacher/Profile")]
public class TeacherProfileController : AppControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(TeacherMyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
        => NewResult(await Mediator.Send(new GetMyTeacherProfileQuery()));

    [HttpPut("bio")]
    [ProducesResponseType(typeof(TeacherMyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBio([FromBody] UpdateTeacherBioRequest body)
        => NewResult(await Mediator.Send(new UpdateTeacherBioCommand { Bio = body.Bio }));

    [HttpPost("sample-lesson-media")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SampleLessonMediaUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSampleLessonMedia(IFormFile file)
        => NewResult(await Mediator.Send(new UploadSampleLessonMediaCommand { File = file }));

    [HttpDelete("sample-lesson-media")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSampleLessonMedia()
        => NewResult(await Mediator.Send(new DeleteSampleLessonMediaCommand()));
}
