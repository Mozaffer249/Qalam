using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.RejectSessionOffer;

public class RejectSessionOfferCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    [BindNever]
    public int OfferId { get; set; }

    public RejectSessionOfferDto Data { get; set; } = new();
}

public class RejectSessionOfferDto
{
    public string? Reason { get; set; }
}
