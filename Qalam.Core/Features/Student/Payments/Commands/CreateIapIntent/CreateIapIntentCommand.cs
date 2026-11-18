using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Commands.CreateIapIntent;

public class CreateIapIntentCommand : IRequest<Response<IapIntentDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreateIapIntentRequestDto Data { get; set; } = null!;
}
