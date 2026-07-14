using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.CreateContentFolder;

public class CreateContentFolderCommand : IRequest<Response<TeacherContentFolderDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreateTeacherContentFolderDto Data { get; set; } = null!;
}
