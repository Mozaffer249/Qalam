using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.DeleteOpenSessionRequestAttachment;

public class DeleteOpenSessionRequestAttachmentCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int OpenSessionRequestId { get; set; }
    public int AttachmentId { get; set; }
}
