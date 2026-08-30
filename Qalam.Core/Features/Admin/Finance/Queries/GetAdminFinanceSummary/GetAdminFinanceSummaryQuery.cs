using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminFinanceSummary;

public class GetAdminFinanceSummaryQuery : IRequest<Response<AdminFinanceSummaryDto>>
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
