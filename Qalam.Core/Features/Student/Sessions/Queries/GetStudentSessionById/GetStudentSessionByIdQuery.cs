using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionById;

public class GetStudentSessionByIdQuery : IRequest<Response<StudentSessionDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
