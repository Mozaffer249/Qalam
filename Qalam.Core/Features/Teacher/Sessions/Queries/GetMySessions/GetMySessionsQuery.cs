using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessions;

public class GetMySessionsQuery : IRequest<Response<List<TeacherMySessionListItemDto>>>, IAuthenticatedRequest
{
    public string Filter { get; set; } = "upcoming";
    public int PageSize { get; set; } = 10;

    [BindNever]
    public int UserId { get; set; }
}
