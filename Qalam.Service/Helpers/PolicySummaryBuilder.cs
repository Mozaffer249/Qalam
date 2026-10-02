using Qalam.Data.DTOs.Policy;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Helpers;

/// <summary>Turns policy rules into short bilingual sentences for students and teachers.</summary>
public static class PolicySummaryBuilder
{
    public static PolicySummaryDto ForStudent(ResolvedPolicy policy, DateTime? effectiveFrom = null)
    {
        var r = policy.Rules;
        var dto = new PolicySummaryDto { PolicyVersionNumber = policy.VersionNumber, EffectiveFrom = effectiveFrom };

        var before = r.BeforeFirstSession;
        dto.Sections.Add(Section("beforeFirstSession", before.Enabled, before.Enabled
            ? new[]
            {
                L($"يمكنك إلغاء التسجيل قبل أول جلسة واسترداد {before.RefundPct:0.##}% من المبلغ المدفوع{Fee(before.FixedFee, true)}.",
                  $"You can cancel before the first session and get {before.RefundPct:0.##}% of what you paid back{Fee(before.FixedFee, false)}."),
                before.MinHoursBeforeFirstSession > 0
                    ? L($"يجب الإلغاء قبل {before.MinHoursBeforeFirstSession} ساعة على الأقل من أول جلسة.",
                        $"Cancel at least {before.MinHoursBeforeFirstSession} hours before the first session.")
                    : null,
                L($"يُرد المبلغ إلى {Dest(before.Destination, true)}.", $"Refunds go to your {Dest(before.Destination, false)}.")
            }
            : new[] { L("لا يمكن إلغاء التسجيل بعد الدفع.", "The enrollment cannot be cancelled after payment.") }));

        var after = r.AfterFirstSession;
        dto.Sections.Add(Section("afterFirstSession", after.Enabled, after.Enabled
            ? new[]
            {
                L($"بعد بدء الجلسات يُسترد {after.RefundPct:0.##}% من قيمة الجلسات غير المستخدمة{Fee(after.FixedFee, true)}.",
                  $"After sessions start, {after.RefundPct:0.##}% of the unused sessions' value is refunded{Fee(after.FixedFee, false)}."),
                after.CountStudentNoShowAsUsed
                    ? L("الجلسات التي غبت عنها تُحتسب مستخدمة.", "Sessions you missed count as used.")
                    : null
            }
            : new[] { L("لا يمكن إلغاء التسجيل بعد بدء الجلسات.", "The enrollment cannot be cancelled after sessions start.") }));

        var session = r.SessionCancellation;
        dto.Sections.Add(Section("sessionCancellation", session.Enabled, session.Enabled
            ? new[]
            {
                L($"إلغاء جلسة قبل {session.NoticeHours} ساعة على الأقل: {InWindow(session.InWindowOutcome, true)}.",
                  $"Cancel a session at least {session.NoticeHours} hours ahead: {InWindow(session.InWindowOutcome, false)}."),
                session.LateOutcome == LateSessionOutcome.PartialRefund && session.LateRefundPct > 0
                    ? L($"الإلغاء المتأخر: استرداد {session.LateRefundPct:0.##}% من قيمة الجلسة.",
                        $"Late cancellation: {session.LateRefundPct:0.##}% of the session value is refunded.")
                    : L("الإلغاء المتأخر: تُحتسب الجلسة مستخدمة.", "Late cancellation: the session counts as used.")
            }
            : new[] { L("لا يمكن إلغاء جلسة منفردة.", "Single sessions cannot be cancelled.") }));

        var teacher = r.TeacherNoShow;
        dto.Sections.Add(Section("teacherNoShow", teacher.Enabled, new[]
        {
            teacher.Enabled
                ? L($"إذا لم يحضر المعلم أو ألغى الجلسة: {TeacherOutcome(teacher.Outcome, true)}.",
                    $"If the teacher misses or cancels a session: {TeacherOutcome(teacher.Outcome, false)}.")
                : L("إذا لم يحضر المعلم ستتواصل معك الإدارة.", "If the teacher misses a session, our team will contact you.")
        }));

        var student = r.StudentNoShow;
        dto.Sections.Add(Section("studentNoShow", student.Enabled, new[]
        {
            !student.Enabled || student.ConsideredUsed
                ? L("إذا غبت عن الجلسة دون إلغاء تُحتسب مستخدمة.", "If you miss a session without cancelling, it counts as used.")
                : L("إذا غبت عن الجلسة لا تُحتسب مستخدمة.", "A missed session does not count as used."),
            student.Enabled && student.AllowExceptionRequest
                ? L("يمكنك طلب استثناء عبر الدعم.", "You can ask support for an exception.")
                : null
        }));

        var tech = r.TechnicalIssue;
        dto.Sections.Add(Section("technicalIssue", tech.Enabled, new[]
        {
            tech.Enabled
                ? L($"أبلغ عن المشكلة التقنية خلال {tech.ReportWindowHours} ساعة من الجلسة: {TechOutcome(tech.Outcome, true)}.",
                    $"Report a technical issue within {tech.ReportWindowHours} hours of the session: {TechOutcome(tech.Outcome, false)}.")
                : L("تُراجع المشاكل التقنية من قبل الإدارة.", "Technical issues are reviewed by our team.")
        }));

        return dto;
    }

    /// <summary>Only what affects the teacher: their earnings and obligations.</summary>
    public static PolicySummaryDto ForTeacher(ResolvedPolicy policy, DateTime? effectiveFrom = null)
    {
        var r = policy.Rules;
        var dto = new PolicySummaryDto { PolicyVersionNumber = policy.VersionNumber, EffectiveFrom = effectiveFrom };

        dto.Sections.Add(Section("teacherNoShow", r.TeacherNoShow.Enabled, new[]
        {
            L("إذا لم تحضر جلسة أو ألغيتها، يحصل الطالب على تعويض ولا تُحتسب لك الجلسة.",
              "If you miss or cancel a session, the student is compensated and the session is not paid."),
            r.TeacherNoShow.TeacherEarningEffect == TeacherEarningEffect.Penalty && r.TeacherNoShow.TeacherPenaltyAmount > 0
                ? L($"قد تُخصم غرامة {r.TeacherNoShow.TeacherPenaltyAmount:0.##} من مستحقاتك.",
                    $"A penalty of {r.TeacherNoShow.TeacherPenaltyAmount:0.##} may be deducted from your earnings.")
                : null
        }));

        dto.Sections.Add(Section("studentNoShow", r.StudentNoShow.Enabled, new[]
        {
            r.StudentNoShow.TeacherEarningEffect == TeacherEarningEffect.Keep || !r.StudentNoShow.Enabled
                ? L("إذا غاب الطالب وحضرت أنت، تُحتسب لك الجلسة.", "If the student misses a session you attended, you are still paid.")
                : L("إذا غاب الطالب قد تتأثر مستحقات الجلسة حسب السياسة.", "If the student misses a session, its earning may be adjusted by the policy.")
        }));

        dto.Sections.Add(Section("sessionCancellation", r.SessionCancellation.Enabled, new[]
        {
            L($"يمكن للطالب إلغاء جلسة قبل {r.SessionCancellation.NoticeHours} ساعة؛ الجلسات الملغاة لا تُحتسب.",
              $"Students may cancel a session {r.SessionCancellation.NoticeHours} hours ahead; cancelled sessions are not paid.")
        }));

        dto.Sections.Add(Section("technicalIssue", r.TechnicalIssue.Enabled, new[]
        {
            r.TechnicalIssue.TeacherEarningEffect == TeacherEarningEffect.Keep
                ? L("المشاكل التقنية لا تؤثر على مستحقاتك.", "Technical issues do not affect your earnings.")
                : L("عند ثبوت مشكلة تقنية قد تُلغى مستحقات الجلسة أو تُعوض بجلسة بديلة.",
                    "When a technical issue is confirmed, the session earning may be cancelled or replaced by a make-up session.")
        }));

        return dto;
    }

    private static PolicySummarySectionDto Section(string key, bool enabled, IEnumerable<PolicyExplanationDto?> lines) => new()
    {
        Section = key,
        Enabled = enabled,
        Lines = lines.Where(l => l != null).Select(l => l!).ToList()
    };

    private static PolicyExplanationDto L(string ar, string en) => new() { Ar = ar, En = en };

    private static string Fee(decimal fee, bool ar) => fee <= 0 ? "" : ar ? $" بعد خصم رسوم {fee:0.##}" : $" minus a {fee:0.##} fee";

    private static string Dest(Data.Entity.Common.Enums.RefundDestination d, bool ar)
        => d == Data.Entity.Common.Enums.RefundDestination.Wallet ? (ar ? "محفظتك" : "wallet") : (ar ? "وسيلة الدفع الأصلية" : "original payment method");

    private static string InWindow(InWindowSessionOutcome o, bool ar) => o switch
    {
        InWindowSessionOutcome.Refund => ar ? "استرداد قيمة الجلسة" : "the session value is refunded",
        InWindowSessionOutcome.Reschedule => ar ? "إعادة جدولة دون خصم" : "reschedule at no cost",
        _ => ar ? "اختر الاسترداد أو إعادة الجدولة" : "choose a refund or a reschedule"
    };

    private static string TeacherOutcome(TeacherNoShowOutcome o, bool ar) => o switch
    {
        TeacherNoShowOutcome.Replacement => ar ? "جلسة بديلة مجانية" : "a free replacement session",
        TeacherNoShowOutcome.RefundAndReplacement => ar ? "استرداد وجلسة بديلة" : "a refund and a replacement session",
        TeacherNoShowOutcome.StudentChoice => ar ? "تختار الاسترداد أو جلسة بديلة" : "you choose a refund or a replacement",
        _ => ar ? "استرداد قيمة الجلسة" : "the session value is refunded"
    };

    private static string TechOutcome(TechnicalIssueOutcome o, bool ar) => o switch
    {
        TechnicalIssueOutcome.FullRefund => ar ? "استرداد كامل لقيمة الجلسة" : "a full refund of the session",
        TechnicalIssueOutcome.PartialRefund => ar ? "استرداد جزئي" : "a partial refund",
        TechnicalIssueOutcome.Reschedule => ar ? "إعادة جدولة الجلسة" : "the session is rescheduled",
        _ => ar ? "جلسة بديلة" : "a replacement session"
    };
}
