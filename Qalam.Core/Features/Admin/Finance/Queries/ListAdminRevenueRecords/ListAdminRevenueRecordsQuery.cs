using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminRevenueRecords;

public class ListAdminRevenueRecordsQuery : IRequest<Response<PagedResult<AdminRevenueRecordDto>>>
{
    public AdminRevenueListFilter Filter { get; set; } = new();
}
