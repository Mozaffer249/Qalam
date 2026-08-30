using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.IssueAdminRefund;

public class IssueAdminRefundCommand : IRequest<Response<AdminRefundDetailDto>>
{
    public IssueAdminRefundDto Body { get; set; } = new();
    public int? InitiatedByUserId { get; set; }
}
