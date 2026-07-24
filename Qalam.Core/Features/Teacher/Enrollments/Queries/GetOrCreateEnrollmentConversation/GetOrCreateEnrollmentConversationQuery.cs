using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetOrCreateEnrollmentConversation;

public class GetOrCreateEnrollmentConversationQuery : IRequest<Response<EnrollmentConversationDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int EnrollmentId { get; set; }
}
