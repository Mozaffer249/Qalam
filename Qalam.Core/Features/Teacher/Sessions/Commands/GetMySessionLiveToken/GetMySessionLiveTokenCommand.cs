using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Live;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.GetMySessionLiveToken;

public class GetMySessionLiveTokenCommand : IRequest<Response<LiveSessionAccessDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
