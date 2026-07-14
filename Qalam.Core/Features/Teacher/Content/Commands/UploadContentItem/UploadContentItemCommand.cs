using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.UploadContentItem;

public class UploadContentItemCommand : IRequest<Response<TeacherContentItemDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public IFormFile File { get; set; } = null!;
    public int? FolderId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}
