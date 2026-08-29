using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Sessions.Commands.ResolveSessionComplaint;

public class ResolveSessionComplaintCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public int ComplaintId { get; set; }
    public ResolveSessionComplaintRequest Body { get; set; } = null!;
}
