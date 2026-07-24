using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Student.Sessions.Commands.SubmitSessionReview;

public class SubmitStudentSessionReviewCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Feedback { get; set; }
}
