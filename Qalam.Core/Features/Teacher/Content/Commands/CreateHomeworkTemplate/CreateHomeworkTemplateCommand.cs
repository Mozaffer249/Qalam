using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.CreateHomeworkTemplate;

public class CreateHomeworkTemplateCommand : IRequest<Response<TeacherContentItemDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreateHomeworkTemplateDto Data { get; set; } = null!;
}
