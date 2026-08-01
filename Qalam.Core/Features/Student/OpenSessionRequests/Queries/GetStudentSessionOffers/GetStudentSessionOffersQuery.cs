using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetStudentSessionOffers;

public class GetStudentSessionOffersQuery
    : IRequest<Response<List<StudentOfferListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    [BindNever]
    public int RequestId { get; set; }
}
