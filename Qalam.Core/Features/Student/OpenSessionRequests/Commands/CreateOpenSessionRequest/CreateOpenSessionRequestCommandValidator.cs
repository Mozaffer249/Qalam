using FluentValidation;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.CreateOpenSessionRequest;

public class CreateOpenSessionRequestCommandValidator : AbstractValidator<CreateOpenSessionRequestCommand>
{
    private const int MaxInvitations = 5;
    private const int MaxSessionsCount = 50;

    public CreateOpenSessionRequestCommandValidator()
    {
        RuleFor(x => x.Data).NotNull().WithMessage("Body is required.");

        When(x => x.Data is not null, () =>
        {
            RuleFor(x => x.Data.StudentId).GreaterThan(0);
            RuleFor(x => x.Data.DomainId).GreaterThan(0);
            RuleFor(x => x.Data.SubjectId).GreaterThan(0);
            RuleFor(x => x.Data.TeachingModeId).GreaterThan(0);

            RuleFor(x => x.Data.TotalSessionsCount)
                .InclusiveBetween(1, MaxSessionsCount)
                .WithMessage($"عدد الجلسات يجب أن يكون بين 1 و {MaxSessionsCount}");

            RuleFor(x => x.Data.StudentNotes)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Data.StudentNotes));

            RuleFor(x => x.Data.Sessions)
                .NotEmpty().WithMessage("يجب تحديد جلسة واحدة على الأقل")
                .Must((cmd, sessions) => sessions.Count == cmd.Data.TotalSessionsCount)
                .WithMessage("عدد الجلسات لا يطابق TotalSessionsCount");

            RuleForEach(x => x.Data.Sessions).ChildRules(s =>
            {
                s.RuleFor(x => x.SequenceNumber).GreaterThan(0);
                s.RuleFor(x => x.TimeSlotId).GreaterThan(0)
                    .WithMessage("يجب اختيار فترة زمنية مفضلة (TimeSlotId)");
                s.RuleFor(x => x.PreferredDate)
                    .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
                    .WithMessage("التاريخ المفضل يجب أن يكون اليوم أو لاحقاً");
                s.RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 360);
                s.RuleFor(x => x.Notes).MaximumLength(500)
                    .When(x => !string.IsNullOrEmpty(x.Notes));

                s.RuleForEach(x => x.Units).ChildRules(u =>
                {
                    u.RuleFor(x => x)
                        .Must(unit => (unit.ContentUnitId.HasValue ^ unit.LessonId.HasValue))
                        .WithMessage("يجب تحديد ContentUnitId أو LessonId (واحد فقط، ليس كلاهما)");
                });
            });

            // Unique sequence numbers within Sessions
            RuleFor(x => x.Data.Sessions).Must(sessions =>
                sessions.Select(s => s.SequenceNumber).Distinct().Count() == sessions.Count)
                .WithMessage("أرقام تسلسل الجلسات يجب أن تكون فريدة");

            // Invitations
            RuleFor(x => x.Data.InvitedStudentIds)
                .Must(ids => ids.Count <= MaxInvitations)
                .WithMessage($"الحد الأقصى لعدد الدعوات هو {MaxInvitations}")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("توجد دعوات مكررة لنفس الطالب");

            RuleFor(x => x.Data)
                .Must(data => !data.InvitedStudentIds.Contains(data.StudentId))
                .WithMessage("لا يمكن دعوة الطالب نفسه");

            // ExpiresAt window (if provided)
            RuleFor(x => x.Data.ExpiresAt)
                .Must(d => !d.HasValue ||
                    (d.Value > DateTime.UtcNow.AddHours(23) && d.Value < DateTime.UtcNow.AddDays(31)))
                .WithMessage("تاريخ انتهاء صلاحية الطلب يجب أن يكون بين يوم و 30 يوماً من الآن");
        });
    }
}
