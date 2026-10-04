using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminDashboardOverview;

public class GetAdminDashboardOverviewQuery : IRequest<Response<AdminDashboardOverviewDto>>
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
