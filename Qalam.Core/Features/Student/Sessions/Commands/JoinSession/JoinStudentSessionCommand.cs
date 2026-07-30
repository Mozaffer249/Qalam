using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Sessions.Commands.JoinSession;

public class JoinStudentSessionCommand : IRequest<Response<StudentSessionJoinDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
