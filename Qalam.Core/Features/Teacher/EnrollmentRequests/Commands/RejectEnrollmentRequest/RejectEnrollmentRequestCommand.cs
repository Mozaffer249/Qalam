using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.RejectEnrollmentRequest;

public class RejectEnrollmentRequestCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int RequestId { get; set; }

    public RejectEnrollmentRequestDto Data { get; set; } = null!;
}
