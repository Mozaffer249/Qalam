using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Enrollments.Commands.MarkEnrollmentConversationRead;

public class MarkEnrollmentConversationReadCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ConversationId { get; set; }
    public MarkEnrollmentConversationReadDto Data { get; set; } = new();
}
