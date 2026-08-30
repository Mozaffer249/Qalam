using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminRefundById;

public class GetAdminRefundByIdQuery : IRequest<Response<AdminRefundDetailDto>>
{
    public int Id { get; set; }
}
