using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminPayoutBatchById;

public class GetAdminPayoutBatchByIdQuery : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
}
