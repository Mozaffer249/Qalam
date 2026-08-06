using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetEmailSuppressions;

public class GetEmailSuppressionsQuery : IRequest<Response<List<EmailSuppressionListItemDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}
