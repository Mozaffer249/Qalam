using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetEnrollmentConversationMessages;

public class GetEnrollmentConversationMessagesQuery : IRequest<Response<EnrollmentConversationMessagesPageDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ConversationId { get; set; }
    public string? Cursor { get; set; }
    public int Take { get; set; } = 50;
    public string Direction { get; set; } = "older";
}
