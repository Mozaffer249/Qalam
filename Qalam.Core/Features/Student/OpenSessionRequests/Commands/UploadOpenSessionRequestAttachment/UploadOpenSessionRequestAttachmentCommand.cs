using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.UploadOpenSessionRequestAttachment;

public class UploadOpenSessionRequestAttachmentCommand
    : IRequest<Response<OpenSessionRequestAttachmentDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int OpenSessionRequestId { get; set; }

    public IFormFile File { get; set; } = default!;
}
