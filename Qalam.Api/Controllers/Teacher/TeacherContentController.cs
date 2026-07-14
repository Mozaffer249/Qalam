using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Content.Commands.CreateContentFolder;
using Qalam.Core.Features.Teacher.Content.Commands.CreateHomeworkTemplate;
using Qalam.Core.Features.Teacher.Content.Commands.DeleteContentFolder;
using Qalam.Core.Features.Teacher.Content.Commands.DeleteContentItem;
using Qalam.Core.Features.Teacher.Content.Commands.LinkCourseSessionContent;
using Qalam.Core.Features.Teacher.Content.Commands.LinkCourseSessionContentBulk;
using Qalam.Core.Features.Teacher.Content.Commands.LinkSessionContent;
using Qalam.Core.Features.Teacher.Content.Commands.LinkSessionContentBulk;
using Qalam.Core.Features.Teacher.Content.Commands.UnlinkCourseSessionContent;
using Qalam.Core.Features.Teacher.Content.Commands.UnlinkSessionContent;
using Qalam.Core.Features.Teacher.Content.Commands.UpdateContentFolder;
using Qalam.Core.Features.Teacher.Content.Commands.UpdateContentItem;
using Qalam.Core.Features.Teacher.Content.Commands.UploadContentItem;
using Qalam.Core.Features.Teacher.Content.Queries.GetContentItem;
using Qalam.Core.Features.Teacher.Content.Queries.ListContentFolders;
using Qalam.Core.Features.Teacher.Content.Queries.ListContentItems;
using Qalam.Core.Features.Teacher.Content.Queries.ListCourseSessionContent;
using Qalam.Core.Features.Teacher.Content.Queries.ListSessionContent;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherContentController : AppControllerBase
{
    [HttpGet(Router.TeacherContentFolders)]
    [ProducesResponseType(typeof(List<TeacherContentFolderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFolders([FromQuery] ListContentFoldersQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpPost(Router.TeacherContentFolders)]
    [ProducesResponseType(typeof(TeacherContentFolderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateFolder([FromBody] CreateTeacherContentFolderDto dto)
        => NewResult(await Mediator.Send(new CreateContentFolderCommand { Data = dto }));

    [HttpPut(Router.TeacherContentFolderById)]
    [ProducesResponseType(typeof(TeacherContentFolderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFolder(int id, [FromBody] UpdateTeacherContentFolderDto dto)
        => NewResult(await Mediator.Send(new UpdateContentFolderCommand { Id = id, Data = dto }));

    [HttpDelete(Router.TeacherContentFolderById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFolder(int id)
        => NewResult(await Mediator.Send(new DeleteContentFolderCommand { Id = id }));

    [HttpGet(Router.TeacherContentItems)]
    [ProducesResponseType(typeof(List<TeacherContentItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListItems([FromQuery] ListContentItemsQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet(Router.TeacherContentItemById)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItem(int id)
        => NewResult(await Mediator.Send(new GetContentItemQuery { Id = id }));

    [HttpPost(Router.TeacherContentItemUpload)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadItem(
        IFormFile file,
        [FromForm] int? folderId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] string? tags)
        => NewResult(await Mediator.Send(new UploadContentItemCommand
        {
            File = file,
            FolderId = folderId,
            Title = title,
            Description = description,
            Tags = tags,
        }));

    [HttpPost(Router.TeacherContentItemHomework)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateHomework([FromBody] CreateHomeworkTemplateDto dto)
        => NewResult(await Mediator.Send(new CreateHomeworkTemplateCommand { Data = dto }));

    [HttpPut(Router.TeacherContentItemById)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateTeacherContentItemDto dto)
        => NewResult(await Mediator.Send(new UpdateContentItemCommand { Id = id, Data = dto }));

    [HttpDelete(Router.TeacherContentItemById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItem(int id)
        => NewResult(await Mediator.Send(new DeleteContentItemCommand { Id = id }));

    [HttpGet(Router.TeacherMySessionContent)]
    [ProducesResponseType(typeof(List<TeacherSessionContentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessionContent(int id)
        => NewResult(await Mediator.Send(new ListSessionContentQuery { ScheduleId = id }));

    [HttpPost(Router.TeacherMySessionContent)]
    [ProducesResponseType(typeof(TeacherSessionContentLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkSessionContent(int id, [FromBody] LinkSessionContentDto dto)
        => NewResult(await Mediator.Send(new LinkSessionContentCommand
        {
            ScheduleId = id,
            ContentItemId = dto.ContentItemId,
        }));

    [HttpPost($"{Router.TeacherMySessionContent}/bulk")]
    [ProducesResponseType(typeof(List<TeacherSessionContentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkSessionContentBulk(int id, [FromBody] LinkSessionContentBulkDto dto)
        => NewResult(await Mediator.Send(new LinkSessionContentBulkCommand
        {
            ScheduleId = id,
            Data = dto,
        }));

    [HttpDelete(Router.TeacherMySessionContentByLinkId)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkSessionContent(int id, int linkId)
        => NewResult(await Mediator.Send(new UnlinkSessionContentCommand
        {
            ScheduleId = id,
            LinkId = linkId,
        }));

    [HttpGet(Router.TeacherCourseSessionContent)]
    [ProducesResponseType(typeof(List<TeacherSessionContentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCourseSessionContent(int courseId, int sessionId)
        => NewResult(await Mediator.Send(new ListCourseSessionContentQuery
        {
            CourseId = courseId,
            SessionId = sessionId,
        }));

    [HttpPost(Router.TeacherCourseSessionContent)]
    [ProducesResponseType(typeof(TeacherSessionContentLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkCourseSessionContent(
        int courseId, int sessionId, [FromBody] LinkSessionContentDto dto)
        => NewResult(await Mediator.Send(new LinkCourseSessionContentCommand
        {
            CourseId = courseId,
            SessionId = sessionId,
            ContentItemId = dto.ContentItemId,
        }));

    [HttpPost($"{Router.TeacherCourseSessionContent}/bulk")]
    [ProducesResponseType(typeof(List<TeacherSessionContentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkCourseSessionContentBulk(
        int courseId, int sessionId, [FromBody] LinkSessionContentBulkDto dto)
        => NewResult(await Mediator.Send(new LinkCourseSessionContentBulkCommand
        {
            CourseId = courseId,
            SessionId = sessionId,
            Data = dto,
        }));

    [HttpDelete(Router.TeacherCourseSessionContentByLinkId)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkCourseSessionContent(int courseId, int sessionId, int linkId)
        => NewResult(await Mediator.Send(new UnlinkCourseSessionContentCommand
        {
            CourseId = courseId,
            SessionId = sessionId,
            LinkId = linkId,
        }));
}
