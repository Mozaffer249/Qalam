using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.Logout
{
    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("Access token is required");
        }
    }
}

