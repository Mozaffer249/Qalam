using FluentValidation;

namespace Qalam.Core.Features.Admin.Commands.ApproveDocument;

public class ApproveDocumentCommandValidator : AbstractValidator<ApproveDocumentCommand>
{
	public ApproveDocumentCommandValidator()
	{
		RuleFor(x => x.TeacherId)
			.GreaterThan(0)
			.WithMessage("Teacher ID must be greater than 0");

		RuleFor(x => x.DocumentId)
			.GreaterThan(0)
			.WithMessage("Document ID must be greater than 0");
	}
}
