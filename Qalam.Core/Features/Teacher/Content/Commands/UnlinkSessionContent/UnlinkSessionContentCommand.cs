using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Content.Commands.UnlinkSessionContent;

public class UnlinkSessionContentCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public int LinkId { get; set; }
}
