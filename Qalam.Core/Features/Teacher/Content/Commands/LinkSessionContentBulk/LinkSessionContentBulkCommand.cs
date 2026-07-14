using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.LinkSessionContentBulk;

public class LinkSessionContentBulkCommand : IRequest<Response<List<TeacherSessionContentLinkDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public LinkSessionContentBulkDto Data { get; set; } = null!;
}
