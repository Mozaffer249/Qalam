using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Sessions.Commands.IssueAdminSessionRefund;

public class IssueAdminSessionRefundCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public AdminSessionRefundRequest Body { get; set; } = null!;
}
