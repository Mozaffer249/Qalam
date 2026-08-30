using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.MarkAdminPayoutBatchPaid;

public class MarkAdminPayoutBatchPaidCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
}
