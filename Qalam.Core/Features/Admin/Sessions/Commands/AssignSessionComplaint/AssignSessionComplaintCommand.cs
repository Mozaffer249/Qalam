using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.AssignSessionComplaint;

public class AssignSessionComplaintCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public int ComplaintId { get; set; }
    public int AssignedToUserId { get; set; }
}
