using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Complaint;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;
using AdminPreview = Qalam.Data.DTOs.Admin.ComplaintResolvePreviewDto;
using UnifiedPreview = Qalam.Data.DTOs.Complaint.ComplaintResolvePreviewDto;
using ResolveComplaintRequest = Qalam.Data.DTOs.Complaint.ResolveComplaintRequest;
using ComplaintSessionFinancialContextDto = Qalam.Data.DTOs.Admin.ComplaintSessionFinancialContextDto;
using ComplaintReplacementSchedulePreviewDto = Qalam.Data.DTOs.Admin.ComplaintReplacementSchedulePreviewDto;

namespace Qalam.Service.Implementations;

public class ComplaintResolutionOrchestrator : IComplaintResolutionOrchestrator
{
    private readonly ISessionComplaintRepository _complaints;
    private readonly ICourseScheduleRepository _schedules;
    private readonly IRefundService _refundService;
    private readonly ISessionAuditService _audit;
    private readonly ITeacherFinanceImpactService _financeImpact;
    private readonly ApplicationDBContext _db;

    public ComplaintResolutionOrchestrator(
        ISessionComplaintRepository complaints,
        ICourseScheduleRepository schedules,
        IRefundService refundService,
        ISessionAuditService audit,
        ITeacherFinanceImpactService financeImpact,
        ApplicationDBContext db)
    {
        _complaints = complaints;
        _schedules = schedules;
        _refundService = refundService;
        _audit = audit;
        _financeImpact = financeImpact;
        _db = db;
    }

    public Task<AdminPreview> GetPreviewAsync(
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

        TeacherEarningLineSnapshot? voidedLine = null;
        if (plan.SessionEarningEffect == ComplaintSessionEarningEffect.Void)
            voidedLine = await CaptureAndVoidEarningForScheduleAsync(schedule.Id, cancellationToken);

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

            if (await _financeImpact.IsAlreadyPaidForEnrollmentAsync(complaint.EnrollmentId, cancellationToken))
            {
                var clawback = voidedLine?.Amount ?? financial.SessionEarningAmount ?? 0m;
                if (clawback > 0)
                {
                    await _financeImpact.RecordSettlementForAlreadyPaidAsync(
                        complaint.TeacherId,
                        clawback,
                        plan.Currency,
                        refund.Id,
                        complaintId,
                        voidedLine?.Id,
                        adminUserId,
                        cancellationToken);
                }
            }
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

        if (voidedLine != null && resolutionCode == SessionComplaintResolution.DeductTeacherEarning)
        {
            await _financeImpact.RecordEarningDeductionPenaltyAsync(
                complaint.TeacherId,
                voidedLine.Amount,
                voidedLine.Currency,
                schedule.Id,
                complaintId,
                resolutionCode.ToString(),
                resolutionNotes,
                adminUserId,
                cancellationToken);
        }

        if (plan.WarnTeacher)
        {
            await _financeImpact.RecordWarningAsync(
                complaint.TeacherId,
                schedule.Id,
                resolutionNotes,
                complaintId,
                resolutionCode.ToString(),
                adminUserId,
                cancellationToken);

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

    private async Task<AdminPreview> BuildPreviewAsync(
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

        return new AdminPreview
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

    private async Task<TeacherEarningLineSnapshot?> CaptureAndVoidEarningForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken)
    {
        var line = await _complaints.GetActiveEarningLineForScheduleAsync(courseScheduleId, cancellationToken);
        if (line == null)
            return null;

        var snapshot = new TeacherEarningLineSnapshot(line.Id, line.Amount, line.Currency);
        line.Status = TeacherEarningLineStatus.Voided;
        await _complaints.UpdateEarningLineAsync(line, cancellationToken);
        return snapshot;
    }

    private sealed record TeacherEarningLineSnapshot(int Id, decimal Amount, string Currency);

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

    public async Task<UnifiedPreview> GetPreviewUnifiedAsync(
        Complaint complaint,
        ComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default)
    {
        if (complaint.SubjectType == ComplaintSubjectType.Session
            && complaint.CourseScheduleId.HasValue
            && complaint.LegacySessionComplaintId.HasValue)
        {
            var legacy = await GetPreviewAsync(
                complaint.CourseScheduleId.Value,
                complaint.LegacySessionComplaintId.Value,
                ComplaintRules.ToLegacySessionResolution(resolutionCode),
                refundAmountOverride,
                paymentIdOverride,
                cancellationToken);

            return new UnifiedPreview
            {
                ResolutionCode = legacy.ResolutionCode,
                SuggestedRefundAmount = legacy.SuggestedRefundAmount,
                Currency = legacy.Currency,
                PaymentId = legacy.PaymentId,
                RemainingRefundable = legacy.RemainingRefundable,
                SessionEarningAmount = legacy.SessionEarningAmount,
                CurrentEarningStatus = legacy.CurrentEarningStatus,
                PayoutImpact = legacy.PayoutImpact,
                PlatformBearEstimate = legacy.PlatformBearEstimate,
                SessionEarningEffect = legacy.SessionEarningEffect,
                Warnings = new List<string>(),
            };
        }

        return await BuildNonSessionPreviewAsync(complaint, resolutionCode, refundAmountOverride, paymentIdOverride, cancellationToken);
    }

    public async Task ResolveUnifiedAsync(
        Complaint complaint,
        int adminUserId,
        ResolveComplaintRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ComplaintRules.IsOpen(complaint.Status))
            throw new InvalidOperationException("Complaint is already closed.");

        if (complaint.SubjectType == ComplaintSubjectType.Session
            && complaint.CourseScheduleId.HasValue
            && complaint.LegacySessionComplaintId.HasValue)
        {
            await ResolveAsync(
                complaint.CourseScheduleId.Value,
                complaint.LegacySessionComplaintId.Value,
                adminUserId,
                ComplaintRules.ToLegacySessionResolution(request.ResolutionCode),
                request.ResolutionNotes,
                request.RefundAmount,
                request.PaymentId,
                cancellationToken);

            await ApplyUnifiedClosureAsync(complaint, adminUserId, request, cancellationToken);
            return;
        }

        await ResolveNonSessionAsync(complaint, adminUserId, request, cancellationToken);
    }

    private async Task<UnifiedPreview> BuildNonSessionPreviewAsync(
        Complaint complaint,
        ComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        decimal remaining = 0m;
        string currency = "SAR";
        int? paymentId = paymentIdOverride ?? complaint.PaymentId;
        decimal? suggested = null;

        if (complaint.SubjectType is ComplaintSubjectType.Enrollment
            or ComplaintSubjectType.Payment
            or ComplaintSubjectType.Refund)
        {
            var payment = paymentId.HasValue
                ? await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
                : null;

            if (payment == null && complaint.EnrollmentId.HasValue)
            {
                var enrollmentId = complaint.EnrollmentId.Value;
                payment = await _db.Payments.AsNoTracking()
                    .Where(p => p.Status == PaymentStatus.Succeeded
                                && (p.EnrollmentPayments.Any(ep => ep.EnrollmentParticipant.EnrollmentId == enrollmentId)
                                    || p.PaymentItems.Any(i => i.ReferenceId == enrollmentId)))
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (payment != null)
            {
                paymentId = payment.Id;
                currency = payment.Currency;
                var refunded = await _db.Refunds.AsNoTracking()
                    .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Succeeded)
                    .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;
                remaining = Math.Max(0m, payment.TotalAmount - refunded);
            }
            else
            {
                warnings.Add("No succeeded payment found for refund preview.");
            }

            if (resolutionCode is ComplaintResolution.FullRefund or ComplaintResolution.PartialRefund)
            {
                suggested = resolutionCode == ComplaintResolution.FullRefund
                    ? remaining
                    : Math.Min(refundAmountOverride ?? remaining, remaining);
                if (suggested <= 0)
                    warnings.Add("No remaining refundable amount.");
            }
            else if (resolutionCode is not ComplaintResolution.NoAction
                     and not ComplaintResolution.RejectComplaint
                     and not ComplaintResolution.WarnTeacher
                     and not ComplaintResolution.CancelledByAdmin)
            {
                warnings.Add("This resolution has limited financial effect for non-session complaints.");
            }
        }
        else if (complaint.SubjectType == ComplaintSubjectType.OpenSessionRequest)
        {
            warnings.Add("Open-session-request complaints have no automatic financial effects unless a payment is linked.");
            if (paymentId.HasValue)
            {
                var payment = await _db.Payments.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
                if (payment != null)
                {
                    currency = payment.Currency;
                    var refunded = await _db.Refunds.AsNoTracking()
                        .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Succeeded)
                        .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;
                    remaining = Math.Max(0m, payment.TotalAmount - refunded);
                }
            }
        }
        else
        {
            warnings.Add("Other complaints are administrative only — no automatic financial effects.");
        }

        return new UnifiedPreview
        {
            ResolutionCode = resolutionCode.ToString(),
            SuggestedRefundAmount = suggested,
            Currency = currency,
            PaymentId = paymentId,
            RemainingRefundable = remaining,
            SessionEarningEffect = "None",
            PayoutImpact = "None",
            Warnings = warnings,
        };
    }

    private async Task ResolveNonSessionAsync(
        Complaint complaint,
        int adminUserId,
        ResolveComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var tracked = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == complaint.Id, cancellationToken)
            ?? throw new InvalidOperationException("Complaint not found.");

        int? refundId = null;
        if (request.ResolutionCode is ComplaintResolution.FullRefund or ComplaintResolution.PartialRefund)
        {
            if (tracked.SubjectType is not (ComplaintSubjectType.Enrollment
                or ComplaintSubjectType.Payment
                or ComplaintSubjectType.Refund)
                && tracked.PaymentId is null)
                throw new InvalidOperationException("Refunds are only allowed for financial subjects.");

            var preview = await BuildNonSessionPreviewAsync(
                tracked, request.ResolutionCode, request.RefundAmount, request.PaymentId, cancellationToken);
            if (!preview.PaymentId.HasValue || preview.SuggestedRefundAmount is null or <= 0)
                throw new InvalidOperationException("Refund cannot be issued for this complaint.");

            var enrollmentId = tracked.EnrollmentId
                ?? throw new InvalidOperationException("Enrollment is required to issue a refund.");

            var refund = await _refundService.IssueRefundAsync(
                preview.PaymentId.Value,
                enrollmentId,
                preview.SuggestedRefundAmount.Value,
                preview.Currency,
                request.ResolutionNotes ?? $"Complaint #{tracked.Id}",
                adminUserId,
                cancellationToken);
            refundId = refund.Id;
        }
        else if (request.ResolutionCode is ComplaintResolution.ReplacementSession
                 or ComplaintResolution.DeductTeacherEarning)
        {
            throw new InvalidOperationException(
                "This resolution is only valid for session complaints.");
        }

        var toStatus = request.ResolutionCode == ComplaintResolution.RejectComplaint
            ? ComplaintStatus.Rejected
            : ComplaintStatus.Resolved;

        ComplaintRules.EnsureTransition(
            tracked.Status == ComplaintStatus.DecisionPending
                ? ComplaintStatus.DecisionPending
                : tracked.Status,
            toStatus);

        if (tracked.Status != ComplaintStatus.DecisionPending
            && ComplaintRules.CanTransition(tracked.Status, ComplaintStatus.DecisionPending))
            tracked.Status = ComplaintStatus.DecisionPending;

        ComplaintRules.EnsureTransition(tracked.Status, toStatus);
        tracked.Status = toStatus;
        tracked.ResolutionCode = request.ResolutionCode;
        tracked.ResolutionNotes = request.ResolutionNotes;
        tracked.ResolvedAt = DateTime.UtcNow;
        tracked.ResolvedByUserId = adminUserId;
        tracked.LinkedRefundId = refundId;
        tracked.RequiresComplainantResponse = false;
        tracked.RequiresRespondentResponse = false;
        await _db.SaveChangesAsync(cancellationToken);

        _db.ComplaintTimelineEntries.Add(new ComplaintTimelineEntry
        {
            ComplaintId = tracked.Id,
            EventType = toStatus == ComplaintStatus.Rejected
                ? ComplaintTimelineEventType.Rejected
                : ComplaintTimelineEventType.Resolved,
            FromStatus = ComplaintStatus.DecisionPending,
            ToStatus = toStatus,
            ActorUserId = adminUserId,
            ActorRole = "Admin",
            Notes = request.ResolutionNotes,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyUnifiedClosureAsync(
        Complaint complaint,
        int adminUserId,
        ResolveComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var tracked = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == complaint.Id, cancellationToken);
        if (tracked == null)
            return;

        var legacy = tracked.LegacySessionComplaintId.HasValue
            ? await _db.SessionComplaints.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == tracked.LegacySessionComplaintId.Value, cancellationToken)
            : null;

        tracked.Status = request.ResolutionCode == ComplaintResolution.RejectComplaint
            ? ComplaintStatus.Rejected
            : ComplaintStatus.Resolved;
        tracked.ResolutionCode = request.ResolutionCode;
        tracked.ResolutionNotes = request.ResolutionNotes ?? legacy?.ResolutionNotes;
        tracked.ResolvedAt = legacy?.ResolvedAt ?? DateTime.UtcNow;
        tracked.ResolvedByUserId = adminUserId;
        tracked.LinkedRefundId = legacy?.RefundId;
        tracked.ReplacementScheduleId = legacy?.ReplacementScheduleId;
        tracked.RequiresComplainantResponse = false;
        tracked.RequiresRespondentResponse = false;
        await _db.SaveChangesAsync(cancellationToken);

        _db.ComplaintTimelineEntries.Add(new ComplaintTimelineEntry
        {
            ComplaintId = tracked.Id,
            EventType = tracked.Status == ComplaintStatus.Rejected
                ? ComplaintTimelineEventType.Rejected
                : ComplaintTimelineEventType.Resolved,
            FromStatus = ComplaintStatus.DecisionPending,
            ToStatus = tracked.Status,
            ActorUserId = adminUserId,
            ActorRole = "Admin",
            Notes = tracked.ResolutionNotes,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

