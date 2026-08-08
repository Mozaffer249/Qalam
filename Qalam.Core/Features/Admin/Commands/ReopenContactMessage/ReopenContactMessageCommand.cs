using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.ReopenContactMessage;

public class ReopenContactMessageCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int Id { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
