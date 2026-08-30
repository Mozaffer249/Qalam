using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.CreateAdminPayoutBatch;

public class CreateAdminPayoutBatchCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public CreatePayoutBatchDto? Body { get; set; }
    public int? CreatedByUserId { get; set; }
}
