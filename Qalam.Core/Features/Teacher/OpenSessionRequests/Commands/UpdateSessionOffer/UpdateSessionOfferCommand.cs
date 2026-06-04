using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.UpdateSessionOffer;

public class UpdateSessionOfferCommand : IRequest<Response<TeacherOfferDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int OfferId { get; set; }
    public UpdateSessionOfferDto Data { get; set; } = default!;
}
