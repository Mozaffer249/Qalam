using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminRevenueSummary;

public class GetAdminRevenueSummaryQuery : IRequest<Response<AdminRevenueSummaryDto>>
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
