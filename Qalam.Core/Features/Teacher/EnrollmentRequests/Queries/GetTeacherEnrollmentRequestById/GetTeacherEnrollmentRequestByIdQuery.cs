using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Queries.GetTeacherEnrollmentRequestById;

public class GetTeacherEnrollmentRequestByIdQuery : IRequest<Response<TeacherEnrollmentRequestDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
