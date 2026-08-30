using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminPendingEarnings;

public class ListAdminPendingEarningsQuery : IRequest<Response<PagedResult<AdminPendingEarningDto>>>
{
    public int? TeacherId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
