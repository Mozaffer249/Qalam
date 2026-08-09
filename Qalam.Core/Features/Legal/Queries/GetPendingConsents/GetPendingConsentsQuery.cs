using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Legal.Queries.GetPendingConsents;

public class GetPendingConsentsQuery : IRequest<Response<List<PendingConsentDocumentDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
