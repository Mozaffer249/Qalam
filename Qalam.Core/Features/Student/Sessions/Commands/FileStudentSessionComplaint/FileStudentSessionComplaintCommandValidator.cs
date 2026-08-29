using FluentValidation;

namespace Qalam.Core.Features.Student.Sessions.Commands.FileStudentSessionComplaint;

public class FileStudentSessionComplaintCommandValidator : AbstractValidator<FileStudentSessionComplaintCommand>
{
    public FileStudentSessionComplaintCommandValidator()
    {
        RuleFor(x => x.ScheduleId).GreaterThan(0);
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(3);
    }
}
