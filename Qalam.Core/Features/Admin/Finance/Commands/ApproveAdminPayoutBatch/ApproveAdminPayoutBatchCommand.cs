using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.ApproveAdminPayoutBatch;

public class ApproveAdminPayoutBatchCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
}
