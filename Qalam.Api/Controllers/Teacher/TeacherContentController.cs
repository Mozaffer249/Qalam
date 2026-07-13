using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherContentController : AppControllerBase
{
    private readonly ITeacherContentService _contentService;
    private readonly ITeacherRepository _teacherRepository;

    public TeacherContentController(ITeacherContentService contentService, ITeacherRepository teacherRepository)
    {
        _contentService = contentService;
        _teacherRepository = teacherRepository;
    }

    [HttpGet(Router.TeacherContentFolders)]
    [ProducesResponseType(typeof(List<TeacherContentFolderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFolders([FromQuery] int? parentId, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var folders = await _contentService.ListFoldersAsync(teacherId.Value, parentId, ct);
        return NewResult(Success(folders));
    }

    [HttpPost(Router.TeacherContentFolders)]
    [ProducesResponseType(typeof(TeacherContentFolderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateFolder([FromBody] CreateTeacherContentFolderDto dto, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var folder = await _contentService.CreateFolderAsync(teacherId.Value, dto, ct);
        if (folder == null)
            return NewResult(BadRequest<TeacherContentFolderDto>("Invalid folder data or duplicate name."));
        return NewResult(Success(folder));
    }

    [HttpPut(Router.TeacherContentFolderById)]
    [ProducesResponseType(typeof(TeacherContentFolderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFolder(int id, [FromBody] UpdateTeacherContentFolderDto dto, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var folder = await _contentService.UpdateFolderAsync(teacherId.Value, id, dto, ct);
        if (folder == null) return NewResult(NotFound<TeacherContentFolderDto>("Folder not found."));
        return NewResult(Success(folder));
    }

    [HttpDelete(Router.TeacherContentFolderById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFolder(int id, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var ok = await _contentService.DeleteFolderAsync(teacherId.Value, id, ct);
        if (!ok) return NewResult(BadRequest<string>("Folder not found or not empty."));
        return NewResult(Success("Deleted"));
    }

    [HttpGet(Router.TeacherContentItems)]
    [ProducesResponseType(typeof(List<TeacherContentItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListItems(
        [FromQuery] int? folderId,
        [FromQuery] TeacherContentItemKind? kind,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var items = await _contentService.ListItemsAsync(teacherId.Value, folderId, kind, search, pageNumber, pageSize, ct);
        return NewResult(Success(items));
    }

    [HttpGet(Router.TeacherContentItemById)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItem(int id, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var item = await _contentService.GetItemAsync(teacherId.Value, id, ct);
        if (item == null) return NewResult(NotFound<TeacherContentItemDto>("Item not found."));
        return NewResult(Success(item));
    }

    [HttpPost(Router.TeacherContentItemUpload)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadItem(
        IFormFile file,
        [FromForm] int? folderId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] string? tags,
        CancellationToken ct = default)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var tagList = ParseTags(tags);
        var item = await _contentService.UploadFileAsync(teacherId.Value, file, folderId, title, description, tagList, ct);
        if (item == null) return NewResult(BadRequest<TeacherContentItemDto>("Invalid file or folder."));
        return NewResult(Success(item));
    }

    [HttpPost(Router.TeacherContentItemHomework)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateHomework([FromBody] CreateHomeworkTemplateDto dto, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var item = await _contentService.CreateHomeworkAsync(teacherId.Value, dto, ct);
        if (item == null) return NewResult(BadRequest<TeacherContentItemDto>("Invalid homework data."));
        return NewResult(Success(item));
    }

    [HttpPut(Router.TeacherContentItemById)]
    [ProducesResponseType(typeof(TeacherContentItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateTeacherContentItemDto dto, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var item = await _contentService.UpdateItemAsync(teacherId.Value, id, dto, ct);
        if (item == null) return NewResult(NotFound<TeacherContentItemDto>("Item not found."));
        return NewResult(Success(item));
    }

    [HttpDelete(Router.TeacherContentItemById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var ok = await _contentService.DeleteItemAsync(teacherId.Value, id, ct);
        if (!ok) return NewResult(BadRequest<string>("Item not found or linked to sessions."));
        return NewResult(Success("Deleted"));
    }

    [HttpGet(Router.TeacherMySessionContent)]
    [ProducesResponseType(typeof(List<TeacherSessionContentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessionContent(int id, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var links = await _contentService.ListSessionContentAsync(teacherId.Value, id, ct);
        return NewResult(Success(links));
    }

    [HttpPost(Router.TeacherMySessionContent)]
    [ProducesResponseType(typeof(TeacherSessionContentLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkSessionContent(int id, [FromBody] LinkSessionContentDto dto, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var link = await _contentService.LinkSessionContentAsync(teacherId.Value, id, dto.ContentItemId, ct);
        if (link == null) return NewResult(BadRequest<TeacherSessionContentLinkDto>("Cannot link content."));
        return NewResult(Success(link));
    }

    [HttpPost($"{Router.TeacherMySessionContent}/bulk")]
    [ProducesResponseType(typeof(List<TeacherSessionContentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkSessionContentBulk(int id, [FromBody] LinkSessionContentBulkDto dto, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var links = await _contentService.LinkSessionContentBulkAsync(teacherId.Value, id, dto, ct);
        return NewResult(Success(links));
    }

    [HttpDelete(Router.TeacherMySessionContentByLinkId)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkSessionContent(int id, int linkId, CancellationToken ct)
    {
        var teacherId = await ResolveTeacherIdAsync();
        if (teacherId == null) return NotFoundTeacher();
        var ok = await _contentService.UnlinkSessionContentAsync(teacherId.Value, id, linkId, ct);
        if (!ok) return NewResult(NotFound<string>("Link not found."));
        return NewResult(Success("Unlinked"));
    }

    private async Task<int?> ResolveTeacherIdAsync()
    {
        var userId = GetUserId();
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        return teacher?.Id;
    }

    private IActionResult NotFoundTeacher() =>
        NewResult(new Response<string>("Teacher profile not found") { StatusCode = HttpStatusCode.NotFound });

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }

    private static List<string>? ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;
        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static Response<T> Success<T>(T entity) => new(entity) { StatusCode = HttpStatusCode.OK };
    private static Response<T> BadRequest<T>(string message) => new(message) { StatusCode = HttpStatusCode.BadRequest };
    private static Response<T> NotFound<T>(string message) => new(message) { StatusCode = HttpStatusCode.NotFound };
}
