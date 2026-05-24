using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetOpenSessionRequestById;

public class GetOpenSessionRequestByIdQuery : IRequest<Response<OpenSessionRequestDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
