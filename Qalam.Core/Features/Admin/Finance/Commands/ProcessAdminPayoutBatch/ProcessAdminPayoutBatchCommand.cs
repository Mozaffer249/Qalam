using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.ProcessAdminPayoutBatch;

public class ProcessAdminPayoutBatchCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
    public int? ProcessedByUserId { get; set; }
}
