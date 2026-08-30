using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.CancelAdminPayoutBatch;

public class CancelAdminPayoutBatchCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
    public string? Reason { get; set; }
}
