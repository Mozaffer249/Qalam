using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Commands.DeleteTeacherArea;

public class DeleteTeacherAreaCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
