using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessionById;

public class GetMySessionByIdQuery : IRequest<Response<TeacherMySessionDetailDto>>, IAuthenticatedRequest
{
    public int Id { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
