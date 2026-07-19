using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CreateIndividualEnrollment;

public class CreateIndividualEnrollmentCommand
    : IRequest<Response<CreateIndividualEnrollmentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreateEnrollmentRequestDto Data { get; set; } = null!;
}
