using FluentValidation;

namespace Qalam.Core.Extensions;

public static class FluentValidationExtensions
{
	/// <summary>
	/// Hides the field name from validation error messages.
	/// Use this to show only the custom validation message without the property name prefix.
	/// </summary>
	/// <example>
	/// RuleFor(x => x.DocumentNumber)
	///     .NotEmpty()
	///     .HideFieldName()  // Instead of .WithName("")
	///     .WithMessage(localizer[...]);
	/// </example>
	public static IRuleBuilderOptions<T, TProperty> HideFieldName<T, TProperty>(
		this IRuleBuilderOptions<T, TProperty> rule)
	{
		return rule.WithName("");
	}
}
