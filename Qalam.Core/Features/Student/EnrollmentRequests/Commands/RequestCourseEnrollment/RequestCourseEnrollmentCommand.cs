using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommand : IRequest<Response<EnrollmentRequestDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public CreateEnrollmentRequestDto Data { get; set; } = null!;
}
