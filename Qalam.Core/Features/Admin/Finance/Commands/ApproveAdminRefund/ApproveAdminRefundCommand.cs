using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.ApproveAdminRefund;

public class ApproveAdminRefundCommand : IRequest<Response<AdminRefundDetailDto>>
{
    public int Id { get; set; }
    public int? ApprovedByUserId { get; set; }
}
