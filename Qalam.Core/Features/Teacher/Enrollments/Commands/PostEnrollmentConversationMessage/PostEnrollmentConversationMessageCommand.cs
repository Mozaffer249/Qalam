using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Enrollments.Commands.PostEnrollmentConversationMessage;

public class PostEnrollmentConversationMessageCommand : IRequest<Response<EnrollmentConversationMessageDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ConversationId { get; set; }
    public PostEnrollmentConversationMessageDto Data { get; set; } = default!;
}
