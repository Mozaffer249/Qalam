using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Live;

namespace Qalam.Core.Features.Student.Sessions.Commands.GetSessionLiveToken;

public class GetStudentSessionLiveTokenCommand : IRequest<Response<LiveSessionAccessDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
