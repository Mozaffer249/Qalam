using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminRevenueById;

public class GetAdminRevenueByIdQuery : IRequest<Response<AdminRevenueDetailDto>>
{
    public int Id { get; set; }
}
