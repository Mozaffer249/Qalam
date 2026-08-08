using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.CloseContactMessage;

public class CloseContactMessageCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int Id { get; set; }
    public string? AdminNote { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
