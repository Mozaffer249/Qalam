using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Student.Sessions.Queries.ListStudentSessions;

public class ListStudentSessionsQuery : IRequest<Response<List<StudentSessionListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
