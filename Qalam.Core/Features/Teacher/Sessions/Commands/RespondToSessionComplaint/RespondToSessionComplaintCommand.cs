using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.RespondToSessionComplaint;

public class RespondToSessionComplaintCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public int ComplaintId { get; set; }
    public string Response { get; set; } = string.Empty;
}
