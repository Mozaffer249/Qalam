using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Legal.Commands.AcceptLegalConsents;

public class AcceptLegalConsentsCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public AcceptLegalConsentsDto Data { get; set; } = new();

    [BindNever]
    public int UserId { get; set; }
}
