using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListSessionContent;

public class ListSessionContentQuery : IRequest<Response<List<TeacherSessionContentLinkDto>>>, IAuthenticatedRequest
{
    public int ScheduleId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
