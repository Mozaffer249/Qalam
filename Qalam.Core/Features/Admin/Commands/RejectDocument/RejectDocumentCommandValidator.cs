using FluentValidation;

namespace Qalam.Core.Features.Admin.Commands.RejectDocument;

public class RejectDocumentCommandValidator : AbstractValidator<RejectDocumentCommand>
{
	public RejectDocumentCommandValidator()
	{
		RuleFor(x => x.TeacherId)
			.GreaterThan(0)
			.WithMessage("Teacher ID must be greater than 0");

		RuleFor(x => x.DocumentId)
			.GreaterThan(0)
			.WithMessage("Document ID must be greater than 0");

		RuleFor(x => x.Reason)
			.NotEmpty()
			.WithMessage("Rejection reason is required")
			.MaximumLength(500)
			.WithMessage("Rejection reason cannot exceed 500 characters");
	}
}
