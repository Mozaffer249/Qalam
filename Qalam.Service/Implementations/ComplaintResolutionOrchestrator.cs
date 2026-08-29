using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Service.Implementations;

public class ComplaintResolutionOrchestrator : IComplaintResolutionOrchestrator
{
    private readonly ISessionComplaintRepository _complaints;
    private readonly ICourseScheduleRepository _schedules;
    private readonly IRefundService _refundService;
    private readonly ISessionAuditService _audit;

    public ComplaintResolutionOrchestrator(
        ISessionComplaintRepository complaints,
        ICourseScheduleRepository schedules,
        IRefundService refundService,
        ISessionAuditService audit)
    {
        _complaints = complaints;
        _schedules = schedules;
        _refundService = refundService;
        _audit = audit;
    }

    public Task<ComplaintResolvePreviewDto> GetPreviewAsync(
        int scheduleId,
        int complaintId,
        SessionComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default) =>
        BuildPreviewAsync(
            scheduleId,
            complaintId,
            resolutionCode,
            refundAmountOverride,
            paymentIdOverride,
            cancellationToken);

    public async Task ResolveAsync(
        int scheduleId,
        int complaintId,
        int adminUserId,
        SessionComplaintResolution resolutionCode,
        string? resolutionNotes,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default)
    {
        await EnsureComplaintOnScheduleAsync(scheduleId, complaintId, cancellationToken);

        var complaint = await _complaints.GetByIdTrackedAsync(complaintId, cancellationToken)
            ?? throw new InvalidOperationException("Complaint not found.");

        if (!SessionComplaintRules.IsBlockingStatus(complaint.Status))
            throw new InvalidOperationException("Complaint is already closed.");

        var schedule = await _schedules.GetByIdNoTrackingAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        var financial = await _complaints.LoadFinancialContextAsync(
            complaint.EnrollmentId,
            scheduleId,
            cancellationToken)
            ?? throw new InvalidOperationException("Enrollment financial context not found.");

        var plan = BuildPlan(resolutionCode, financial, refundAmountOverride, paymentIdOverride);

        int? replacementScheduleId = null;
        if (plan.CreateReplacementSchedule)
            replacementScheduleId = await CreateReplacementScheduleAsync(complaint, schedule, cancellationToken);

        if (plan.SessionEarningEffect == ComplaintSessionEarningEffect.Void)
            await VoidEarningForScheduleAsync(schedule.Id, cancellationToken);

        int? refundId = null;
        if (plan.IssueRefund)
        {
            if (!plan.PaymentId.HasValue || plan.PaymentId.Value <= 0)
                throw new InvalidOperationException("No succeeded payment found for refund.");

            if (plan.RefundAmount <= 0)
                throw new InvalidOperationException("Refund amount must be positive.");

            var refund = await _refundService.IssueRefundAsync(
                plan.PaymentId.Value,
                complaint.EnrollmentId,
                plan.RefundAmount,
                plan.Currency,
                resolutionNotes ?? $"Session complaint #{complaintId}",
                adminUserId,
                cancellationToken);
            refundId = refund.Id;
        }

        complaint.Status = resolutionCode == SessionComplaintResolution.RejectComplaint
            ? SessionComplaintStatus.Rejected
            : SessionComplaintStatus.Resolved;
        complaint.ResolutionCode = resolutionCode;
        complaint.ResolutionNotes = resolutionNotes;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedByUserId = adminUserId;
        complaint.RequiresTeacherResponse = false;
        complaint.RefundId = refundId;
        complaint.ReplacementScheduleId = replacementScheduleId;
        await _complaints.SaveChangesAsync(cancellationToken);

        if (plan.SessionEarningEffect == ComplaintSessionEarningEffect.Release)
            await ReleaseEarningForScheduleAsync(schedule.Id, cancellationToken);

        if (plan.WarnTeacher)
        {
            await _audit.LogAsync(
                schedule.Id,
                adminUserId,
                "Admin",
                SessionAuditActionType.TeacherWarned,
                new { notes = resolutionNotes, complaintId },
                cancellationToken);
        }

        if (replacementScheduleId.HasValue)
        {
            await _audit.LogAsync(
                schedule.Id,
                adminUserId,
                "Admin",
                SessionAuditActionType.ReplacementSessionGranted,
                new { complaintId, replacementScheduleId },
                cancellationToken);
        }

        await _audit.LogAsync(
            schedule.Id,
            adminUserId,
            "Admin",
            SessionAuditActionType.ComplaintStatusChanged,
            new { complaintId, resolutionCode = resolutionCode.ToString(), refundId, replacementScheduleId },
            cancellationToken);
    }

    private async Task EnsureComplaintOnScheduleAsync(
        int scheduleId,
        int complaintId,
        CancellationToken cancellationToken)
    {
        if (!await _complaints.BelongsToScheduleAsync(complaintId, scheduleId, cancellationToken))
            throw new InvalidOperationException("Complaint not found.");
    }

    private async Task<ComplaintResolvePreviewDto> BuildPreviewAsync(
        int scheduleId,
        int complaintId,
        SessionComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken)
    {
        await EnsureComplaintOnScheduleAsync(scheduleId, complaintId, cancellationToken);

        var complaint = await _complaints.GetByIdAsync(complaintId, cancellationToken)
            ?? throw new InvalidOperationException("Complaint not found.");

        var schedule = await _schedules.GetByIdNoTrackingAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        var financial = await _complaints.LoadFinancialContextAsync(
            complaint.EnrollmentId,
            scheduleId,
            cancellationToken)
            ?? throw new InvalidOperationException("Enrollment financial context not found.");

        var plan = BuildPlan(resolutionCode, financial, refundAmountOverride, paymentIdOverride);
        var payoutImpact = await _complaints.GetPayoutImpactAsync(
            complaint.EnrollmentId,
            plan.RefundAmount,
            cancellationToken);

        var platformBear = plan.IssueRefund && plan.RefundAmount > 0
            ? Math.Max(0m, Math.Round(
                plan.RefundAmount - (financial.SessionEarningAmount ?? 0m),
                2,
                MidpointRounding.AwayFromZero))
            : (decimal?)null;

        ComplaintReplacementSchedulePreviewDto? replacementPreview = null;
        if (plan.CreateReplacementSchedule)
        {
            var suggestedDate = schedule.Date > DateOnly.FromDateTime(DateTime.UtcNow)
                ? schedule.Date
                : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
            replacementPreview = new ComplaintReplacementSchedulePreviewDto
            {
                DurationMinutes = schedule.DurationMinutes,
                TeacherId = complaint.TeacherId,
                SuggestedDate = suggestedDate.ToString("yyyy-MM-dd"),
            };
        }

        return new ComplaintResolvePreviewDto
        {
            ResolutionCode = resolutionCode.ToString(),
            SuggestedRefundAmount = plan.IssueRefund ? plan.RefundAmount : null,
            Currency = plan.Currency,
            PaymentId = plan.PaymentId,
            RemainingRefundable = financial.RemainingRefundable,
            SessionEarningAmount = financial.SessionEarningAmount,
            CurrentEarningStatus = financial.SessionEarningStatus,
            PayoutImpact = payoutImpact,
            PlatformBearEstimate = platformBear,
            SessionEarningEffect = plan.SessionEarningEffect.ToString(),
            ReplacementPreview = replacementPreview,
        };
    }

    private static ComplaintResolutionPlan BuildPlan(
        SessionComplaintResolution resolutionCode,
        ComplaintSessionFinancialContextDto financial,
        decimal? refundAmountOverride,
        int? paymentIdOverride)
    {
        var paymentId = paymentIdOverride ?? financial.PrimaryPaymentId;
        var refundAmount = SessionComplaintRefundCalculator.ResolveRefundAmount(
            resolutionCode, financial, refundAmountOverride);

        return resolutionCode switch
        {
            SessionComplaintResolution.FullRefund or SessionComplaintResolution.PartialRefund => new ComplaintResolutionPlan
            {
                IssueRefund = true,
                RefundAmount = refundAmount,
                PaymentId = paymentId,
                Currency = financial.Currency,
                SessionEarningEffect = ComplaintSessionEarningEffect.Void,
            },
            SessionComplaintResolution.DeductTeacherEarning => new ComplaintResolutionPlan
            {
                SessionEarningEffect = ComplaintSessionEarningEffect.Void,
            },
            SessionComplaintResolution.ReplacementSession => new ComplaintResolutionPlan
            {
                CreateReplacementSchedule = true,
                SessionEarningEffect = ComplaintSessionEarningEffect.Void,
            },
            SessionComplaintResolution.WarnTeacher => new ComplaintResolutionPlan
            {
                WarnTeacher = true,
                SessionEarningEffect = ComplaintSessionEarningEffect.Release,
            },
            SessionComplaintResolution.RejectComplaint or SessionComplaintResolution.NoAction => new ComplaintResolutionPlan
            {
                SessionEarningEffect = ComplaintSessionEarningEffect.Release,
            },
            _ => new ComplaintResolutionPlan
            {
                SessionEarningEffect = ComplaintSessionEarningEffect.Release,
            },
        };
    }

    private async Task<int> CreateReplacementScheduleAsync(
        SessionComplaint complaint,
        CourseSchedule source,
        CancellationToken cancellationToken)
    {
        var suggestedDate = source.Date > DateOnly.FromDateTime(DateTime.UtcNow)
            ? source.Date
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var replacement = new CourseSchedule
        {
            EnrollmentId = source.EnrollmentId,
            CourseSessionId = source.CourseSessionId,
            Date = suggestedDate,
            TeacherAvailabilityId = source.TeacherAvailabilityId,
            DurationMinutes = source.DurationMinutes,
            TeachingModeId = source.TeachingModeId,
            LocationId = source.LocationId,
            Status = ScheduleStatus.Scheduled,
            TeacherNote = $"Replacement for complaint #{complaint.Id}",
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _schedules.AddAsync(replacement);
        return created.Id;
    }

    private async Task VoidEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken)
    {
        var line = await _complaints.GetActiveEarningLineForScheduleAsync(courseScheduleId, cancellationToken);
        if (line == null)
            return;

        line.Status = TeacherEarningLineStatus.Voided;
        await _complaints.UpdateEarningLineAsync(line, cancellationToken);
    }

    private async Task ReleaseEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken)
    {
        if (await _complaints.HasBlockingComplaintAsync(courseScheduleId, cancellationToken))
            return;

        var line = await _complaints.GetOnHoldEarningLineForScheduleAsync(courseScheduleId, cancellationToken);
        if (line == null)
            return;

        line.Status = TeacherEarningLineStatus.Pending;
        await _complaints.UpdateEarningLineAsync(line, cancellationToken);
    }
}
