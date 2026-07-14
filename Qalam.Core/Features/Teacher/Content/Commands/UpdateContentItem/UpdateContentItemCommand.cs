using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.UpdateContentItem;

public class UpdateContentItemCommand : IRequest<Response<TeacherContentItemDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
    public UpdateTeacherContentItemDto Data { get; set; } = null!;
}
