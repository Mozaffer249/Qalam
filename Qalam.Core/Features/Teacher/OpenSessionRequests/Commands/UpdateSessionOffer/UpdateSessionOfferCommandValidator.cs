using FluentValidation;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.UpdateSessionOffer;

public class UpdateSessionOfferCommandValidator : AbstractValidator<UpdateSessionOfferCommand>
{
    public UpdateSessionOfferCommandValidator()
    {
        RuleFor(x => x.OfferId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        When(x => x.Data != null, () =>
        {
            RuleFor(x => x.Data.Price)
                .GreaterThan(0).When(x => x.Data.Price.HasValue)
                .WithMessage("MUST_BE_POSITIVE");
            RuleFor(x => x.Data.TeacherNotes)
                .MaximumLength(1000).When(x => x.Data.TeacherNotes != null);
            // ValidityHours is ignored; offer expiry is not rewritten on update.
        });
    }
}
