using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Sessions.Commands.SubmitSessionReview;

public class SubmitStudentSessionReviewCommandHandler : ResponseHandler,
    IRequestHandler<SubmitStudentSessionReviewCommand, Response<string>>
{
    private readonly ISessionReviewService _reviewService;

    public SubmitStudentSessionReviewCommandHandler(
        ISessionReviewService reviewService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _reviewService = reviewService;
    }

    public async Task<Response<string>> Handle(
        SubmitStudentSessionReviewCommand request,
        CancellationToken cancellationToken)
    {
        var (ok, message, forbidden, notFound) = await _reviewService.SubmitStudentReviewAsync(
            request.UserId, request.Id, request.Rating, request.Feedback, cancellationToken);

        if (forbidden) return Forbidden<string>(message);
        if (notFound) return NotFound<string>(message);
        if (!ok) return BadRequest<string>(message);
        return Success(entity: message);
    }
}
