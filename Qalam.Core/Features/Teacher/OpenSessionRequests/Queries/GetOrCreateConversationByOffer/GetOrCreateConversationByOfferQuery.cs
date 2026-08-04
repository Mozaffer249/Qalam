using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetOrCreateConversationByOffer;

public class GetOrCreateConversationByOfferQuery : IRequest<Response<OfferConversationDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int OfferId { get; set; }
}
