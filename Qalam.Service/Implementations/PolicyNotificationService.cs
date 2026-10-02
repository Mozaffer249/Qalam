using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public static class PolicyNotificationTypes
{
    public const string EnrollmentCancelled = "EnrollmentCancelled";
    public const string RefundApproved = "RefundApproved";
    public const string RefundCompleted = "RefundCompleted";
    public const string SessionCancelled = "SessionCancelled";
    public const string TeacherNoShowRecorded = "TeacherNoShowRecorded";
    public const string StudentNoShowRecorded = "StudentNoShowRecorded";
    public const string ReplacementSessionCreated = "ReplacementSessionCreated";
    public const string WalletCredited = "WalletCredited";
}

/// <summary>
/// Student-side recipients (payer, owner, students, guardians) see refund amounts;
/// the teacher only ever sees the effect on their own earnings.
/// </summary>
public class PolicyNotificationService : IPolicyNotificationService
{
    private readonly ApplicationDBContext _db;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<PolicyNotificationService> _logger;

    public PolicyNotificationService(
        ApplicationDBContext db,
        INotificationDispatcher dispatcher,
        ILogger<PolicyNotificationService> logger)
    {
        _db = db;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task NotifyCaseAsync(PolicyCase policyCase, CancellationToken cancellationToken = default)
    {
        if (policyCase.Kind == PolicyCaseKind.Reversal)
            return;

        try
        {
            var (studentSide, teacherUserId) = await RecipientsAsync(policyCase.EnrollmentId, cancellationToken);
            await NotifyMainEventAsync(policyCase, studentSide, teacherUserId, cancellationToken);
            await NotifyMoneyAsync(policyCase, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications for policy case {PolicyCaseId}.", policyCase.Id);
        }
    }

    private async Task NotifyMainEventAsync(
        PolicyCase c,
        List<int> studentSide,
        int? teacherUserId,
        CancellationToken cancellationToken)
    {
        var type = await MainTypeAsync(c, cancellationToken);
        if (type == null)
            return;

        var data = Data(c);
        var refund = c.RefundAmount > 0 ? Money(c.RefundAmount, c.Currency) : null;
        var session = c.CourseScheduleId is int sid ? $"#{sid}" : string.Empty;
        var enrollment = $"#{c.EnrollmentId}";

        (string TitleAr, string TitleEn, string BodyAr, string BodyEn) student = type switch
        {
            PolicyNotificationTypes.EnrollmentCancelled => (
                "تم إلغاء الاشتراك", "Enrollment cancelled",
                $"تم إلغاء الاشتراك {enrollment}." + (refund != null ? $" مبلغ الاسترداد: {refund}." : string.Empty),
                $"Enrollment {enrollment} was cancelled." + (refund != null ? $" Refund: {refund}." : string.Empty)),
            PolicyNotificationTypes.TeacherNoShowRecorded => (
                "لم يحضر المعلم الجلسة", "Teacher did not attend",
                $"تم تسجيل غياب المعلم عن الجلسة {session}. لن تُحتسب عليك الجلسة." + (refund != null ? $" مبلغ الاسترداد: {refund}." : string.Empty),
                $"The teacher missed session {session}. It will not count against you." + (refund != null ? $" Refund: {refund}." : string.Empty)),
            PolicyNotificationTypes.StudentNoShowRecorded => (
                "تم تسجيل غياب الطالب", "Student no-show recorded",
                $"تم تسجيل غياب الطالب عن الجلسة {session} واحتسابها كجلسة مستخدمة.",
                $"A no-show was recorded for session {session}; it counts as used."),
            PolicyNotificationTypes.ReplacementSessionCreated => (
                "تم إنشاء جلسة بديلة", "Replacement session created",
                $"تم إنشاء جلسة بديلة للجلسة {session}. راجع جدولك للموعد الجديد.",
                $"A replacement for session {session} was created. Check your schedule for the new time."),
            _ => (
                "تم إلغاء الجلسة", "Session cancelled",
                $"تم إلغاء الجلسة {session}." + (refund != null ? $" مبلغ الاسترداد: {refund}." : string.Empty),
                $"Session {session} was cancelled." + (refund != null ? $" Refund: {refund}." : string.Empty))
        };

        await _dispatcher.NotifyAsync(studentSide,
            new NotificationContent(type, student.TitleAr, student.TitleEn, student.BodyAr, student.BodyEn, data),
            cancellationToken);

        if (teacherUserId is not int teacherId)
            return;

        var earnings = c.TeacherEarningImpact != 0 ? SignedMoney(c.TeacherEarningImpact, c.Currency) : null;
        var earningsAr = earnings != null ? $" الأثر على أرباحك: {earnings}." : string.Empty;
        var earningsEn = earnings != null ? $" Effect on your earnings: {earnings}." : string.Empty;

        (string TitleAr, string TitleEn, string BodyAr, string BodyEn) teacher = type switch
        {
            PolicyNotificationTypes.EnrollmentCancelled => (
                "تم إلغاء اشتراك", "Enrollment cancelled",
                $"تم إلغاء الاشتراك {enrollment}.{earningsAr}",
                $"Enrollment {enrollment} was cancelled.{earningsEn}"),
            PolicyNotificationTypes.TeacherNoShowRecorded => (
                "تم تسجيل غيابك عن جلسة", "No-show recorded",
                $"تم تسجيل غيابك عن الجلسة {session}.{earningsAr}",
                $"You were recorded as absent for session {session}.{earningsEn}"),
            PolicyNotificationTypes.StudentNoShowRecorded => (
                "غياب الطالب", "Student no-show",
                $"تم تسجيل غياب الطالب عن الجلسة {session}.{earningsAr}",
                $"The student missed session {session}.{earningsEn}"),
            PolicyNotificationTypes.ReplacementSessionCreated => (
                "تم إنشاء جلسة بديلة", "Replacement session created",
                $"تم إنشاء جلسة بديلة للجلسة {session}.{earningsAr}",
                $"A replacement for session {session} was created.{earningsEn}"),
            _ => (
                "تم إلغاء جلسة", "Session cancelled",
                $"تم إلغاء الجلسة {session}.{earningsAr}",
                $"Session {session} was cancelled.{earningsEn}")
        };

        await _dispatcher.NotifyAsync(new[] { teacherId },
            new NotificationContent(type, teacher.TitleAr, teacher.TitleEn, teacher.BodyAr, teacher.BodyEn, data),
            cancellationToken);
    }

    private async Task<string?> MainTypeAsync(PolicyCase c, CancellationToken cancellationToken)
    {
        switch (c.Kind)
        {
            case PolicyCaseKind.BeforeFirstSessionCancel:
            case PolicyCaseKind.AfterFirstSessionCancel:
                return PolicyNotificationTypes.EnrollmentCancelled;
            case PolicyCaseKind.TeacherNoShow:
                return PolicyNotificationTypes.TeacherNoShowRecorded;
            case PolicyCaseKind.StudentNoShow:
                return PolicyNotificationTypes.StudentNoShowRecorded;
        }

        if (c.ReplacementScheduleId != null)
            return PolicyNotificationTypes.ReplacementSessionCreated;

        if (c.CourseScheduleId is int scheduleId)
        {
            var status = await _db.CourseSchedules.AsNoTracking()
                .Where(s => s.Id == scheduleId)
                .Select(s => (ScheduleStatus?)s.Status)
                .FirstOrDefaultAsync(cancellationToken);
            if (status == ScheduleStatus.Cancelled)
                return PolicyNotificationTypes.SessionCancelled;
        }

        return null;
    }

    /// <summary>Wallet credits and original-method refunds go to whoever receives the money.</summary>
    private async Task NotifyMoneyAsync(PolicyCase c, CancellationToken cancellationToken)
    {
        var data = Data(c);

        var credits = await _db.WalletTransactions.AsNoTracking()
            .Where(t => t.PolicyCaseId == c.Id
                        && (t.Type == WalletTransactionType.Refund || t.Type == WalletTransactionType.AdminCredit))
            .Select(t => new { t.Wallet.UserId, t.Amount, t.Currency })
            .ToListAsync(cancellationToken);

        foreach (var credit in credits)
        {
            var amount = Money(credit.Amount, credit.Currency);
            await _dispatcher.NotifyAsync(new[] { credit.UserId },
                new NotificationContent(PolicyNotificationTypes.WalletCredited,
                    "تمت إضافة رصيد إلى محفظتك", "Wallet credited",
                    $"تمت إضافة {amount} إلى محفظتك في قلم.",
                    $"{amount} was added to your Qalam wallet.",
                    data),
                cancellationToken);
        }

        var refunds = await _db.Refunds.AsNoTracking()
            .Where(r => (r.PolicyCaseId == c.Id || r.Id == c.RefundId)
                        && r.Destination == RefundDestination.OriginalMethod
                        && r.Amount > 0)
            .Select(r => new { r.Payment.PayerUserId, r.Amount, r.Currency, r.Status })
            .ToListAsync(cancellationToken);

        foreach (var refund in refunds)
        {
            var amount = Money(refund.Amount, refund.Currency);
            var content = refund.Status == RefundStatus.Succeeded
                ? new NotificationContent(PolicyNotificationTypes.RefundCompleted,
                    "تم استرداد المبلغ", "Refund completed",
                    $"تم استرداد {amount} إلى وسيلة الدفع الأصلية. قد يستغرق ظهوره بعض الوقت حسب البنك.",
                    $"{amount} was refunded to your original payment method. Your bank may take a few days to show it.",
                    data)
                : new NotificationContent(PolicyNotificationTypes.RefundApproved,
                    "تمت الموافقة على الاسترداد", "Refund approved",
                    $"تمت الموافقة على استرداد {amount} إلى وسيلة الدفع الأصلية وهو قيد المعالجة.",
                    $"A refund of {amount} to your original payment method was approved and is being processed.",
                    data);
            await _dispatcher.NotifyAsync(new[] { refund.PayerUserId }, content, cancellationToken);
        }
    }

    private async Task<(List<int> StudentSide, int? TeacherUserId)> RecipientsAsync(int enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await _db.Enrollments.AsNoTracking()
            .Where(e => e.Id == enrollmentId)
            .Select(e => new
            {
                e.PaidByUserId,
                e.OwnerUserId,
                e.LeaderStudentId,
                e.ApprovedByTeacherId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (enrollment == null)
            return (new List<int>(), null);

        var teacherUserId = await _db.Teachers.AsNoTracking()
            .Where(t => t.Id == enrollment.ApprovedByTeacherId)
            .Select(t => t.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        var studentIds = await _db.EnrollmentParticipants.AsNoTracking()
            .Where(p => p.EnrollmentId == enrollmentId)
            .Select(p => p.StudentId)
            .ToListAsync(cancellationToken);
        if (enrollment.LeaderStudentId is int leaderId)
            studentIds.Add(leaderId);

        var students = await _db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.UserId, GuardianUserId = s.Guardian != null ? s.Guardian.UserId : null })
            .ToListAsync(cancellationToken);

        var userIds = new List<int>();
        if (enrollment.PaidByUserId is int payer)
            userIds.Add(payer);
        if (enrollment.OwnerUserId is int owner)
            userIds.Add(owner);
        foreach (var s in students)
        {
            userIds.Add(s.UserId);
            if (s.GuardianUserId is int guardian)
                userIds.Add(guardian);
        }

        var studentSide = userIds.Distinct().Where(id => id != teacherUserId).ToList();
        return (studentSide, teacherUserId);
    }

    private static Dictionary<string, object?> Data(PolicyCase c) => new()
    {
        ["policyCaseId"] = c.Id,
        ["enrollmentId"] = c.EnrollmentId,
        ["scheduleId"] = c.CourseScheduleId,
        ["replacementScheduleId"] = c.ReplacementScheduleId
    };

    private static string Money(decimal amount, string? currency)
        => $"{amount.ToString("0.##", CultureInfo.InvariantCulture)} {currency}".Trim();

    private static string SignedMoney(decimal amount, string? currency)
        => (amount > 0 ? "+" : amount < 0 ? "-" : string.Empty) + Money(Math.Abs(amount), currency);
}
