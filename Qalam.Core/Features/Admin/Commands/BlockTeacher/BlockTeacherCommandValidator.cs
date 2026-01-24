using FluentValidation;

namespace Qalam.Core.Features.Admin.Commands.BlockTeacher;

public class BlockTeacherCommandValidator : AbstractValidator<BlockTeacherCommand>
{
	public BlockTeacherCommandValidator()
	{
		RuleFor(x => x.TeacherId)
			.GreaterThan(0)
			.WithMessage("Teacher ID must be greater than 0");

		RuleFor(x => x.Reason)
			.MaximumLength(500)
			.WithMessage("Reason cannot exceed 500 characters")
			.When(x => !string.IsNullOrEmpty(x.Reason));
	}
}
