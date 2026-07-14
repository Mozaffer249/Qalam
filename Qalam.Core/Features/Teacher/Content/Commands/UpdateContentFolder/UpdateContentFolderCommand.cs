using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.UpdateContentFolder;

public class UpdateContentFolderCommand : IRequest<Response<TeacherContentFolderDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
    public UpdateTeacherContentFolderDto Data { get; set; } = null!;
}
