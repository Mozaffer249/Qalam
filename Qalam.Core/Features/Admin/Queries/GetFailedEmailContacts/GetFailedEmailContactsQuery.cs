using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetFailedEmailContacts;

public class GetFailedEmailContactsQuery : IRequest<Response<List<FailedEmailContactDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}
