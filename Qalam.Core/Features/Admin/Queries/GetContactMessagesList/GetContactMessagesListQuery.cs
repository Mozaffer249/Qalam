using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetContactMessagesList;

public class GetContactMessagesListQuery : IRequest<Response<List<AdminContactMessageDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
}
