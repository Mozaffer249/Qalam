using FluentValidation;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.CreateSessionOffer;

public class CreateSessionOfferCommandValidator : AbstractValidator<CreateSessionOfferCommand>
{
    public CreateSessionOfferCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        When(x => x.Data != null, () =>
        {
            RuleFor(x => x.Data.SessionRequestId).GreaterThan(0);
            RuleFor(x => x.Data.Price).GreaterThan(0).WithMessage("MUST_BE_POSITIVE");
            RuleFor(x => x.Data.TeacherNotes).MaximumLength(1000);
            RuleFor(x => x.Data.ValidityHours)
                .InclusiveBetween(24, 168)
                .WithMessage("INVALID_VALIDITY_HOURS");
            RuleFor(x => x.Data.CommitmentConfirmed)
                .Equal(true)
                .WithMessage("COMMITMENT_NOT_CONFIRMED");
        });
    }
}
