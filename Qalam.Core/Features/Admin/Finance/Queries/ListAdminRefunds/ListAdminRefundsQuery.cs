using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminRefunds;

public class ListAdminRefundsQuery : IRequest<Response<PagedResult<AdminRefundListItemDto>>>
{
    public RefundStatus? Status { get; set; }
    public int? EnrollmentId { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public string? Search { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
