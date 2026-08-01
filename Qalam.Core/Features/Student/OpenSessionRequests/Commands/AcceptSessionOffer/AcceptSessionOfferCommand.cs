using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.AcceptSessionOffer;

public class AcceptSessionOfferCommand
    : IRequest<Response<AcceptSessionOfferResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    [BindNever]
    public int OfferId { get; set; }
}
