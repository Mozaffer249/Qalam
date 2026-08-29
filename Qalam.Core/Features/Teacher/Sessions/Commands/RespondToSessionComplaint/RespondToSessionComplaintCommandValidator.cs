using FluentValidation;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.RespondToSessionComplaint;

public class RespondToSessionComplaintCommandValidator : AbstractValidator<RespondToSessionComplaintCommand>
{
    public RespondToSessionComplaintCommandValidator()
    {
        RuleFor(x => x.ScheduleId).GreaterThan(0);
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.Response)
            .NotEmpty()
            .MinimumLength(3);
    }
}
