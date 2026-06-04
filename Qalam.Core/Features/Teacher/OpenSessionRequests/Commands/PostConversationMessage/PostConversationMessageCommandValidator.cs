using FluentValidation;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.PostConversationMessage;

public class PostConversationMessageCommandValidator : AbstractValidator<PostConversationMessageCommand>
{
    public PostConversationMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        When(x => x.Data != null, () =>
        {
            RuleFor(x => x.Data.Content)
                .NotEmpty()
                .MaximumLength(4000);
        });
    }
}
