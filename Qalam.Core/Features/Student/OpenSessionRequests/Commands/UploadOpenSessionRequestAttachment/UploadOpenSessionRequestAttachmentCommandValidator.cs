using FluentValidation;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.UploadOpenSessionRequestAttachment;

public class UploadOpenSessionRequestAttachmentCommandValidator
    : AbstractValidator<UploadOpenSessionRequestAttachmentCommand>
{
    public UploadOpenSessionRequestAttachmentCommandValidator()
    {
        RuleFor(x => x.OpenSessionRequestId).GreaterThan(0);
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(f => f != null && f.Length > 0).WithMessage("File is empty");
    }
}
