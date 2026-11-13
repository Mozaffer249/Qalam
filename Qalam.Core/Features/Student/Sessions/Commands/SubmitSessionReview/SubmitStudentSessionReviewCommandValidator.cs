using FluentValidation;

namespace Qalam.Core.Features.Student.Sessions.Commands.SubmitSessionReview;

public class SubmitStudentSessionReviewCommandValidator : AbstractValidator<SubmitStudentSessionReviewCommand>
{
    public SubmitStudentSessionReviewCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        // NELC's "rated" xAPI statement requires a text response, so feedback is mandatory.
        RuleFor(x => x.Feedback)
            .NotEmpty().WithMessage("Feedback is required.")
            .MinimumLength(15).WithMessage("Feedback must be at least 15 characters.")
            .MaximumLength(600).WithMessage("Feedback must not exceed 600 characters.");
    }
}
