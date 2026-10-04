using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.RejectAdminRefund;

public class RejectAdminRefundCommand : IRequest<Response<AdminRefundDetailDto>>
{
    public int Id { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? Reason { get; set; }
}
