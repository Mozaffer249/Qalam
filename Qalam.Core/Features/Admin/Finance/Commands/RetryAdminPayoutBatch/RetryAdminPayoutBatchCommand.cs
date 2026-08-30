using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.RetryAdminPayoutBatch;

public class RetryAdminPayoutBatchCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
    public int? ProcessedByUserId { get; set; }
}
