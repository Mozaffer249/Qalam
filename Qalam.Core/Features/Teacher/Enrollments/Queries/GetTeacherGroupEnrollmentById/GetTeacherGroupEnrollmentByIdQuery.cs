using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherGroupEnrollmentById;

public class GetTeacherGroupEnrollmentByIdQuery : IRequest<Response<TeacherGroupEnrollmentDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
}
